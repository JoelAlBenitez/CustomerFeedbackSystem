namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface ITokenAnalyzer
{
    bool IsReady { get; }

    Task WarmUpAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> ExtractLemmas(string text);
}
