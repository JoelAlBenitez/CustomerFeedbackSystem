namespace CustomerFeedbackSystem.Data.Dto;

public sealed class SocialCommentCsvRecord
{
    public string? IdComment { get; set; }

    public string? IdCliente { get; set; }

    public string? IdProducto { get; set; }

    public string? Fuente { get; set; }

    public string? Fecha { get; set; }

    public string? Comentario { get; set; }
}
