namespace CustomerFeedbackSystem.OLAP.Worker;

public enum EtlPhase
{
    // Only E: the three extractors into Staging.*.
    Extract,

    // Only L: staging and the OLTP into Dimenciones.*.
    Load,

    // E then L in a single run. The load is skipped if any source failed extracting.
    Full,
}

public static class EtlPhaseParser
{
    public const string ConfigurationKey = "phase";

    public static EtlPhase Parse(string? value) =>
        Enum.TryParse<EtlPhase>(value, ignoreCase: true, out var phase) ? phase : EtlPhase.Full;

    public static bool IncludesExtract(this EtlPhase phase) => phase is EtlPhase.Extract or EtlPhase.Full;

    public static bool IncludesLoad(this EtlPhase phase) => phase is EtlPhase.Load or EtlPhase.Full;
}
