using System.Globalization;
using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Transformation;

public sealed class SurveyTransformer
{
    private const string SourceFile = "surveys_part1.csv";

    /// <summary>Shared by the catalog builder and the resolver so both always agree on the key shape.</summary>
    public static string BuildClasificacionKey(string clasificacion, string puntajeSatisfaccion) =>
        $"{clasificacion.Trim()}|{puntajeSatisfaccion.Trim()}";

    public Result<(Comentario Comentario, Encuesta Fact)> Transform(
        SurveyCsvRecord record,
        int rowNumber,
        int comentarioId,
        int factId,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds,
        IReadOnlyDictionary<string, int> clasificacionIds,
        IReadOnlyDictionary<string, int> fuenteEncuestaIds)
    {
        // Most rows in this file carry fabricated ids (see surveys_part1.csv analysis):
        // this is where the vast majority are expected to be rejected for real.
        var clienteCsvId = IdParsing.ExtractNumericId(record.IdCliente!);
        if (!clienteIds.TryGetValue(clienteCsvId, out var idCliente))
        {
            return Result<(Comentario, Encuesta)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdCliente), record.IdCliente!));
        }

        var productoCsvId = IdParsing.ExtractNumericId(record.IdProducto!);
        if (!productoIds.TryGetValue(productoCsvId, out var idProducto))
        {
            return Result<(Comentario, Encuesta)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.IdProducto), record.IdProducto!));
        }

        var clasificacionKey = BuildClasificacionKey(record.Clasificacion!, record.PuntajeSatisfaccion!);
        if (!clasificacionIds.TryGetValue(clasificacionKey, out var idClasificacion))
        {
            return Result<(Comentario, Encuesta)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.Clasificacion), clasificacionKey));
        }

        var fuenteKey = TextNormalization.OrSentinel(record.Fuente);
        if (!fuenteEncuestaIds.TryGetValue(fuenteKey, out var idFuenteEncuesta))
        {
            return Result<(Comentario, Encuesta)>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.Fuente), fuenteKey));
        }

        DateTime.TryParse(record.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha);

        var comentario = new Comentario { IdComentario = comentarioId, Comentarios = record.Comentario!.Trim() };
        var fact = new Encuesta
        {
            IdOpinion = factId,
            IdCliente = idCliente,
            IdComentario = comentarioId,
            IdClasificacion = idClasificacion,
            IdFuenteEncuestas = idFuenteEncuesta,
            Fecha = fecha,
        };

        return Result<(Comentario, Encuesta)>.Success((comentario, fact));
    }
}
