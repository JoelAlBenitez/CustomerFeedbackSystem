namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public static class WarehouseKeyQueries
{
    public const string Clientes = """
        SELECT NkCliente, SkCliente FROM Dimenciones.DimCliente WHERE NkCliente IS NOT NULL;
        """;

    public const string Productos = """
        SELECT NkProducto, SkProducto FROM Dimenciones.DimProducto WHERE NkProducto IS NOT NULL;
        """;

    public const string Fechas = """
        SELECT SkFecha FROM Dimenciones.DimFecha;
        """;

    public const string Fuentes = """
        SELECT SkFuente, Canal, FuenteDetalle FROM Dimenciones.DimFuente;
        """;

    public const string Clasificaciones = """
        SELECT SkClasificacion, Clasificacion FROM Dimenciones.DimClasificacion;
        """;

    public const string Palabras = """
        SELECT SKPalabra, palabra FROM Dimenciones.PalabraClave;
        """;

    public const string RowCounts = """
        SELECT
            (SELECT COUNT(*) FROM Dimenciones.DimFecha)         AS Fechas,
            (SELECT COUNT(*) FROM Dimenciones.DimCliente)       AS Clientes,
            (SELECT COUNT(*) FROM Dimenciones.DimProducto)      AS Productos,
            (SELECT COUNT(*) FROM Dimenciones.DimFuente)        AS Fuentes,
            (SELECT COUNT(*) FROM Dimenciones.DimClasificacion) AS Clasificaciones,
            (SELECT COUNT(*) FROM Dimenciones.PalabraClave)     AS Palabras;
        """;
}
