namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Csv;

public sealed class SurveyCsvRow
{
    public string? IdOpinion { get; set; }

    public string? IdCliente { get; set; }

    public string? IdProducto { get; set; }

    public string? Fecha { get; set; }

    public string? Comentario { get; set; }

    public string? Clasificacion { get; set; }

    public string? PuntajeSatisfaccion { get; set; }

    public string? Fuente { get; set; }
}
