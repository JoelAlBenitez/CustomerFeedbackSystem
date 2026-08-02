using CustomerFeedbackSystem.Data.Common;
using CustomerFeedbackSystem.Data.Common.Errors;
using CustomerFeedbackSystem.Data.Dto;

namespace CustomerFeedbackSystem.Data.Validation;

public sealed class SurveyRecordValidator : IRecordValidator<SurveyCsvRecord>
{
    private const string File = "surveys_part1.csv";
    private static readonly HashSet<string> KnownClasificaciones = new(StringComparer.OrdinalIgnoreCase)
    {
        "Positiva", "Negativa", "Neutra",
    };

    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);

    public Result<SurveyCsvRecord> Validate(SurveyCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (ValidationRules.IsBlank(record.IdOpinion))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdOpinion), "is required"));
        }
        else if (!_seenIds.Add(record.IdOpinion!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdOpinion!));
        }

        if (!ValidationRules.IsPositiveInt(record.IdCliente, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdCliente), "must be a positive integer"));
        }

        if (!ValidationRules.IsPositiveInt(record.IdProducto, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdProducto), "must be a positive integer"));
        }

        if (!ValidationRules.IsValidDate(record.Fecha, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Fecha), "is not a valid date"));
        }

        if (ValidationRules.IsBlank(record.Comentario))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Comentario), "is required"));
        }

        if (ValidationRules.IsBlank(record.Clasificacion) || !KnownClasificaciones.Contains(record.Clasificacion!))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Clasificacion), "must be one of Positiva/Negativa/Neutra"));
        }

        if (!ValidationRules.IsIntInRange(record.PuntajeSatisfaccion, 1, 5, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.PuntajeSatisfaccion), "must be an integer between 1 and 5"));
        }

        // Blank Fuente is not rejected: Transformation applies the "-" sentinel.

        return errors.Count == 0
            ? Result<SurveyCsvRecord>.Success(record)
            : Result<SurveyCsvRecord>.Failure(errors);
    }
}
