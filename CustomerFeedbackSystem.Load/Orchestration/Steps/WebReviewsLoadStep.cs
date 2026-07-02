using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Dto;
using CustomerFeedbackSystem.Load.Dto.Maps;
using CustomerFeedbackSystem.Load.Sources;
using CustomerFeedbackSystem.Load.Transformation;
using CustomerFeedbackSystem.Load.Validation;

namespace CustomerFeedbackSystem.Load.Orchestration.Steps;

public sealed class WebReviewsLoadStep
{
    private const string SourceName = "web_reviews.csv";

    public async Task RunAsync(
        string filePath,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds,
        EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<WebReviewCsvRecord, WebReviewCsvRecordMap>(filePath, SourceName);
        var validator = new WebReviewRecordValidator();
        var transformer = new WebReviewTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var comentarioIds = await context.IdentityAllocator.AllocateRangeAsync(
            "Comentarios", "IdComentario", validRows.Count, context.CancellationToken);
        var reseñaIds = await context.IdentityAllocator.AllocateRangeAsync(
            "Reseñas", "IdReview", validRows.Count, context.CancellationToken);

        var comentarios = new List<Comentario>(validRows.Count);
        var reseñas = new List<Reseña>(validRows.Count);

        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, rowNumber) = validRows[i];
            var result = transformer.Transform(record, rowNumber, comentarioIds[i], reseñaIds[i], clienteIds, productoIds);
            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            comentarios.Add(result.Value.Comentario);
            reseñas.Add(result.Value.Fact);
        }

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Comentarios",
            ["IdComentario", "Comentarios"], comentarios,
            c => [c.IdComentario, c.Comentarios], context.CancellationToken);

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Reseñas",
            ["IdReview", "IdCliente", "IdProducto", "IdCommentario", "Rating"], reseñas,
            r => [r.IdReview, r.IdCliente, r.IdProducto, r.IdCommentario, r.Rating], context.CancellationToken);

        stats.RecordInserted(reseñas.Count);
    }
}
