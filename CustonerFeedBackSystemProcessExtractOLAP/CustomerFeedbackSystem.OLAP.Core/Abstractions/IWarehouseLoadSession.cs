using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface IWarehouseLoadSession : IAsyncDisposable
{
    Task<Result> OpenAsync(CancellationToken cancellationToken = default);

    Task<Result> CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
