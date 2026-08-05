using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Orchestration;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CustomerFeedbackSystem.OLAP.Tests.Core;

/// <summary>
/// The heart of the orchestration, exercised entirely with doubles — no database, no disk,
/// no network. That is possible only because Core references no infrastructure.
/// </summary>
public sealed class ExtractionSourceRunnerTests
{
    private static ExtractionSourceRunner<StagingEncuestaCsv> BuildRunner(
        IReadOnlyList<Result<StagingEncuestaCsv>> results,
        RecordingStagingWriter<StagingEncuestaCsv> writer,
        RecordingResetService reset,
        int batchSize = 5_000) =>
        new(
            new FakeExtractor<StagingEncuestaCsv>(results),
            writer,
            reset,
            new ExtractionRunOptions { BatchSize = batchSize, Enabled = true },
            NullLogger<ExtractionSourceRunner<StagingEncuestaCsv>>.Instance);

    private static List<Result<StagingEncuestaCsv>> Successes(int count) =>
        Enumerable.Range(1, count)
            .Select(i => Result<StagingEncuestaCsv>.Success(StagingSamples.Encuesta(i.ToString())))
            .ToList();

    [Fact]
    public async Task RunAsync_WithTenRecordsAndBatchOfThree_WritesFourBatches()
    {
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();
        var runner = BuildRunner(Successes(10), writer, new RecordingResetService(), batchSize: 3);

        var stats = await runner.RunAsync();

        writer.BatchSizes.Should().Equal(3, 3, 3, 1);
        stats.Read.Should().Be(10);
        stats.Written.Should().Be(10);
    }

    [Fact]
    public async Task RunAsync_WhenSourceIsUnavailable_DoesNotResetTheTable()
    {
        // The rule that matters most in this class: an unavailable source must NOT empty its
        // table, so the previous run's data survives instead of leaving an empty table behind.
        var reset = new RecordingResetService();
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();
        var results = new List<Result<StagingEncuestaCsv>>
        {
            Result<StagingEncuestaCsv>.Failure(new SourceUnavailableError("csv", "file not found")),
        };

        var stats = await BuildRunner(results, writer, reset).RunAsync();

        reset.CallCount.Should().Be(0);
        writer.BatchSizes.Should().BeEmpty();
        stats.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenSourceIsAvailable_ResetsBeforeTheFirstWrite()
    {
        var reset = new RecordingResetService();
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();

        await BuildRunner(Successes(2), writer, reset, batchSize: 1).RunAsync();

        reset.CallCount.Should().Be(1);
        reset.Tables.Should().ContainSingle().Which.Should().Be(writer.TableName);
    }

    [Fact]
    public async Task RunAsync_WithSomeInvalidRecords_WritesTheRestAndCountsRejections()
    {
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();
        var results = Successes(8);
        results.Insert(2, Result<StagingEncuestaCsv>.Failure(
            new RecordValidationError("csv", 3, "IdOpinion", "is empty")));
        results.Insert(5, Result<StagingEncuestaCsv>.Failure(
            new RecordValidationError("csv", 6, "Comentario", "is empty")));

        var stats = await BuildRunner(results, writer, new RecordingResetService()).RunAsync();

        stats.Read.Should().Be(8);
        stats.Written.Should().Be(8);
        stats.Rejected.Should().Be(2);
        stats.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_WithTruncations_CountsThemSeparatelyFromRejections()
    {
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();
        var results = Successes(3);
        results.Insert(0, Result<StagingEncuestaCsv>.Failure(
            new FieldTruncatedError("csv", 1, "Comentario", 50)));

        var stats = await BuildRunner(results, writer, new RecordingResetService()).RunAsync();

        stats.Truncated.Should().Be(1);
        stats.Rejected.Should().Be(0);
        stats.Written.Should().Be(3);
    }

    [Fact]
    public async Task RunAsync_WhenWriteFails_CountsTheErrorAndDoesNotThrow()
    {
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>(failWrites: true);

        var stats = await BuildRunner(Successes(3), writer, new RecordingResetService()).RunAsync();

        stats.Failed.Should().BeTrue();
        stats.Written.Should().Be(0);
        stats.ErrorsByCode.Should().ContainKey("STAGING_WRITE");
    }

    [Fact]
    public async Task RunAsync_WithZeroRecords_ResetsButNeverWrites()
    {
        // Zero rows is a legitimate result: staging must reflect this run, not the last one.
        var reset = new RecordingResetService();
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();

        var stats = await BuildRunner([], writer, reset).RunAsync();

        reset.CallCount.Should().Be(1);
        writer.BatchSizes.Should().BeEmpty();
        stats.Read.Should().Be(0);
        stats.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_RecordsElapsedTime()
    {
        var stats = await BuildRunner(Successes(1), new RecordingStagingWriter<StagingEncuestaCsv>(),
            new RecordingResetService()).RunAsync();

        stats.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_WhenResetFails_MarksTheSourceFailedWithoutWriting()
    {
        var writer = new RecordingStagingWriter<StagingEncuestaCsv>();

        var stats = await BuildRunner(Successes(3), writer, new RecordingResetService(fail: true)).RunAsync();

        stats.Failed.Should().BeTrue();
        writer.BatchSizes.Should().BeEmpty();
    }
}
