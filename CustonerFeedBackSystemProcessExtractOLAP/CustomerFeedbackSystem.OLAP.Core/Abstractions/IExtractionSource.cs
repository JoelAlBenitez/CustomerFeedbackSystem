using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IExtractionSource
{
    string SourceName { get; }

    string TableName { get; }

    bool Enabled { get; }

    Task<SourceExtractionStats> RunAsync(CancellationToken cancellationToken = default);
}
