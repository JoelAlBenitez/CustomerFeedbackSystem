using CustomerFeedbackSystem.OLAP.Core.Dimensions;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public static class StagingOpinionQueries
{
    public const string Encuestas = $"""
        SELECT
            e.FuenteRaw            AS FuenteDetalleRaw,
            e.IdClienteRaw         AS ClienteRaw,
            e.IdProductoRaw        AS ProductoRaw,
            e.FechaEncuesta_Raw    AS FechaRaw,
            e.NivelSatisfaccionRaw AS PuntajeRaw,
            e.ComentariosRaw       AS TextoRaw,
            e.ClasificacionRaw     AS EtiquetaOrigenRaw
        FROM Staging.stgEncuestasCSV AS e
        ORDER BY e.IdEncuestaRaw;
        """;

    public const string Resenas = $"""
        SELECT
            '{DimensionSentinels.RawSentinel}' AS FuenteDetalleRaw,
            r.IdUsuarioRaw                     AS ClienteRaw,
            r.IdProductoRaw                    AS ProductoRaw,
            r.FechaPublicacionRaw              AS FechaRaw,
            r.EstrellasRaw                     AS PuntajeRaw,
            r.CuerpoResenaRaw                  AS TextoRaw,
            '{DimensionSentinels.RawSentinel}' AS EtiquetaOrigenRaw
        FROM Staging.stgResenasWebBD AS r
        ORDER BY r.IdResenaRaw;
        """;

    public const string Sociales = $"""
        SELECT
            s.PlataformaRaw                    AS FuenteDetalleRaw,
            s.UsuarioRedSocial_Raw             AS ClienteRaw,
            '{DimensionSentinels.RawSentinel}' AS ProductoRaw,
            s.FechaPostRaw                     AS FechaRaw,
            '{DimensionSentinels.RawSentinel}' AS PuntajeRaw,
            s.TextoComentarioRaw               AS TextoRaw,
            '{DimensionSentinels.RawSentinel}' AS EtiquetaOrigenRaw
        FROM Staging.stgRedesSocialesAPI AS s
        ORDER BY s.IdPostRaw;
        """;
}
