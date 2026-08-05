namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class DimensionLoadOptions
{
    public const string SectionName = "Load";

    public int CommandTimeoutSeconds { get; set; } = 300;

    public int MinKeywordFrequency { get; set; } = 2;

    public int DateDimensionPaddingYears { get; set; } = 1;
}
