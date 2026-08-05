using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class DimensionLoadReport
{
    private readonly List<DimensionLoadStats> _dimensions = [];

    public IReadOnlyList<DimensionLoadStats> Dimensions => _dimensions;

    public TimeSpan Elapsed { get; set; }

    public bool Committed { get; set; }

    public Error? FailureReason { get; set; }

    public void Add(DimensionLoadStats stats) => _dimensions.Add(stats);

    public long TotalRead => _dimensions.Sum(d => d.Read);

    public long TotalWritten => _dimensions.Sum(d => d.Written);

    public long TotalDiscarded => _dimensions.Sum(d => d.Discarded);

    public bool AnyDimensionFailed => _dimensions.Exists(d => d.Failed);
}
