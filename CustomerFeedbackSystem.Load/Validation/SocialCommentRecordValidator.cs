using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Validation;

public sealed class SocialCommentRecordValidator : IRecordValidator<SocialCommentCsvRecord>
{
    private const string File = "social_comments.csv";
    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);

    public Result<SocialCommentCsvRecord> Validate(SocialCommentCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (ValidationRules.IsBlank(record.IdComment))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdComment), "is required"));
        }
        else if (!_seenIds.Add(record.IdComment!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdComment!));
        }

     
        if (!ValidationRules.IsBlank(record.IdCliente) && !ValidationRules.MatchesPrefixedId(record.IdCliente, 'C'))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdCliente), "must look like 'C<digits>' when present"));
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


        return errors.Count == 0
            ? Result<SocialCommentCsvRecord>.Success(record)
            : Result<SocialCommentCsvRecord>.Failure(errors);
    }
}
