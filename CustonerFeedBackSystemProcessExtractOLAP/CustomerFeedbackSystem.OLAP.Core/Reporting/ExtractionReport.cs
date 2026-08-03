namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class ExtractionReport
{
    private readonly List<SourceExtractionStats> _sources = [];

    public IReadOnlyList<SourceExtractionStats> Sources => _sources;
    public TimeSpan Elapsed { get; set; }

    public void Add(SourceExtractionStats stats) => _sources.Add(stats);

    public long TotalRead => _sources.Sum(s => s.Read);

    public long TotalWritten => _sources.Sum(s => s.Written);

    public long TotalRejected => _sources.Sum(s => s.Rejected);

    public long TotalTruncated => _sources.Sum(s => s.Truncated);

    public bool AnySourceFailed => _sources.Exists(s => s.Failed);
}
