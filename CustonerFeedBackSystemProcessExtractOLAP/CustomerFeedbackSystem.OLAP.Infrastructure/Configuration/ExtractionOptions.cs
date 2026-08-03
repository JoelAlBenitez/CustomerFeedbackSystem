namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class ExtractionOptions
{
    public const string SectionName = "Extraction";

    public int BatchSize { get; set; } = 5_000;
}
