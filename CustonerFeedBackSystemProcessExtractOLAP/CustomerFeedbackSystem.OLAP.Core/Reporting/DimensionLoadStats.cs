namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class DimensionLoadStats
{
    public DimensionLoadStats(string dimensionName, string tableName)
    {
        DimensionName = dimensionName;
        TableName = tableName;
    }

    public string DimensionName { get; }

    public string TableName { get; }

    // Source rows examined: staging rows, OLTP rows or distinct lemmas, depending on the loader.
    public long Read { get; private set; }

    public long Written { get; private set; }

    // Read but deliberately not written: unparseable dates, lemmas below the frequency threshold.
    public long Discarded { get; private set; }

    public bool Failed { get; private set; }

    public TimeSpan Elapsed { get; set; }

    public string? Note { get; set; }

    public void RecordRead(long count = 1) => Read += count;

    public void RecordWritten(long count) => Written += count;

    public void RecordDiscarded(long count = 1) => Discarded += count;

    public void MarkFailed() => Failed = true;
}
