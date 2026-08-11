namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class FactLoadOutcome
{
    public required IReadOnlyList<FactLoadStats> Tables { get; init; }

    public required IReadOnlyList<ChannelFactStats> Channels { get; init; }

    public required KeyResolutionStats Resolution { get; init; }

    public required ClassifierAgreement Agreement { get; init; }
}
