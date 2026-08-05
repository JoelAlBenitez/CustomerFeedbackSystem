using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using FluentAssertions;

namespace CustomerFeedbackSystem.OLAP.Tests.Core;

public sealed class SourceExtractionStatsTests
{
    [Fact]
    public void RecordError_WithTruncation_CountsAsTruncatedNotRejected()
    {
        var stats = new SourceExtractionStats("csv", "[Staging].[stgEncuestasCSV]");

        stats.RecordError(new FieldTruncatedError("csv", 1, "Comentario", 50));

        stats.Truncated.Should().Be(1);
        stats.Rejected.Should().Be(0);
    }

    [Fact]
    public void RecordError_WithValidation_CountsAsRejected()
    {
        var stats = new SourceExtractionStats("csv", "[Staging].[stgEncuestasCSV]");

        stats.RecordError(new RecordValidationError("csv", 4, "IdOpinion", "is empty"));

        stats.Rejected.Should().Be(1);
        stats.Truncated.Should().Be(0);
        stats.ErrorsByCode["VALIDATION"].Should().Be(1);
    }

    [Fact]
    public void AnySourceFailed_IsTrue_WhenOneSourceFailed()
    {
        var healthy = new SourceExtractionStats("csv", "a");
        var broken = new SourceExtractionStats("api", "b");
        broken.MarkFailed();

        var report = new ExtractionReport();
        report.Add(healthy);
        report.Add(broken);

        report.AnySourceFailed.Should().BeTrue();
    }

    [Fact]
    public void Totals_SumAcrossSources()
    {
        var first = new SourceExtractionStats("csv", "a");
        first.RecordRead();
        first.RecordRead();
        first.RecordWritten(2);

        var second = new SourceExtractionStats("api", "b");
        second.RecordRead();
        second.RecordWritten(1);
        second.RecordError(new RecordValidationError("api", 1, "idPost", "is empty"));

        var report = new ExtractionReport();
        report.Add(first);
        report.Add(second);

        report.TotalRead.Should().Be(3);
        report.TotalWritten.Should().Be(3);
        report.TotalRejected.Should().Be(1);
        report.AnySourceFailed.Should().BeFalse();
    }
}
