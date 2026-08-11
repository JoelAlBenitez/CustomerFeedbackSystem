namespace CustomerFeedbackSystem.OLAP.Core.Reporting;

public sealed record FactResetOutcome(long Opiniones, long Palabras)
{
    public static FactResetOutcome Empty { get; } = new(0, 0);

    public bool Cleared => Opiniones > 0 || Palabras > 0;
}
