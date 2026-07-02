namespace CustomerFeedbackSystem.Load.Transformation;

/// <summary>
/// The single place that decides what "missing text" becomes. Used both when
/// building a lookup catalog's distinct values and when resolving a row against
/// that same catalog, so the two can never drift out of sync.
/// </summary>
internal static class TextNormalization
{
    public const string Sentinel = "-";

    public static string OrSentinel(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Sentinel : value.Trim();

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
