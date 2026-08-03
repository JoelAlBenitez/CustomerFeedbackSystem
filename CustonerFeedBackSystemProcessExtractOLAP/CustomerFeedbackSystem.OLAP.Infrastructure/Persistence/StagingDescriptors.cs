using CustomerFeedbackSystem.OLAP.Core.Staging;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;


public static class StagingDescriptors
{
    private const string Schema = "Staging";

    public static StagingTableDescriptor<StagingEncuestaCsv> Encuestas { get; } = new()
    {
        SchemaName = Schema,
        TableName = "stgEncuestasCSV",
        ColumnNames =
        [
            "IdEncuestaRaw", "IdClienteRaw", "IdProductoRaw", "FechaEncuesta_Raw",
            "NivelSatisfaccionRaw", "ComentariosRaw", "ClasificacionRaw", "FuenteRaw",
            "NombreArchivoMeta", "FechaCargaMeta",
        ],
        ValueSelector = e =>
        [
            e.IdEncuestaRaw, e.IdClienteRaw, e.IdProductoRaw, e.FechaEncuestaRaw,
            e.NivelSatisfaccionRaw, e.ComentariosRaw, e.ClasificacionRaw, e.FuenteRaw,
            e.NombreArchivoMeta, e.FechaCargaMeta,
        ],
    };

    public static StagingTableDescriptor<StagingResenaWeb> Resenas { get; } = new()
    {
        SchemaName = Schema,
        TableName = "stgResenasWebBD",
        ColumnNames =
        [
            "IdResenaRaw", "IdUsuarioRaw", "IdProductoRaw", "FechaPublicacionRaw",
            "EstrellasRaw", "TituloResenaRaw", "CuerpoResenaRaw", "FechaCargaMeta",
        ],
        ValueSelector = e =>
        [
            e.IdResenaRaw, e.IdUsuarioRaw, e.IdProductoRaw, e.FechaPublicacionRaw,
            e.EstrellasRaw, e.TituloResenaRaw, e.CuerpoResenaRaw, e.FechaCargaMeta,
        ],
    };

    public static StagingTableDescriptor<StagingComentarioSocial> Sociales { get; } = new()
    {
        SchemaName = Schema,
        TableName = "stgRedesSocialesAPI",
        ColumnNames =
        [
            "IdPostRaw", "UsuarioRedSocial_Raw", "PlataformaRaw", "FechaPostRaw",
            "TextoComentarioRaw", "Interacciones_Raw", "EndpointAPIMeta", "FechaCargaMeta",
        ],
        ValueSelector = e =>
        [
            e.IdPostRaw, e.UsuarioRedSocialRaw, e.PlataformaRaw, e.FechaPostRaw,
            e.TextoComentarioRaw, e.InteraccionesRaw, e.EndpointApiMeta, e.FechaCargaMeta,
        ],
    };

  
    public static IReadOnlySet<string> AllQualifiedNames { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Encuestas.QualifiedName,
            Resenas.QualifiedName,
            Sociales.QualifiedName,
        };
}
