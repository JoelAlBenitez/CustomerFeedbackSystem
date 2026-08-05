namespace CustomerFeedbackSystem.OLAP.Worker;

public enum EtlPhase
{
    Extract,
    Load,
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
