using System.Globalization;
using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Common;
using CustomerFeedbackSystem.Data.Common.Errors;
using CustomerFeedbackSystem.Data.Dto;

namespace CustomerFeedbackSystem.Data.Transformation;

public sealed class FuenteDatoTransformer
{
    private const string SourceFile = "fuente_datos.csv";

    public Result<FuentesDato> Transform(
        FuenteDatoCsvRecord record,
        int rowNumber,
        int assignedId,
        IReadOnlyDictionary<string, int> tipoFuenteIds)
    {
        var tipoKey = TextNormalization.OrSentinel(record.TipoFuente);
        if (!tipoFuenteIds.TryGetValue(tipoKey, out var idTipoFuente))
        {
            return Result<FuentesDato>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.TipoFuente), tipoKey));
        }

        DateTime.TryParse(record.FechaCarga, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaCarga);

        return Result<FuentesDato>.Success(new FuentesDato
        {
            IdFuenteDatos = assignedId,
            IdTipoFuentes = idTipoFuente,
            FechaCarga = fechaCarga,
        });
    }
}
