using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IFactLoader
{
    Task<Result<FactLoadOutcome>> LoadAsync(CancellationToken cancellationToken = default);
}
