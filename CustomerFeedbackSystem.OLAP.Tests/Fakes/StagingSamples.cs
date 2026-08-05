using CustomerFeedbackSystem.OLAP.Core.Staging;

namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;


internal static class StagingSamples
{
    public static readonly DateTime LoadedAt = new(2026, 8, 3, 14, 32, 8, DateTimeKind.Utc);

    public static StagingEncuestaCsv Encuesta(string id = "1") => new()
    {
        IdEncuestaRaw = id,
        IdClienteRaw = "8537",
        IdProductoRaw = "366",
        FechaEncuestaRaw = "2025-07-15",
        NivelSatisfaccionRaw = "3",
        ComentariosRaw = "El producto está bien.",
        ClasificacionRaw = "Neutra",
        FuenteRaw = "EncuestaInterna",
        NombreArchivoMeta = "surveys_part1.csv",
        FechaCargaMeta = LoadedAt,
    };

    public static StagingResenaWeb Resena(string id = "1") => new()
    {
        IdResenaRaw = id,
        IdUsuarioRaw = "7",
        IdProductoRaw = "16",
        FechaPublicacionRaw = "2024-10-23",
        EstrellasRaw = "4",
        TituloResenaRaw = "-",
        CuerpoResenaRaw = "Producto llegó rápido y funciona perfecto.",
        FechaCargaMeta = LoadedAt,
    };

    public static StagingComentarioSocial Social(string id = "CS000001") => new()
    {
        IdPostRaw = id,
        UsuarioRedSocialRaw = "Cliente_19",
        PlataformaRaw = "Instagram",
        FechaPostRaw = "2025-06-15",
        TextoComentarioRaw = "Información suficiente, sin mayor novedad",
        InteraccionesRaw = "0",
        EndpointApiMeta = "/api/v1/social-comments?page=1&pageSize=500",
        FechaCargaMeta = LoadedAt,
    };
}
