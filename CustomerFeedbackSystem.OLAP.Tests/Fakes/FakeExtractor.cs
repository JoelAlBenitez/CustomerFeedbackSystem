using System.Runtime.CompilerServices;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;

namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;

internal sealed class FakeExtractor<T> : IExtractor<T>
{
    private readonly IReadOnlyList<Result<T>> _results;
    private readonly Func<Task>? _beforeEachItem;

    public FakeExtractor(IReadOnlyList<Result<T>> results, Func<Task>? beforeEachItem = null)
    {
        _results = results;
        _beforeEachItem = beforeEachItem;
    }

    public string SourceName { get; init; } = "fake-source";

    public async IAsyncEnumerable<Result<T>> ExtractAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var result in _results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_beforeEachItem is not null)
            {
                await _beforeEachItem();
            }

            yield return result;
        }
    }
}

internal sealed class ThrowingExtractor<T> : IExtractor<T>
{
    public string SourceName => "throwing-source";

    public IAsyncEnumerable<Result<T>> ExtractAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("contract violation on purpose");
}
