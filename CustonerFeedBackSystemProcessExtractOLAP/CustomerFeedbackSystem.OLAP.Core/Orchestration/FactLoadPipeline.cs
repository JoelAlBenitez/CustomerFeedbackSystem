using System.Diagnostics;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.OLAP.Core.Orchestration;

public sealed class FactLoadPipeline
{
    private readonly IWarehouseLoadSession _session;
    private readonly IFactResetService _resetService;
    private readonly IFactLoader _loader;
    private readonly ILogger<FactLoadPipeline> _logger;

    public FactLoadPipeline(
        IWarehouseLoadSession session,
        IFactResetService resetService,
        IFactLoader loader,
        ILogger<FactLoadPipeline> logger)
    {
        _session = session;
        _resetService = resetService;
        _loader = loader;
        _logger = logger;
    }

    public async Task<FactLoadReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var report = new FactLoadReport();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting fact load.");

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

            var result = await RunLoaderAsync(cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Fact load failed: {Error}. Rolling back — the warehouse is left untouched.",
                    result.Errors[0]);

                await _session.RollbackAsync(cancellationToken);
                return Abort(report, stopwatch, result.Errors[0]);
            }

            foreach (var table in result.Value.Tables)
            {
                report.Add(table);
            }

            report.Channels = result.Value.Channels;
            report.Resolution = result.Value.Resolution;
            report.Agreement = result.Value.Agreement;

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
            "Fact load committed in {Elapsed}: {Read} read, {Written} written, {Discarded} discarded.",
            report.Elapsed, report.TotalRead, report.TotalWritten, report.TotalDiscarded);

        return report;
    }

    private async Task<Result<FactLoadOutcome>> RunLoaderAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _loader.LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The fact loader threw an unexpected exception.");
            return Result<FactLoadOutcome>.Failure(new FactLoadError("HechoOpiniones", ex.Message));
        }
    }

    private static FactLoadReport Abort(FactLoadReport report, Stopwatch stopwatch, Error error)
    {
        stopwatch.Stop();
        report.Elapsed = stopwatch.Elapsed;
        report.Committed = false;
        report.FailureReason = error;
        return report;
    }
}
