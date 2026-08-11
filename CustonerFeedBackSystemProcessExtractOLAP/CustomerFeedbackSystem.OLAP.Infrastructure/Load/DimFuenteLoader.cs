using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimFuenteLoader : IDimensionLoader
{
    private const int CanalWidth = 20;
    private const int DetalleWidth = 50;
    private const int TipoCargaWidth = 20;

    private const string DefaultSurveySource = "EncuestaInterna";
    private const string WebSourceDetail = "SitioWeb";

    private static readonly string[] Columns = ["SkFuente", "Canal", "FuenteDetalle", "TipoCarga"];

    private readonly SqlWarehouseLoadSession _session;
    private readonly DimensionLoadOptions _options;

    public DimFuenteLoader(SqlWarehouseLoadSession session, IOptions<DimensionLoadOptions> options)
    {
        _session = session;
        _options = options.Value;
    }

    public string DimensionName => "DimFuente";

    public string TableName => WarehouseTables.DimFuente;

    public int Order => 2;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DimensionLoadStats(DimensionName, TableName);

        var surveySources = await ReadDistinctAsync(StagingQueries.DistinctSurveySources, stats, cancellationToken);
        var platforms = await ReadDistinctAsync(StagingQueries.DistinctPlatforms, stats, cancellationToken);

        if (surveySources.Count == 0)
        {
            surveySources.Add(DefaultSurveySource);
        }

        if (platforms.Count == 0)
        {
            platforms.Add(DimensionSentinels.UnknownAttribute);
        }

        var rows = new List<DimFuente>();
        var nextKey = 1;

        foreach (var source in surveySources)
        {
            rows.Add(Build(nextKey++, DimensionSentinels.CanalEncuestas, source, DimensionSentinels.TipoCargaCsv));
        }

        rows.Add(Build(
            nextKey++,
            DimensionSentinels.CanalResenasWeb,
            WebSourceDetail,
            DimensionSentinels.TipoCargaBaseDatos));

        foreach (var platform in platforms)
        {
            rows.Add(Build(
                nextKey++,
                DimensionSentinels.CanalRedesSociales,
                platform,
                DimensionSentinels.TipoCargaApi));
        }

        await SqlWarehouseWriter.WriteAsync(
            _session, TableName, Columns, rows,
            r => [r.SkFuente, r.Canal, r.FuenteDetalle, r.TipoCarga],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.ReseedAsync(
            _session, TableName, rows.Count, _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(rows.Count);
        stats.Note = $"{surveySources.Count} encuesta(s), 1 web, {platforms.Count} plataforma(s)";

        return Result<DimensionLoadStats>.Success(stats);
    }

    private static DimFuente Build(int key, string canal, string detalle, string tipoCarga) => new()
    {
        SkFuente = key,
        Canal = RawValueParsing.Truncate(canal, CanalWidth),
        FuenteDetalle = RawValueParsing.Truncate(detalle, DetalleWidth),
        TipoCarga = RawValueParsing.Truncate(tipoCarga, TipoCargaWidth),
    };

    private async Task<List<string>> ReadDistinctAsync(
        string sql,
        DimensionLoadStats stats,
        CancellationToken cancellationToken)
    {
        var values = new List<string>();

        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.RecordRead();

            var raw = reader.IsDBNull(0) ? null : reader.GetString(0);
            var value = RawValueParsing.OrUnknownAttribute(raw);

            if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(value);
            }
        }

        return values;
    }
}
