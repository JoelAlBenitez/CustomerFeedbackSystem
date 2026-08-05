using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

// Every run is a full refresh (doc 17 §7): dimensions are emptied before being rebuilt.
public interface IDimensionResetService
{
    Task<Result> ResetAsync(CancellationToken cancellationToken = default);
}
