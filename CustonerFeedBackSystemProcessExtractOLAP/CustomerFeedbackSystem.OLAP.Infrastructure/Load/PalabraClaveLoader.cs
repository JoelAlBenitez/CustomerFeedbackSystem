using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Dimensions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Transformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class PalabraClaveLoader : IDimensionLoader
{
    private static readonly string[] Columns = ["SKPalabra", "palabra"];

    private readonly SqlWarehouseLoadSession _session;
    private readonly ITokenAnalyzer _tokenAnalyzer;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<PalabraClaveLoader> _logger;

    public PalabraClaveLoader(
        SqlWarehouseLoadSession session,
        ITokenAnalyzer tokenAnalyzer,
        IOptions<DimensionLoadOptions> options,
        ILogger<PalabraClaveLoader> logger)
    {
        _session = session;
        _tokenAnalyzer = tokenAnalyzer;
        _options = options.Value;
        _logger = logger;
    }

    public string DimensionName => "PalabraClave";

    public string TableName => WarehouseTables.PalabraClave;

    public int Order => 6;

    public async Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_tokenAnalyzer.IsReady)
        {
            return Result<DimensionLoadStats>.Failure(new PreconditionError(
                "Spanish NLP model",
                "Catalyst did not load, so no lemma can be extracted. Check the warm-up log and "
                + "the 'catalyst-models' folder before loading Dimenciones.PalabraClave"));
        }

        var stats = new DimensionLoadStats(DimensionName, TableName);
        var frequencies = await CountLemmasAsync(stats, cancellationToken);

        var threshold = Math.Max(1, _options.MinKeywordFrequency);

        var rows = new List<PalabraClave>();
        var discarded = 0;
        var nextKey = 1;

        foreach (var entry in frequencies.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.Ordinal))
        {
            if (entry.Value < threshold)
            {
                discarded++;
                continue;
            }

            rows.Add(new PalabraClave
            {
                SkPalabra = nextKey++,
                Palabra = entry.Key,
            });
        }

        stats.RecordDiscarded(discarded);

        await SqlWarehouseWriter.WriteAsync(
            _session, TableName, Columns, rows,
            r => [r.SkPalabra, r.Palabra],
            _options.CommandTimeoutSeconds, cancellationToken);

        await SqlWarehouseWriter.ReseedAsync(
            _session, TableName, rows.Count, _options.CommandTimeoutSeconds, cancellationToken);

        stats.RecordWritten(rows.Count);
        stats.Note = $"{frequencies.Count} lemas distintos, umbral ≥ {threshold}";

        _logger.LogInformation(
            "Keyword dimension built from {Comments} comment(s): {Distinct} distinct lemmas, "
            + "{Kept} kept, {Discarded} below the frequency threshold of {Threshold}.",
            stats.Read, frequencies.Count, rows.Count, discarded, threshold);

        return Result<DimensionLoadStats>.Success(stats);
    }

    private async Task<Dictionary<string, int>> CountLemmasAsync(
        DimensionLoadStats stats,
        CancellationToken cancellationToken)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.Ordinal);

        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = StagingQueries.AllComments;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            stats.RecordRead();

            var text = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (DimensionSentinels.IsMissing(text))
            {
                continue;
            }

            foreach (var lemma in _tokenAnalyzer.ExtractLemmas(text!))
            {
                var word = RawValueParsing.Truncate(lemma, DimensionSentinels.PalabraMaxLength);
                frequencies[word] = frequencies.GetValueOrDefault(word) + 1;
            }
        }

        return frequencies;
    }
}
