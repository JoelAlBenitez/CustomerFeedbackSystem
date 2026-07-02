using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Common;
using CustomerFeedbackSystem.Load.Common.Errors;
using CustomerFeedbackSystem.Load.Dto;

namespace CustomerFeedbackSystem.Load.Transformation;

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
