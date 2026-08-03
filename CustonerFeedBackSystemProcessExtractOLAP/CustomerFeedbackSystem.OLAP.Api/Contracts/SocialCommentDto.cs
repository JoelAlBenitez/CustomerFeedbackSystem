namespace CustomerFeedbackSystem.OLAP.Api.Contracts;
public sealed class SocialCommentDto
{
  
    public required string IdPost { get; init; }

    public required string UsuarioRedSocial { get; init; }

    public required string Plataforma { get; init; }

    public required DateTime FechaPost { get; init; }

    public required string TextoComentario { get; init; }

    public string? Interacciones { get; init; }
}
