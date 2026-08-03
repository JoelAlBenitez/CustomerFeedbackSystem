using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IStagingResetService
{
    Task<Result> ResetAsync(string tableName, CancellationToken cancellationToken = default);
}
