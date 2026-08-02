#nullable disable

namespace CustomerFeedbackSystem.Data.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public string Nombre { get; set; }
}