namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimFuente
{
    public required int SkFuente { get; init; }
    public required string Canal { get; init; }
    public required string FuenteDetalle { get; init; }
    public required string TipoCarga { get; init; }
}
