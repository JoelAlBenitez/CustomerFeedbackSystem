namespace CustomerFeedbackSystem.OLAP.Api.Persistence.Entities;

/// <summary>
/// Maps dbo.ComentariosSociales of the OLTP database. Database-first, like the OLTP project:
/// the database already exists and the context adapts to it.
/// <para>No navigation properties — joins are written explicitly in LINQ (doc 02 §4).</para>
/// </summary>
public sealed class ComentariosSociale
{
    public int IdComentarioSocial { get; set; }

    public int IdComentario { get; set; }

    public int IdCliente { get; set; }

    public int IdProducto { get; set; }

    public int IdFuenteSocial { get; set; }

    public DateTime Fecha { get; set; }
}
