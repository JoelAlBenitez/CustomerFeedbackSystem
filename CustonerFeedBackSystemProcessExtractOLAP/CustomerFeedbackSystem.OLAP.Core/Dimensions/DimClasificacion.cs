namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

// Fixed four-member dimension: the three labels of SRS §2.4 plus the undecided one.
public sealed class DimClasificacion
{
    public const string Positiva = "Positiva";
    public const string Negativa = "Negativa";
    public const string Neutra = "Neutra";

    public required int SkClasificacion { get; init; }
    public required string Clasificacion { get; init; }

    // Representative 1-5 score of the label; NULL for "Sin Clasificar".
    public byte? PuntajeBase { get; init; }

    // Redundant with Clasificacion on purpose: indicators 2.3, 4.1, 5.2 and 6.2 aggregate with
    // SUM(CAST(EsNegativa AS INT)) instead of comparing text.
    public required bool EsPositiva { get; init; }
    public required bool EsNegativa { get; init; }
    public required bool EsNeutra { get; init; }
}
