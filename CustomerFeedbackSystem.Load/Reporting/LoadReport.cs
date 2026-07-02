using CustomerFeedbackSystem.Load.Common;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.Load.Reporting;

public sealed class LoadReport
{
    private readonly List<SourceLoadStats> _sources = [];

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

    public void Print(ILogger logger, TimeSpan elapsed)
    {
        logger.LogInformation("========== Load summary ==========");
        foreach (var source in _sources)
        {
            logger.LogInformation(
                "{Source,-22} read={Read,5} inserted={Inserted,5} rejected={Rejected,5}",
                source.SourceName, source.Read, source.Inserted, source.Rejected);

            foreach (var (reason, count) in source.RejectionsByReason)
            {
                logger.LogInformation("    rejected [{Reason}]: {Count}", reason, count);
            }
        }

        logger.LogInformation("-----------------------------------");
        logger.LogInformation(
            "TOTAL read={Read} inserted={Inserted} rejected={Rejected} elapsed={Elapsed}",
            TotalRead, TotalInserted, TotalRejected, elapsed);
        logger.LogInformation("===================================");
    }
}
