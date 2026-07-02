#nullable disable

namespace CustomerFeedbackSystem.Data.Models;

public partial class ComentariosSociale
{
    public int IdComentarioSocial { get; set; }

    public int IdComentario { get; set; }

    public int IdCliente { get; set; }

    public int IdProducto { get; set; }

    public int IdFuenteSocial { get; set; }

    public DateTime Fecha { get; set; }
}