#nullable disable
namespace CustomerFeedbackSystem.Data.Models;

public partial class FuentesDato
{
    public int IdFuenteDatos { get; set; }

    public int IdTipoFuentes { get; set; }

    public DateTime FechaCarga { get; set; }
}