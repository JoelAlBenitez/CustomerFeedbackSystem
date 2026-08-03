using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;
public interface IExtractor<T>
{
    string SourceName { get; }

    IAsyncEnumerable<Result<T>> ExtractAsync(CancellationToken cancellationToken = default);
}
