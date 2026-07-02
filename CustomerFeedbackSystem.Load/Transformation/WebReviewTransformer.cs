using System.Globalization;
using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Transformation;

public sealed class WebReviewTransformer
{
    private const string SourceFile = "web_reviews.csv";

    public Result<(Comentario Comentario, Reseña Fact)> Transform(
        WebReviewCsvRecord record,
        int rowNumber,
        int comentarioId,
        int factId,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds)
    {
        var clienteCsvId = IdParsing.ExtractNumericId(record.IdCliente!);
        if (!clienteIds.TryGetValue(clienteCsvId, out var idCliente))
        {
            return Result<(Comentario, Reseña)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdCliente), record.IdCliente!));
        }

        var productoCsvId = IdParsing.ExtractNumericId(record.IdProducto!);
        if (!productoIds.TryGetValue(productoCsvId, out var idProducto))
        {
            return Result<(Comentario, Reseña)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdProducto), record.IdProducto!));
        }

        int.TryParse(record.Rating, NumberStyles.None, CultureInfo.InvariantCulture, out var rating);

        var comentario = new Comentario { IdComentario = comentarioId, Comentarios = record.Comentario!.Trim() };
        var fact = new Reseña
        {
            IdReview = factId,
            IdCliente = idCliente,
            IdProducto = idProducto,
            IdCommentario = comentarioId,
            Rating = rating,
        };

        return Result<(Comentario, Reseña)>.Success((comentario, fact));
    }
}
