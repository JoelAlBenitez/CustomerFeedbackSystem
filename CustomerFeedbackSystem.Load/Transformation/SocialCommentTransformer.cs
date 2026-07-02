using System.Globalization;
using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Transformation;

public sealed class SocialCommentTransformer
{
    private const string SourceFile = "social_comments.csv";

    public Result<(Comentario Comentario, ComentariosSociale Fact)> Transform(
        SocialCommentCsvRecord record,
        int rowNumber,
        int comentarioId,
        int factId,
        int sentinelClienteId,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds,
        IReadOnlyDictionary<string, int> fuenteSocialIds)
    {
        int idCliente;
        if (string.IsNullOrWhiteSpace(record.IdCliente))
        {
            // Missing data, not a fabricated reference: map to the one shared
            // "Cliente Desconocido" catalog row instead of rejecting the row.
            idCliente = sentinelClienteId;
        }
        else
        {
            var clienteCsvId = IdParsing.ExtractNumericId(record.IdCliente);
            if (!clienteIds.TryGetValue(clienteCsvId, out idCliente))
            {
                return Result<(Comentario, ComentariosSociale)>.Failure(
                    new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdCliente), record.IdCliente));
            }
        }

        var productoCsvId = IdParsing.ExtractNumericId(record.IdProducto!);
        if (!productoIds.TryGetValue(productoCsvId, out var idProducto))
        {
            return Result<(Comentario, ComentariosSociale)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdProducto), record.IdProducto!));
        }

        var fuenteKey = TextNormalization.OrSentinel(record.Fuente);
        if (!fuenteSocialIds.TryGetValue(fuenteKey, out var idFuenteSocial))
        {
            return Result<(Comentario, ComentariosSociale)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.Fuente), fuenteKey));
        }

        DateTime.TryParse(record.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha);

        var comentario = new Comentario { IdComentario = comentarioId, Comentarios = record.Comentario!.Trim() };
        var fact = new ComentariosSociale
        {
            IdComentarioSocial = factId,
            IdComentario = comentarioId,
            IdCliente = idCliente,
            IdProducto = idProducto,
            IdFuenteSocial = idFuenteSocial,
            Fecha = fecha,
        };

        return Result<(Comentario, ComentariosSociale)>.Success((comentario, fact));
    }
}
