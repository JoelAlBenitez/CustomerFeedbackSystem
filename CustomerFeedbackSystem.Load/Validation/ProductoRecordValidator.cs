using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Validation;

public sealed class ProductoRecordValidator : IRecordValidator<ProductoCsvRecord>
{
    private const string File = "products.csv";
    private readonly HashSet<string> _seenIds = new(StringComparer.OrdinalIgnoreCase);

    public Result<ProductoCsvRecord> Validate(ProductoCsvRecord record, int rowNumber, string sourceFile)
    {
        var errors = new List<Error>();

        if (!ValidationRules.IsPositiveInt(record.IdProducto, out _))
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.IdProducto), "must be a positive integer"));
        }
        else if (!_seenIds.Add(record.IdProducto!))
        {
            errors.Add(new DuplicateRecordError(File, rowNumber, record.IdProducto!));
        }

        if (!ValidationRules.IsBlank(record.Nombre) && record.Nombre!.Length > 100)
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Nombre), "exceeds 100 characters"));
        }

        if (!ValidationRules.IsBlank(record.Categoria) && record.Categoria!.Length > 50)
        {
            errors.Add(new ValidationError(File, rowNumber, nameof(record.Categoria), "exceeds 50 characters"));
        }


        return errors.Count == 0
            ? Result<ProductoCsvRecord>.Success(record)
            : Result<ProductoCsvRecord>.Failure(errors);
    }
}
