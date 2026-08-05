using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IDimensionLoader
{
    string DimensionName { get; }

    string TableName { get; }

    // Ascending execution order inside the single load transaction.
    int Order { get; }

    Task<Result<DimensionLoadStats>> LoadAsync(CancellationToken cancellationToken = default);
}
