using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

// The L phase is all-or-nothing: Hechos.HechoOpiniones has five foreign keys and half-populated
// dimensions cannot be committed. This is the opposite policy to the E phase, which uses one
// transaction per source because the staging tables have no relationships between them.
public interface IDimensionLoadSession : IAsyncDisposable
{
    Task<Result> OpenAsync(CancellationToken cancellationToken = default);

    Task<Result> CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
