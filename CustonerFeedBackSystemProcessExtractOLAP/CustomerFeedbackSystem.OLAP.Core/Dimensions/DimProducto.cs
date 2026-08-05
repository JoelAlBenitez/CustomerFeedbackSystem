namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimProducto
{
    public required int SkProducto { get; init; }
    public int? NkProducto { get; init; }
    public required string Nombre { get; init; }
    public required string Categoria { get; init; }
}
