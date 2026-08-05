namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public static class DimensionTables
{
    public const string DimFecha = "[Dimenciones].[DimFecha]";
    public const string DimCliente = "[Dimenciones].[DimCliente]";
    public const string DimProducto = "[Dimenciones].[DimProducto]";
    public const string DimFuente = "[Dimenciones].[DimFuente]";
    public const string DimClasificacion = "[Dimenciones].[DimClasificacion]";
    public const string PalabraClave = "[Dimenciones].[PalabraClave]";

    public const string HechoOpiniones = "[Hechos].[HechoOpiniones]";
    public const string HechoOpinionPalabra = "[Hechos].[HechoOpinionPalabra]";

    // DimFecha is absent: its SkFecha is not an IDENTITY column.
    public static IReadOnlyList<(string Table, string IdentityColumn)> IdentityDimensions { get; } =
    [
        (DimCliente, "SkCliente"),
        (DimProducto, "SkProducto"),
        (DimFuente, "SkFuente"),
        (DimClasificacion, "SkClasificacion"),
        (PalabraClave, "SKPalabra"),
    ];
}
