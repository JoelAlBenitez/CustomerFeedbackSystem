using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Validation;

public sealed class ClienteRecordValidator : IRecordValidator<ClienteCsvRecord>
{
    private const string File = "clients.csv";
    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _seenEmails = new(StringComparer.OrdinalIgnoreCase);

    public Result<ClienteCsvRecord> Validate(ClienteCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (!ValidationRules.IsPositiveInt(record.IdCliente, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdCliente), "must be a positive integer"));
        }
        else if (!_seenIds.Add(record.IdCliente!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdCliente!));
        }

        if (ValidationRules.IsBlank(record.Email) || !record.Email!.Contains('@') || !record.Email.Contains('.'))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Email), "must be a valid, non-empty email"));
        }
        else if (!_seenEmails.Add(record.Email))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.Email));
        }

        return errors.Count == 0
            ? Result<ClienteCsvRecord>.Success(record)
            : Result<ClienteCsvRecord>.Failure(errors);
    }
}
