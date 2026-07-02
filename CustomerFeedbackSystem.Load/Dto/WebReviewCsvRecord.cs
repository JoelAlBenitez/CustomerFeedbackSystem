namespace CustomerFeedbackSystem.Load.Dto;

public sealed class WebReviewCsvRecord
{
    public string? IdReview { get; set; }

    public string? IdCliente { get; set; }

    public string? IdProducto { get; set; }

    public string? Fecha { get; set; }

    public string? Comentario { get; set; }

    public string? Rating { get; set; }
}
