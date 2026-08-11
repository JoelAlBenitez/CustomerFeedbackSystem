using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class FactLoadReport
{
    private readonly List<FactLoadStats> _tables = [];

    public IReadOnlyList<FactLoadStats> Tables => _tables;

    public IReadOnlyList<ChannelFactStats> Channels { get; set; } = [];

    public KeyResolutionStats Resolution { get; set; } = new();

    public ClassifierAgreement Agreement { get; set; } = new();

    public FactResetOutcome Reset { get; set; } = FactResetOutcome.Empty;

    public TimeSpan Elapsed { get; set; }

    public bool Committed { get; set; }

    public Error? FailureReason { get; set; }

    public void Add(FactLoadStats stats) => _tables.Add(stats);

    public long TotalRead => _tables.Sum(t => t.Read);

    public long TotalWritten => _tables.Sum(t => t.Written);

    public long TotalDiscarded => _tables.Sum(t => t.Discarded);

    public bool AnyFactFailed => _tables.Exists(t => t.Failed);
}
