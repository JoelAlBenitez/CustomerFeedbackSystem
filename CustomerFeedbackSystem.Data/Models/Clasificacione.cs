#nullable disable
namespace CustomerFeedbackSystem.Data.Models;

public partial class Clasificacione
{
    public int IdClasificacion { get; set; }

    public string Clasificacion { get; set; }

    public int PuntajeSastifacion { get; set; }
}