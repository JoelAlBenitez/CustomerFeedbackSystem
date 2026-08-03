namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface ITokenAnalyzer
{
    Task WarmUpAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> ExtractLemmas(string text);
}
