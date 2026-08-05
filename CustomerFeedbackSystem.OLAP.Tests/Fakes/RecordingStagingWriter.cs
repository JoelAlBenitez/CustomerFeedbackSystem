using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;

namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;

internal sealed class RecordingStagingWriter<T> : IStagingWriter<T>
{
    private readonly bool _failWrites;

    public RecordingStagingWriter(bool failWrites = false)
    {
        _failWrites = failWrites;
    }

    public string TableName { get; init; } = "[Staging].[fake]";

    public List<int> BatchSizes { get; } = [];

    public List<T> Written { get; } = [];

    public Task<Result<int>> WriteBatchAsync(
        IReadOnlyCollection<T> batch,
        CancellationToken cancellationToken = default)
    {
        BatchSizes.Add(batch.Count);

        if (_failWrites)
        {
            return Task.FromResult(Result<int>.Failure(new StagingWriteError(TableName, "simulated failure")));
        }

        Written.AddRange(batch);
        return Task.FromResult(Result<int>.Success(batch.Count));
    }
}
