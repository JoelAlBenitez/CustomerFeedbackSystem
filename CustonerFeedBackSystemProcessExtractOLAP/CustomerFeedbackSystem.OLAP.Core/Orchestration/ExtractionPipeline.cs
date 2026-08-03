using System.Diagnostics;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.OLAP.Core.Orchestration;


public sealed class ExtractionPipeline
{
    private readonly IReadOnlyList<IExtractionSource> _sources;
    private readonly ILogger<ExtractionPipeline> _logger;

    public ExtractionPipeline(IEnumerable<IExtractionSource> sources, ILogger<ExtractionPipeline> logger)
    {
        _sources = sources.ToList();
        _logger = logger;
    }

    public async Task<ExtractionReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var report = new ExtractionReport();
        var stopwatch = Stopwatch.StartNew();

        var enabled = _sources.Where(s => s.Enabled).ToList();
        _logger.LogInformation("Starting extraction for {Count} enabled source(s).", enabled.Count);

        var stats = await Task.WhenAll(enabled.Select(source => RunSourceSafelyAsync(source, cancellationToken)));

        foreach (var stat in stats)
        {
            report.Add(stat);
        }

        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;

        _logger.LogInformation(
            "Extraction finished in {Elapsed}: {Read} read, {Written} written, {Rejected} rejected.",
            report.Elapsed, report.TotalRead, report.TotalWritten, report.TotalRejected);

        return report;
    }

   
    private async Task<SourceExtractionStats> RunSourceSafelyAsync(
        IExtractionSource source,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Source {Source} threw an unexpected exception and was isolated.", source.SourceName);

            var stats = new SourceExtractionStats(source.SourceName, source.TableName);
            stats.MarkFailed();
            return stats;
        }
    }
}
