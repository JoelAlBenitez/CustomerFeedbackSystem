using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Orchestration;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CustomerFeedbackSystem.OLAP.Tests.Core;

public sealed class ExtractionPipelineTests
{
    /// <summary>A source under full test control: outcome, duration and whether it misbehaves.</summary>
    private sealed class StubSource : IExtractionSource
    {
        private readonly bool _failed;
        private readonly bool _throws;
        private readonly TimeSpan _duration;

        public StubSource(
            string name,
            bool enabled = true,
            bool failed = false,
            bool throws = false,
            TimeSpan? duration = null)
        {
            SourceName = name;
            Enabled = enabled;
            _failed = failed;
            _throws = throws;
            _duration = duration ?? TimeSpan.Zero;
        }

        public string SourceName { get; }

        public string TableName => $"[Staging].[{SourceName}]";

        public bool Enabled { get; }

        public bool WasRun { get; private set; }

        public async Task<SourceExtractionStats> RunAsync(CancellationToken cancellationToken = default)
        {
            WasRun = true;

            if (_throws)
            {
                throw new InvalidOperationException("contract violation on purpose");
            }

            if (_duration > TimeSpan.Zero)
            {
                await Task.Delay(_duration, cancellationToken);
            }

            var stats = new SourceExtractionStats(SourceName, TableName);
            stats.RecordRead();
            stats.RecordWritten(1);

            if (_failed)
            {
                stats.MarkFailed();
            }

            return stats;
        }
    }

    private static ExtractionPipeline Build(params IExtractionSource[] sources) =>
        new(sources, NullLogger<ExtractionPipeline>.Instance);

    [Fact]
    public async Task RunAsync_WithThreeHealthySources_ReportsAllThree()
    {
        var report = await Build(new StubSource("csv"), new StubSource("db"), new StubSource("api")).RunAsync();

        report.Sources.Should().HaveCount(3);
        report.AnySourceFailed.Should().BeFalse();
        report.TotalWritten.Should().Be(3);
    }

    [Fact]
    public async Task RunAsync_WhenOneSourceFails_TheOthersStillComplete()
    {
        var healthy = new StubSource("csv");
        var broken = new StubSource("api", failed: true);
        var alsoHealthy = new StubSource("db");

        var report = await Build(healthy, broken, alsoHealthy).RunAsync();

        report.AnySourceFailed.Should().BeTrue();
        report.TotalWritten.Should().Be(3);
        report.Sources.Should().HaveCount(3);
    }

    [Fact]
    public async Task RunAsync_WhenASourceThrowsUnexpectedly_IsolatesItAndKeepsGoing()
    {
        // The last-resort guard: an implementation that breaks the IExtractor contract must
        // not take the run down with it.
        var rogue = new StubSource("rogue", throws: true);
        var healthy = new StubSource("csv");

        var report = await Build(rogue, healthy).RunAsync();

        report.AnySourceFailed.Should().BeTrue();
        report.Sources.Should().Contain(s => s.SourceName == "csv" && s.Written == 1);
    }

    [Fact]
    public async Task RunAsync_WithADisabledSource_DoesNotRunIt()
    {
        var disabled = new StubSource("api", enabled: false);
        var enabled = new StubSource("csv");

        var report = await Build(disabled, enabled).RunAsync();

        disabled.WasRun.Should().BeFalse();
        report.Sources.Should().ContainSingle().Which.SourceName.Should().Be("csv");
    }

    [Fact]
    public async Task RunAsync_WithNoEnabledSources_ReturnsAnEmptyReportWithoutThrowing()
    {
        var report = await Build(new StubSource("csv", enabled: false)).RunAsync();

        report.Sources.Should().BeEmpty();
        report.TotalRead.Should().Be(0);
        report.AnySourceFailed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_PropagatesTheCancellation()
    {
        // Cancellation is an order, not a failure.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var pipeline = Build(new StubSource("slow", duration: TimeSpan.FromSeconds(5)));

        var act = () => pipeline.RunAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_WithSlowSources_TakesLessThanTheirSum()
    {
        // Proof the sources overlap: three 300 ms sources in parallel must finish well under
        // the 900 ms they would take in sequence.
        var delay = TimeSpan.FromMilliseconds(300);
        var pipeline = Build(
            new StubSource("a", duration: delay),
            new StubSource("b", duration: delay),
            new StubSource("c", duration: delay));

        var report = await pipeline.RunAsync();

        report.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(750));
    }
}
