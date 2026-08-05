namespace CustomerFeedbackSystem.OLAP.Core.Abstractions;

public interface ITokenAnalyzer
{
    // False when the Spanish model could not be loaded. The E phase tolerates that; the load of
    // Dimenciones.PalabraClave cannot, and must fail loudly instead of writing zero lemmas.
    bool IsReady { get; }

    Task WarmUpAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> ExtractLemmas(string text);
}
