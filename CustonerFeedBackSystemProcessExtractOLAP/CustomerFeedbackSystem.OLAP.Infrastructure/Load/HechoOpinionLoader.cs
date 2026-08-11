using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Facts;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class HechoOpinionLoader : IFactLoader
{
    private static readonly string[] FactColumns =
    [
        "IdFactOpinion", "SkFecha", "SkCliente", "SkProducto",
        "SkFuente", "SkClasificacion", "Puntaje", "EsSatisfactoria",
    ];

    private static readonly string[] BridgeColumns = ["IdFactOpinion", "SKPalabra"];

    private static readonly (string Canal, string Sql)[] Channels =
    [
        (DimensionSentinels.CanalEncuestas, StagingOpinionQueries.Encuestas),
        (DimensionSentinels.CanalResenasWeb, StagingOpinionQueries.Resenas),
        (DimensionSentinels.CanalRedesSociales, StagingOpinionQueries.Sociales),
    ];

    private readonly SqlWarehouseLoadSession _session;
    private readonly ISentimentClassifier _classifier;
    private readonly ITokenAnalyzer _tokenAnalyzer;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<HechoOpinionLoader> _logger;

    public HechoOpinionLoader(
        SqlWarehouseLoadSession session,
        ISentimentClassifier classifier,
        ITokenAnalyzer tokenAnalyzer,
        IOptions<DimensionLoadOptions> options,
        ILogger<HechoOpinionLoader> logger)
    {
        _session = session;
        _classifier = classifier;
        _tokenAnalyzer = tokenAnalyzer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<FactLoadOutcome>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_tokenAnalyzer.IsReady)
        {
            return Result<FactLoadOutcome>.Failure(new PreconditionError(
                "Spanish NLP model",
                "Catalyst did not load, so no lemma can be linked to Dimenciones.PalabraClave. "
                + "Check the warm-up log and the 'catalyst-models' folder before loading the facts"));
        }

        var maps = await WarehouseKeyMapReader.ReadAsync(
            _session, _options.CommandTimeoutSeconds, cancellationToken);

        if (maps.IsFailure)
        {
            return Result<FactLoadOutcome>.Failure(maps.Errors[0]);
        }

        var accumulator = new FactAccumulator();

        foreach (var (canal, sql) in Channels)
        {
            await ReadChannelAsync(canal, sql, maps.Value, accumulator, cancellationToken);
        }

        await WriteAsync(accumulator, cancellationToken);

        _logger.LogInformation(
            "Fact load built {Facts} opinion(s) and {Bridge} keyword link(s) across {Channels} channel(s).",
            accumulator.Facts.Count, accumulator.Bridge.Count, accumulator.Channels.Count);

        return Result<FactLoadOutcome>.Success(new FactLoadOutcome
        {
            Tables = [accumulator.FactStats, accumulator.BridgeStats],
            Channels = accumulator.Channels,
            Resolution = accumulator.Resolution,
            Agreement = accumulator.Agreement,
        });
    }

    private async Task ReadChannelAsync(
        string canal,
        string sql,
        WarehouseKeyMaps maps,
        FactAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        var channelStats = new ChannelFactStats(canal);
        accumulator.Channels.Add(channelStats);

        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            accumulator.FactStats.RecordRead();

            var row = ReadRow(reader);
            Project(canal, row, maps, accumulator, channelStats);
        }
    }

    private static StagingOpinionRow ReadRow(System.Data.Common.DbDataReader reader) => new()
    {
        FuenteDetalleRaw = reader.IsDBNull(0) ? null : reader.GetString(0),
        ClienteRaw = reader.IsDBNull(1) ? null : reader.GetString(1),
        ProductoRaw = reader.IsDBNull(2) ? null : reader.GetString(2),
        FechaRaw = reader.IsDBNull(3) ? null : reader.GetString(3),
        PuntajeRaw = reader.IsDBNull(4) ? null : reader.GetString(4),
        TextoRaw = reader.IsDBNull(5) ? null : reader.GetString(5),
        EtiquetaOrigenRaw = reader.IsDBNull(6) ? null : reader.GetString(6),
    };

    private void Project(
        string canal,
        StagingOpinionRow row,
        WarehouseKeyMaps maps,
        FactAccumulator accumulator,
        ChannelFactStats channelStats)
    {
        var texto = DimensionSentinels.IsMissing(row.TextoRaw) ? null : row.TextoRaw!.Trim();
        var tienePuntaje = RawValueParsing.TryParseScore(row.PuntajeRaw, out var puntaje);

        if (texto is null && !tienePuntaje)
        {
            accumulator.FactStats.RecordDiscarded();
            return;
        }

        var detalle = RawValueParsing.OrUnknownAttribute(row.FuenteDetalleRaw);
        if (!maps.TryResolveFuente(canal, detalle, out var skFuente))
        {
            accumulator.FactStats.RecordDiscarded();
            _logger.LogWarning(
                "No member of Dimenciones.DimFuente matches channel '{Canal}' with detail '{Detalle}'; "
                + "the opinion was discarded.",
                canal, detalle);
            return;
        }

        var polarity = _classifier.Classify(tienePuntaje ? puntaje : null, texto);

        var fact = new HechoOpinion
        {
            IdFactOpinion = accumulator.NextFactId++,
            SkFecha = ResolveFecha(row.FechaRaw, maps, accumulator.Resolution),
            SkCliente = ResolveCliente(row.ClienteRaw, maps, accumulator.Resolution),
            SkProducto = ResolveProducto(row.ProductoRaw, maps, accumulator.Resolution),
            SkFuente = skFuente,
            SkClasificacion = maps.Clasificaciones[polarity],
            Puntaje = tienePuntaje ? puntaje : null,
            EsSatisfactoria = polarity == SentimentPolarity.Positiva,
        };

        accumulator.Facts.Add(fact);
        accumulator.Resolution.RecordPuntaje(tienePuntaje);

        RecordPolarity(channelStats, polarity);
        channelStats.RecordOpinion(tienePuntaje);
        RecordAgreement(row.EtiquetaOrigenRaw, polarity, accumulator.Agreement);

        LinkKeywords(fact.IdFactOpinion, texto, maps, accumulator);
    }

    private void LinkKeywords(
        long idFactOpinion,
        string? texto,
        WarehouseKeyMaps maps,
        FactAccumulator accumulator)
    {
        if (texto is null)
        {
            return;
        }

        var linked = new HashSet<int>();

        foreach (var lemma in _tokenAnalyzer.ExtractLemmas(texto))
        {
            accumulator.BridgeStats.RecordRead();

            var word = RawValueParsing.Truncate(lemma, DimensionSentinels.PalabraMaxLength);

            if (!maps.Palabras.TryGetValue(word, out var skPalabra))
            {
                accumulator.BridgeStats.RecordDiscarded();
                continue;
            }

            if (!linked.Add(skPalabra))
            {
                continue;
            }

            accumulator.Bridge.Add(new HechoOpinionPalabra
            {
                IdFactOpinion = idFactOpinion,
                SkPalabra = skPalabra,
            });
        }
    }

    private static int ResolveFecha(string? raw, WarehouseKeyMaps maps, KeyResolutionStats resolution)
    {
        if (RawValueParsing.TryParseDate(raw, out var date))
        {
            var key = RawValueParsing.ToDateKey(date);

            if (maps.Fechas.Contains(key))
            {
                resolution.RecordFecha(true);
                return key;
            }
        }

        resolution.RecordFecha(false);
        return DimensionSentinels.UnknownDateKey;
    }

    private static int ResolveCliente(string? raw, WarehouseKeyMaps maps, KeyResolutionStats resolution)
    {
        if (DimensionSentinels.IsMissing(raw))
        {
            resolution.RecordCliente(false, anonimo: true);
            return DimClienteLoader.AnonymousKey;
        }

        if (RawValueParsing.TryExtractNumericId(raw, out var nkCliente)
            && maps.Clientes.TryGetValue(nkCliente, out var skCliente))
        {
            resolution.RecordCliente(true, anonimo: false);
            return skCliente;
        }

        resolution.RecordCliente(false, anonimo: false);
        return DimClienteLoader.UnknownKey;
    }

    private static int ResolveProducto(string? raw, WarehouseKeyMaps maps, KeyResolutionStats resolution)
    {
        if (DimensionSentinels.IsMissing(raw))
        {
            resolution.RecordProducto(false, sinOrigen: true);
            return DimProductoLoader.UnknownKey;
        }

        if (RawValueParsing.TryExtractNumericId(raw, out var nkProducto)
            && maps.Productos.TryGetValue(nkProducto, out var skProducto))
        {
            resolution.RecordProducto(true, sinOrigen: false);
            return skProducto;
        }

        resolution.RecordProducto(false, sinOrigen: false);
        return DimProductoLoader.UnknownKey;
    }

    private static void RecordPolarity(ChannelFactStats channelStats, SentimentPolarity polarity)
    {
        switch (polarity)
        {
            case SentimentPolarity.Positiva:
                channelStats.RecordPositiva();
                break;
            case SentimentPolarity.Negativa:
                channelStats.RecordNegativa();
                break;
            case SentimentPolarity.Neutra:
                channelStats.RecordNeutra();
                break;
            default:
                channelStats.RecordSinClasificar();
                break;
        }
    }

    private static void RecordAgreement(
        string? etiquetaOrigenRaw,
        SentimentPolarity polarity,
        ClassifierAgreement agreement)
    {
        if (DimensionSentinels.IsMissing(etiquetaOrigenRaw))
        {
            return;
        }

        var origen = WarehouseKeyMapReader.ToPolarity(etiquetaOrigenRaw!);
        if (origen is not null)
        {
            agreement.Record(origen.Value == polarity);
        }
    }

    private async Task WriteAsync(FactAccumulator accumulator, CancellationToken cancellationToken)
    {
        await SqlWarehouseWriter.WriteAsync(
            _session, WarehouseTables.HechoOpiniones, FactColumns, accumulator.Facts,
            f => [f.IdFactOpinion, f.SkFecha, f.SkCliente, f.SkProducto, f.SkFuente, f.SkClasificacion,
                f.Puntaje, f.EsSatisfactoria],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.ReseedAsync(
            _session, WarehouseTables.HechoOpiniones, accumulator.Facts.Count,
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.WriteAsync(
            _session, WarehouseTables.HechoOpinionPalabra, BridgeColumns, accumulator.Bridge,
            b => [b.IdFactOpinion, b.SkPalabra],
            _options.CommandTimeoutSeconds, cancellationToken);

        accumulator.FactStats.RecordWritten(accumulator.Facts.Count);
        accumulator.BridgeStats.RecordWritten(accumulator.Bridge.Count);

        accumulator.FactStats.Note =
            $"{accumulator.Channels.Count} canales consolidados en una sola tabla de hechos";
        accumulator.BridgeStats.Note =
            $"{accumulator.Bridge.Count} vínculos opinión↔palabra clave (lemas distintos por opinión)";
    }
}
