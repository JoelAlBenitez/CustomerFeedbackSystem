using System.Diagnostics;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.OLAP.Core.Orchestration;

public sealed class DimensionLoadPipeline
{
    private readonly IDimensionLoadSession _session;
    private readonly IDimensionResetService _resetService;
    private readonly IReadOnlyList<IDimensionLoader> _loaders;
    private readonly ILogger<DimensionLoadPipeline> _logger;

    public DimensionLoadPipeline(
        IDimensionLoadSession session,
        IDimensionResetService resetService,
        IEnumerable<IDimensionLoader> loaders,
        ILogger<DimensionLoadPipeline> logger)
    {
        _session = session;
        _resetService = resetService;
        _loaders = loaders.OrderBy(l => l.Order).ToList();
        _logger = logger;
    }

    public async Task<DimensionLoadReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var report = new DimensionLoadReport();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting dimension load for {Count} dimension(s).", _loaders.Count);

        await using (_session)
        {
            var opened = await _session.OpenAsync(cancellationToken);
            if (opened.IsFailure)
            {
                return Abort(report, stopwatch, opened.Errors[0]);
            }

            var reset = await _resetService.ResetAsync(cancellationToken);
            if (reset.IsFailure)
            {
                await _session.RollbackAsync(cancellationToken);
                return Abort(report, stopwatch, reset.Errors[0]);
            }

            foreach (var loader in _loaders)
            {
                var result = await RunLoaderAsync(loader, cancellationToken);

                if (result.IsFailure)
                {
                    var failed = new DimensionLoadStats(loader.DimensionName, loader.TableName);
                    failed.MarkFailed();
                    report.Add(failed);

                    _logger.LogError(
                        "Dimension {Dimension} failed: {Error}. Rolling back — the warehouse is left untouched.",
                        loader.DimensionName, result.Errors[0]);

                    await _session.RollbackAsync(cancellationToken);
                    return Abort(report, stopwatch, result.Errors[0]);
                }

                report.Add(result.Value);
            }

            var committed = await _session.CommitAsync(cancellationToken);
            if (committed.IsFailure)
            {
                return Abort(report, stopwatch, committed.Errors[0]);
            }
        }

        report.Committed = true;
        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;

        _logger.LogInformation(
            "Dimension load committed in {Elapsed}: {Read} read, {Written} written, {Discarded} discarded.",
            report.Elapsed, report.TotalRead, report.TotalWritten, report.TotalDiscarded);

        return report;
    }

    private async Task<Result<DimensionLoadStats>> RunLoaderAsync(
        IDimensionLoader loader,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        Result<DimensionLoadStats> result;
        try
        {
            result = await loader.LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dimension {Dimension} threw an unexpected exception.", loader.DimensionName);
            return Result<DimensionLoadStats>.Failure(
                new Common.Errors.DimensionLoadError(loader.DimensionName, ex.Message));
        }

        stopwatch.Stop();

        if (result.IsSuccess)
        {
            result.Value.Elapsed = stopwatch.Elapsed;

            _logger.LogInformation(
                "Dimension {Dimension} loaded: {Written} rows into {Table} in {Elapsed}.",
                loader.DimensionName, result.Value.Written, loader.TableName, stopwatch.Elapsed);
        }

        return result;
    }

    private static DimensionLoadReport Abort(DimensionLoadReport report, Stopwatch stopwatch, Error error)
    {
        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;
        report.Committed = false;
        report.FailureReason = error;
        return report;
    }
}
