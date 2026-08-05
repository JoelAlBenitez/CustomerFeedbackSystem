namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public static class StagingQueries
{
    public const string DistinctDates = """
        SELECT DISTINCT FechaEncuesta_Raw   AS FechaRaw FROM Staging.stgEncuestasCSV
        UNION
        SELECT DISTINCT FechaPublicacionRaw AS FechaRaw FROM Staging.stgResenasWebBD
        UNION
        SELECT DISTINCT FechaPostRaw        AS FechaRaw FROM Staging.stgRedesSocialesAPI;
        """;

    public const string DistinctSurveySources = """
        SELECT DISTINCT FuenteRaw FROM Staging.stgEncuestasCSV ORDER BY FuenteRaw;
        """;

    public const string DistinctPlatforms = """
        SELECT DISTINCT PlataformaRaw FROM Staging.stgRedesSocialesAPI ORDER BY PlataformaRaw;
        """;

    public const string AllComments = """
        SELECT ComentariosRaw     AS Texto FROM Staging.stgEncuestasCSV
        UNION ALL
        SELECT CuerpoResenaRaw    AS Texto FROM Staging.stgResenasWebBD
        UNION ALL
        SELECT TextoComentarioRaw AS Texto FROM Staging.stgRedesSocialesAPI;
        """;

    public const string RowCounts = """
        SELECT
            (SELECT COUNT(*) FROM Staging.stgEncuestasCSV)     AS Encuestas,
            (SELECT COUNT(*) FROM Staging.stgResenasWebBD)     AS Resenas,
            (SELECT COUNT(*) FROM Staging.stgRedesSocialesAPI) AS Sociales;
        """;
}
