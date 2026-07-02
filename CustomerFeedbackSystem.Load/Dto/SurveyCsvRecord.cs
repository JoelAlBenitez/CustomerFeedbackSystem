namespace CustomerFeedbackSystem.Load.Dto;

public sealed class SurveyCsvRecord
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
