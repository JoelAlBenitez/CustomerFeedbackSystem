namespace CustomerFeedbackSystem.OLAP.Core.Facts;

public sealed class HechoOpinion
{
    public long IdFactOpinion { get; set; }

    public int SkFecha { get; set; }

    public int SkCliente { get; set; }

    public int SkProducto { get; set; }

    public int SkFuente { get; set; }

    public int SkClasificacion { get; set; }

    public byte? Puntaje { get; set; }

    public bool EsSatisfactoria { get; set; }
}
