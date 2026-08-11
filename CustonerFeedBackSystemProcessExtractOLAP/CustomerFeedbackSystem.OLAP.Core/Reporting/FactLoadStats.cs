namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class FactLoadStats
{
    public FactLoadStats(string factName, string tableName)
    {
        FactName = factName;
        TableName = tableName;
    }

    public string FactName { get; }

    public string TableName { get; }

    public long Read { get; private set; }

    public long Written { get; private set; }

    public long Discarded { get; private set; }

    public bool Failed { get; private set; }

    public TimeSpan Elapsed { get; set; }

    public string? Note { get; set; }

    public void RecordRead(long count = 1) => Read += count;

    public void RecordWritten(long count) => Written += count;

    public void RecordDiscarded(long count = 1) => Discarded += count;

    public void MarkFailed() => Failed = true;
}
