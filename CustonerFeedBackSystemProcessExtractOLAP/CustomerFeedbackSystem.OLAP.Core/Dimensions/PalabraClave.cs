namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

// One row per lemma, not per word form, so "recomiendo" and "recomendaría" collapse into a
// single member and indicator 5.2 counts them together.
public sealed class PalabraClave
{
    public const int MaxLength = 100;

    public required int SkPalabra { get; init; }
    public required string Palabra { get; init; }

    // Corpus-wide count. Not persisted — the schema has no column — but it is what the minimum
    // frequency threshold filters on and what the report counts as discarded.
    public required int Frecuencia { get; init; }
}
