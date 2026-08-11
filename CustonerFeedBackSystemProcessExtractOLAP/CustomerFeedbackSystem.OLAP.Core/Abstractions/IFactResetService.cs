using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IFactResetService
{
    Task<Result> ResetAsync(CancellationToken cancellationToken = default);
}
