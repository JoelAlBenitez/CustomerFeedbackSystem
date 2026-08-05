namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public static class OltpDimensionQueries
{
    public const string Productos = """
        SELECT
            p.IdProducto,
            p.Nombre,
            c.Nombre AS Categoria
        FROM dbo.Productos AS p
        INNER JOIN dbo.Categorias AS c ON c.IdCategoria = p.IdCategoria
        ORDER BY p.IdProducto;
        """;

    public const string Clientes = """
        SELECT
            c.IdCliente,
            c.Nombre
        FROM dbo.Clientes AS c
        ORDER BY c.IdCliente;
        """;
}
