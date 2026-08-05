namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;

/// <summary>
/// Drains an IAsyncEnumerable into a list. Six lines instead of taking a dependency on
/// System.Linq.Async just to materialise a stream in a test.
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> DrainAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var items = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }
}
