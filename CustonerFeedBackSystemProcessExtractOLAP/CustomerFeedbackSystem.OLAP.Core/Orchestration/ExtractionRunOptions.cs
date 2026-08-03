namespace CustomerFeedbackSystem.OLAP.Core.Orchestration;

public sealed class ExtractionRunOptions
{
    
    public int BatchSize { get; init; } = 5_000;

    public bool Enabled { get; init; } = true;
}
