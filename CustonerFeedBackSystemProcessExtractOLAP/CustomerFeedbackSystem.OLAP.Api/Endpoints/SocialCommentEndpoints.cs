using CustomerFeedbackSystem.OLAP.Api.Configuration;
using CustomerFeedbackSystem.OLAP.Api.Contracts;
using CustomerFeedbackSystem.OLAP.Api.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Api.Endpoints;
public static class SocialCommentEndpoints
{
    public static void MapSocialCommentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/social-comments", GetPageAsync)
            .WithName("GetSocialComments")
            .WithSummary("Social comments, paged, oldest first.")
            .WithOpenApi()
            .Produces<PagedResponse<SocialCommentDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/health", CheckHealthAsync)
            .WithName("Health")
            .WithSummary("Reports whether the OLTP database is reachable.")
            .WithOpenApi();
    }

    private static async Task<IResult> GetPageAsync(
        OltpReadDbContext db,
        IOptions<PagingOptions> pagingOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int? pageSize = null)
    {
        var paging = pagingOptions.Value;

        if (page < 1)
        {
            return Results.BadRequest(new { error = "page must be 1 or greater." });
        }

        var requestedSize = pageSize ?? paging.DefaultPageSize;
        if (requestedSize < 1)
        {
            return Results.BadRequest(new { error = "pageSize must be 1 or greater." });
        }

        var effectiveSize = Math.Min(requestedSize, paging.MaxPageSize);

        try
        {
            var query =
                from cs in db.ComentariosSociales
                join com in db.Comentarios on cs.IdComentario equals com.IdComentario
                join cli in db.Clientes on cs.IdCliente equals cli.IdCliente
                join fs in db.FuentesSociales on cs.IdFuenteSocial equals fs.IdFuenteSocial
                select new { cs.IdComentarioSocial, cs.Fecha, cli.Nombre, Plataforma = fs.Nombre, com.Comentarios };

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
               
                .OrderBy(x => x.IdComentarioSocial)
                .Skip((page - 1) * effectiveSize)
                .Take(effectiveSize)
                .ToListAsync(cancellationToken);

            var response = new PagedResponse<SocialCommentDto>
            {
                Page = page,
                PageSize = effectiveSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)effectiveSize),
                Items = items.ConvertAll(x => new SocialCommentDto
                {
                    IdPost = $"CS{x.IdComentarioSocial:D6}",
                    UsuarioRedSocial = x.Nombre,
                    Plataforma = x.Plataforma,
                    FechaPost = x.Fecha,
                    TextoComentario = x.Comentarios,
                    Interacciones = null,
                }),
            };

            return Results.Ok(response);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Could not read social comments; SQL error {Number}.", ex.Number);
            return Results.Problem(
                detail: "The transactional database is not responding.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> CheckHealthAsync(OltpReadDbContext db, CancellationToken cancellationToken)
    {
        var reachable = await db.Database.CanConnectAsync(cancellationToken);

        return reachable
            ? Results.Ok(new { status = "healthy" })
            : Results.Problem(
                detail: "The transactional database is not reachable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
