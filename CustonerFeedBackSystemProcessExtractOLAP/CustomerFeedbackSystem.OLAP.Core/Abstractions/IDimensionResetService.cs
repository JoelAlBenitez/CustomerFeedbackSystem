using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IDimensionResetService
{
    Task<Result> ResetAsync(CancellationToken cancellationToken = default);
}
