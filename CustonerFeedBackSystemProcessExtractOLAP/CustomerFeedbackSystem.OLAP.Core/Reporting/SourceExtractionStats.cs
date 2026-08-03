using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;

namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed class SourceExtractionStats
{
    private readonly Dictionary<string, int> _errorsByCode = new(StringComparer.Ordinal);

    public SourceExtractionStats(string sourceName, string tableName)
    {
        SourceName = sourceName;
        TableName = tableName;
    }

    public string SourceName { get; }

    public string TableName { get; }

    public long Read { get; private set; }

    public long Written { get; private set; }

    public long Rejected { get; private set; }

    public long Truncated { get; private set; }

    public bool Failed { get; private set; }

    public TimeSpan Elapsed { get; set; }

    public IReadOnlyDictionary<string, int> ErrorsByCode => _errorsByCode;

    public void RecordRead() => Read++;

    public void RecordWritten(int count) => Written += count;

    public void MarkFailed() => Failed = true;

    public void RecordError(Error error)
    {
        _errorsByCode[error.Code] = _errorsByCode.GetValueOrDefault(error.Code) + 1;

       
        if (error is FieldTruncatedError)
        {
            Truncated++;
        }
        else
        {
            Rejected++;
        }
    }
}
