namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;

public sealed class DatabaseSourceOptions
{
    public const string SectionName = "Sources:Database";

    public bool Enabled { get; set; } = true;

    public int CommandTimeoutSeconds { get; set; } = 120;
}
