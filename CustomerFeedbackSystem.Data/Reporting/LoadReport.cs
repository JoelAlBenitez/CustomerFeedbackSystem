namespace CustomerFeedbackSystem.Data.Reporting;

/// <summary>
/// Accumulates per-source stats for a run. Purely data — how this gets
/// displayed (console table, log lines, etc.) is a presentation concern that
/// belongs to whichever project is driving the run, not to this library.
/// </summary>
public sealed class LoadReport
{
    private readonly List<SourceLoadStats> _sources = [];

    public IReadOnlyList<SourceLoadStats> Sources => _sources;

    public TimeSpan Elapsed { get; set; }

    public SourceLoadStats ForSource(string sourceName)
    {
        var existing = _sources.Find(s => s.SourceName == sourceName);
        if (existing is not null)
        {
            return existing;
        }

        var created = new SourceLoadStats(sourceName);
        _sources.Add(created);
        return created;
    }

    public int TotalRead => _sources.Sum(s => s.Read);

    public int TotalInserted => _sources.Sum(s => s.Inserted);

    public int TotalRejected => _sources.Sum(s => s.Rejected);
}
