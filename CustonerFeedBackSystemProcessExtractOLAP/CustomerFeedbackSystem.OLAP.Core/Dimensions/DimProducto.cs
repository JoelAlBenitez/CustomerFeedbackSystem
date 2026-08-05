namespace CustomerFeedbackSystem.OLAP.Core.Dimensions;

public sealed class DimProducto
{
    public required int SkProducto { get; init; }

    // dbo.Productos.IdProducto; NULL for the "Desconocido" member.
    public int? NkProducto { get; init; }

    public required string Nombre { get; init; }
    public required string Categoria { get; init; }

    public static DimProducto UnknownMember(int skProducto) => new()
    {
        SkProducto = skProducto,
        NkProducto = null,
        Nombre = DimensionSentinels.UnknownMember,
        Categoria = DimensionSentinels.UncategorizedProduct,
    };
}
