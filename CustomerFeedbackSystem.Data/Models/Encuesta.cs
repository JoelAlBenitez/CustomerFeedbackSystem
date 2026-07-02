#nullable disable

namespace CustomerFeedbackSystem.Data.Models;

public partial class Encuesta
{
    public int IdOpinion { get; set; }

    public int IdCliente { get; set; }

    public int IdComentario { get; set; }

    public int IdClasificacion { get; set; }

    public int IdFuenteEncuestas { get; set; }

    public DateTime Fecha { get; set; }
}