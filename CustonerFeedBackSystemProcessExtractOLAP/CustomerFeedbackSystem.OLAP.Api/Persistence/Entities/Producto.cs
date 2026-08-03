namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;
public sealed class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = string.Empty;
}
