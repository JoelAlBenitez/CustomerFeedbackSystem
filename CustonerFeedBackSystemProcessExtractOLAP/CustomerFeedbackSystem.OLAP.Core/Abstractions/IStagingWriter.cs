using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;


public interface IStagingWriter<T>
{
    string TableName { get; }

    Task<Result<int>> WriteBatchAsync(IReadOnlyCollection<T> batch, CancellationToken cancellationToken = default);
}
