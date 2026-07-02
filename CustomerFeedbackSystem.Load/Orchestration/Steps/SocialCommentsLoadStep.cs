using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Dto;
using CustomerFeedbackSystem.Load.Dto.Maps;
using CustomerFeedbackSystem.Load.Sources;
using CustomerFeedbackSystem.Load.Transformation;
using CustomerFeedbackSystem.Load.Validation;

namespace CustomerFeedbackSystem.Load.Orchestration.Steps;

public sealed class SocialCommentsLoadStep
{
    private const string SourceName = "social_comments.csv";

    public async Task RunAsync(
        string filePath,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds,
        int sentinelClienteId,
        EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<SocialCommentCsvRecord, SocialCommentCsvRecordMap>(filePath, SourceName);
        var validator = new SocialCommentRecordValidator();
        var transformer = new SocialCommentTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var fuenteNames = LookupCatalogBuilder.DistinctValues(validRows, r => TextNormalization.OrSentinel(r.Record.Fuente));
        var fuenteSocialIds = await LoadFuentesSocialesAsync(fuenteNames, context);

        var comentarioIds = await context.IdentityAllocator.AllocateRangeAsync(
            "Comentarios", "IdComentario", validRows.Count, context.CancellationToken);
        var factIds = await context.IdentityAllocator.AllocateRangeAsync(
            "ComentariosSociales", "IdComentarioSocial", validRows.Count, context.CancellationToken);

        var comentarios = new List<Comentario>();
        var facts = new List<ComentariosSociale>();

        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, rowNumber) = validRows[i];
            var result = transformer.Transform(
                record, rowNumber, comentarioIds[i], factIds[i], sentinelClienteId,
                clienteIds, productoIds, fuenteSocialIds);

            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            comentarios.Add(result.Value.Comentario);
            facts.Add(result.Value.Fact);
        }

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Comentarios",
            ["IdComentario", "Comentarios"], comentarios,
            c => [c.IdComentario, c.Comentarios], context.CancellationToken);

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "ComentariosSociales",
            ["IdComentarioSocial", "IdComentario", "IdCliente", "IdProducto", "IdFuenteSocial", "Fecha"], facts,
            f => [f.IdComentarioSocial, f.IdComentario, f.IdCliente, f.IdProducto, f.IdFuenteSocial, f.Fecha],
            context.CancellationToken);

        stats.RecordInserted(facts.Count);
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadFuentesSocialesAsync(
        IReadOnlyList<string> fuenteNames, EtlRunContext context)
    {
        var ids = await context.IdentityAllocator.AllocateRangeAsync(
            "FuentesSociales", "IdFuenteSocial", fuenteNames.Count, context.CancellationToken);

        var fuentes = fuenteNames
            .Select((name, i) => new FuentesSociale { IdFuenteSocial = ids[i], Nombre = TextNormalization.Truncate(name, 50) })
            .ToList();

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "FuentesSociales",
            ["IdFuenteSocial", "Nombre"], fuentes,
            f => [f.IdFuenteSocial, f.Nombre], context.CancellationToken);

        return LookupCatalogBuilder.ToIdMap(fuenteNames, ids);
    }
}
