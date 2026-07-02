using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Validation;

public sealed class WebReviewRecordValidator : IRecordValidator<WebReviewCsvRecord>
{
    private const string File = "web_reviews.csv";
    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);

    public Result<WebReviewCsvRecord> Validate(WebReviewCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (ValidationRules.IsBlank(record.IdReview))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdReview), "is required"));
        }
        else if (!_seenIds.Add(record.IdReview!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdReview!));
        }

        if (!ValidationRules.MatchesPrefixedId(record.IdCliente, 'C'))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdCliente), "must look like 'C<digits>'"));
        }

        if (!ValidationRules.MatchesPrefixedId(record.IdProducto, 'P'))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdProducto), "must look like 'P<digits>'"));
        }

        if (!ValidationRules.IsValidDate(record.Fecha, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Fecha), "is not a valid date"));
        }

        if (ValidationRules.IsBlank(record.Comentario))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Comentario), "is required"));
        }

        if (!ValidationRules.IsIntInRange(record.Rating, 1, 5, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Rating), "must be an integer between 1 and 5"));
        }

        return errors.Count == 0
            ? Result<WebReviewCsvRecord>.Success(record)
            : Result<WebReviewCsvRecord>.Failure(errors);
    }
}
