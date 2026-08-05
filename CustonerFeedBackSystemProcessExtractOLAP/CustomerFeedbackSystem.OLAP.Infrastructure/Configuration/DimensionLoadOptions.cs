namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class DimensionLoadOptions
{
    public const string SectionName = "Load";

    public int CommandTimeoutSeconds { get; set; } = 300;

    // A lemma appearing once in the whole corpus does not support any analysis and only inflates
    // the dimension (doc 17 §2.4). Set to 1 to keep every lemma.
    public int MinKeywordFrequency { get; set; } = 2;

    // Widens DimFecha beyond the range observed in staging, so a later run with newer data still
    // finds its keys. Zero means "exactly the observed years".
    public int DateDimensionPaddingYears { get; set; } = 1;
}
