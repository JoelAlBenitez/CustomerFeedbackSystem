namespace CustomerFeedbackSystem.OLAP.Core.Staging;

public sealed class StagingComentarioSocial
{
    public required string IdPostRaw { get; init; }
    public required string UsuarioRedSocialRaw { get; init; }
    public required string PlataformaRaw { get; init; }
    public required string FechaPostRaw { get; init; }
    public required string TextoComentarioRaw { get; init; }
    public required string InteraccionesRaw { get; init; }

    public required string EndpointApiMeta { get; init; }

    public required DateTime FechaCargaMeta { get; init; }

}
