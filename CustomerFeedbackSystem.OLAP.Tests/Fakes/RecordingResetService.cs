using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;

namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;

internal sealed class RecordingResetService : IStagingResetService
{
    private readonly bool _fail;

    public RecordingResetService(bool fail = false)
    {
        _fail = fail;
    }

    public int CallCount { get; private set; }

    public List<string> Tables { get; } = [];

    public Task<Result> ResetAsync(string tableName, CancellationToken cancellationToken = default)
    {
        CallCount++;
        Tables.Add(tableName);

        return Task.FromResult(_fail
            ? Result.Failure(new StagingWriteError(tableName, "simulated reset failure"))
            : Result.Success());
    }
}
