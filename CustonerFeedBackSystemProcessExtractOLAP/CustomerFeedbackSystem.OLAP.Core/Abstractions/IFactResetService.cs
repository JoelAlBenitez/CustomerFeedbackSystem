using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IFactResetService
{
    Task<Result<FactResetOutcome>> ResetAsync(CancellationToken cancellationToken = default);
}
