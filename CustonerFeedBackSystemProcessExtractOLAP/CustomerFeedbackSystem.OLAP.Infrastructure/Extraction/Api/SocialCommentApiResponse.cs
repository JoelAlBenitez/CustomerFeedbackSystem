namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Api;

public sealed class SocialCommentApiResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<SocialCommentItem> Items { get; set; } = [];
}


public sealed class SocialCommentItem
{
    public string? IdPost { get; set; }

    public string? UsuarioRedSocial { get; set; }

    public string? Plataforma { get; set; }

    public DateTime? FechaPost { get; set; }

    public string? TextoComentario { get; set; }

    public string? Interacciones { get; set; }
}
