using System.Diagnostics;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.OLAP.Core.Orchestration;

/// <summary>
/// Runs one source end to end: extract, batch, write, count. Implements the non-generic
/// <see cref="IExtractionSource"/> so the pipeline can hold sources of different record types
/// in one collection.
/// <para>
/// Lives in Core and knows nothing about SQL Server, CsvHelper or HttpClient — which is what
/// makes the whole orchestration testable with doubles and no database.
/// </para>
/// </summary>
public sealed class ExtractionSourceRunner<T> : IExtractionSource
{
    private readonly IExtractor<T> _extractor;
    private readonly IStagingWriter<T> _writer;
    private readonly IStagingResetService _resetService;
    private readonly ExtractionRunOptions _options;
    private readonly ILogger<ExtractionSourceRunner<T>> _logger;

    public ExtractionSourceRunner(
        IExtractor<T> extractor,
        IStagingWriter<T> writer,
        IStagingResetService resetService,
        ExtractionRunOptions options,
        ILogger<ExtractionSourceRunner<T>> logger)
    {
        _extractor = extractor;
        _writer = writer;
        _resetService = resetService;
        _options = options;
        _logger = logger;
    }

    public string SourceName => _extractor.SourceName;

    public string TableName => _writer.TableName;

    public bool Enabled => _options.Enabled;

    public async Task<SourceExtractionStats> RunAsync(CancellationToken cancellationToken = default)
    {
        var stats = new SourceExtractionStats(SourceName, TableName);
        var stopwatch = Stopwatch.StartNew();

        var buffer = new List<T>(_options.BatchSize);
        var tableReady = false;

        await foreach (var result in _extractor.ExtractAsync(cancellationToken))
        {
            if (result.IsFailure)
            {
                var error = result.Errors[0];

                // The whole source is gone. Record it, mark the source failed, and — this is
                // the part that matters — do NOT truncate the table. That ordering is what
                // preserves the previous run's data when a source is unavailable, instead of
                // leaving an empty table behind.
                if (error is SourceUnavailableError unavailable)
                {
                    _logger.LogWarning(
                        "Source {Source} unavailable: {Reason}. Continuing with remaining sources; "
                        + "{Table} keeps the previous run's data.",
                        SourceName, unavailable.Reason, TableName);

                    stats.RecordError(error);
                    stats.MarkFailed();
                    stopwatch.Stop();
                    stats.Elapsed = stopwatch.Elapsed;
                    return stats;
                }

                // A single bad record never stops the source.
                stats.RecordError(error);
                continue;
            }

            // First successful record proves the source is alive — only now is it safe to
            // empty the destination table.
            if (!tableReady)
            {
                var reset = await _resetService.ResetAsync(TableName, cancellationToken);
                if (reset.IsFailure)
                {
                    _logger.LogError("Could not reset {Table}: {Error}", TableName, reset.Errors[0]);
                    stats.RecordError(reset.Errors[0]);
                    stats.MarkFailed();
                    stopwatch.Stop();
                    stats.Elapsed = stopwatch.Elapsed;
                    return stats;
                }

                tableReady = true;
            }

            stats.RecordRead();
            buffer.Add(result.Value);

            if (buffer.Count >= _options.BatchSize)
            {
                await FlushAsync(buffer, stats, cancellationToken);
            }
        }

        // A source that yielded nothing at all still gets its table emptied: zero rows is a
        // legitimate result and staging must reflect this run, not the last one.
        if (!tableReady)
        {
            var reset = await _resetService.ResetAsync(TableName, cancellationToken);
            if (reset.IsFailure)
            {
                stats.RecordError(reset.Errors[0]);
                stats.MarkFailed();
            }
        }

        if (buffer.Count > 0)
        {
            await FlushAsync(buffer, stats, cancellationToken);
        }

        stopwatch.Stop();
        stats.Elapsed = stopwatch.Elapsed;

        var rowsPerSecond = stopwatch.Elapsed.TotalSeconds > 0
            ? stats.Read / stopwatch.Elapsed.TotalSeconds
            : stats.Read;

        _logger.LogInformation(
            "Source {Source} completed: {Read} read, {Written} written, {Rejected} rejected, "
            + "{Truncated} truncated in {Elapsed} ({RowsPerSecond:F0} rows/s).",
            SourceName, stats.Read, stats.Written, stats.Rejected, stats.Truncated,
            stopwatch.Elapsed, rowsPerSecond);

        return stats;
    }

    /// <summary>
    /// Writes the buffer and clears it. While this is awaited the IAsyncEnumerable is not
    /// being consumed, so the extractor throttles itself — the bounded buffer IS the
    /// back-pressure, and no Channel is needed for the base case.
    /// <para>
    /// If measurement ever showed reading sitting idle waiting on SQL, the evolution is a
    /// Channel.CreateBounded&lt;List&lt;T&gt;&gt;(capacity: 3) with a dedicated consumer to
    /// overlap the two. That is complexity worth paying for only once a measurement justifies
    /// it.
    /// </para>
    /// </summary>
    private async Task FlushAsync(List<T> buffer, SourceExtractionStats stats, CancellationToken cancellationToken)
    {
        var write = await _writer.WriteBatchAsync(buffer, cancellationToken);

        if (write.IsSuccess)
        {
            stats.RecordWritten(write.Value);
        }
        else
        {
            stats.RecordError(write.Errors[0]);
            stats.MarkFailed();
        }

        buffer.Clear();
    }
}
