namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimClasificacion
{
    public required int SkClasificacion { get; init; }
    public required string Clasificacion { get; init; }
    public byte? PuntajeBase { get; init; }
    public required bool EsPositiva { get; init; }
    public required bool EsNegativa { get; init; }
    public required bool EsNeutra { get; init; }
}
