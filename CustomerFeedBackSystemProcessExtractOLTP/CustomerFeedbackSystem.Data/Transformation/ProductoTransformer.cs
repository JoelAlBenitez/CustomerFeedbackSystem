using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Common;
using CustomerFeedbackSystem.Data.Common.Errors;
using CustomerFeedbackSystem.Data.Dto;

namespace CustomerFeedbackSystem.Data.Transformation;

public sealed class ProductoTransformer
{
    private const string SourceFile = "products.csv";

    public Result<Producto> Transform(
        ProductoCsvRecord record,
        int rowNumber,
        int assignedId,
        IReadOnlyDictionary<string, int> categoriaIds)
    {
        var categoriaKey = TextNormalization.OrSentinel(record.Categoria);
        if (!categoriaIds.TryGetValue(categoriaKey, out var idCategoria))
        {
            return Result<Producto>.Failure(
                new ReferentialIntegrityError(SourceFile, rowNumber, nameof(record.Categoria), categoriaKey));
        }

        var nombre = TextNormalization.Truncate(TextNormalization.OrSentinel(record.Nombre), 100);

        return Result<Producto>.Success(new Producto
        {
            IdProducto = assignedId,
            IdCategoria = idCategoria,
            Nombre = nombre,
        });
    }
}
