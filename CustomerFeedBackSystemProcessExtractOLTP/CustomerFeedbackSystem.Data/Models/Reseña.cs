#nullable disable
namespace CustomerFeedbackSystem.Data.Models;

public partial class Reseña
{
    public int IdReview { get; set; }

    public int IdCliente { get; set; }

    public int IdProducto { get; set; }

    public int IdCommentario { get; set; }

    public int Rating { get; set; }
}