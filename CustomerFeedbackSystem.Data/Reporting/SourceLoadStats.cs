using CustomerFeedbackSystem.Data.Common;
namespace CustomerFeedbackSystem.Data.Reporting;
public sealed class SourceLoadStats
{
    private readonly Dictionary<string, int> _rejectionsByReason = new();

    public SourceLoadStats(string sourceName)
    {
        SourceName = sourceName;
    }

    public string SourceName { get; }

    public int Read { get; private set; }

    public int Rejected { get; private set; }

    public int Inserted { get; private set; }

    public IReadOnlyDictionary<string, int> RejectionsByReason => _rejectionsByReason;

    public void RecordRead() => Read++;

    public void RecordRejected(Error error)
    {
        Rejected++;
        _rejectionsByReason[error.Code] = _rejectionsByReason.GetValueOrDefault(error.Code) + 1;
    }

    public void RecordInserted(int count) => Inserted += count;
}
