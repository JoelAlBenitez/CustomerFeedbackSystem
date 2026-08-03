namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Database;

public static class WebReviewQuery
{
  
    public const string Sql = """
        SELECT
            r.IdReview,
            r.IdCliente,
            r.IdProducto,
            r.Rating,
            c.Comentarios,
            fd.FechaCarga
        FROM dbo.[Reseñas] AS r
        INNER JOIN dbo.Comentarios AS c
            ON c.IdComentario = r.IdCommentario
        OUTER APPLY (
            SELECT TOP (1) f.FechaCarga
            FROM dbo.FuentesDatos AS f
            INNER JOIN dbo.TipoFuentesDatos AS t ON t.IdTipoFuentes = f.IdTipoFuentes
            WHERE t.TipoFuente = 'Web'
            ORDER BY f.FechaCarga DESC
        ) AS fd
        ORDER BY r.IdReview;
        """;

  
    public const int IdReview = 0;
    public const int IdCliente = 1;
    public const int IdProducto = 2;
    public const int Rating = 3;
    public const int Comentarios = 4;
    public const int FechaCarga = 5;
}
