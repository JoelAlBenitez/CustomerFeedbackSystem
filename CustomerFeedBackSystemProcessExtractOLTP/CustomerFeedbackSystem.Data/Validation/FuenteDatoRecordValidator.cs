using CustomerFeedbackSystem.Data.Common;
using CustomerFeedbackSystem.Data.Common.Errors;
using CustomerFeedbackSystem.Data.Dto;

namespace CustomerFeedbackSystem.Data.Validation;

public sealed class FuenteDatoRecordValidator : IRecordValidator<FuenteDatoCsvRecord>
{
    private const string File = "fuente_datos.csv";
    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);

    public Result<FuenteDatoCsvRecord> Validate(FuenteDatoCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (ValidationRules.IsBlank(record.IdFuente))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdFuente), "is required"));
        }
        else if (!_seenIds.Add(record.IdFuente!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdFuente!));
        }

        if (!ValidationRules.IsValidDate(record.FechaCarga, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.FechaCarga), "is not a valid date"));
        }


        return errors.Count == 0
            ? Result<FuenteDatoCsvRecord>.Success(record)
            : Result<FuenteDatoCsvRecord>.Failure(errors);
    }
}
