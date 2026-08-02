using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Dto;
using CustomerFeedbackSystem.Data.Dto.Maps;
using CustomerFeedbackSystem.Data.Sources;
using CustomerFeedbackSystem.Data.Transformation;
using CustomerFeedbackSystem.Data.Validation;

namespace CustomerFeedbackSystem.Data.Orchestration.Steps;

public sealed record ProductosLoadResult(IReadOnlyDictionary<int, int> ProductoIds);

public sealed class ProductosLoadStep
{
    private const string SourceName = "products.csv";

    public async Task<ProductosLoadResult> RunAsync(string filePath, EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<ProductoCsvRecord, ProductoCsvRecordMap>(filePath, SourceName);
        var validator = new ProductoRecordValidator();
        var transformer = new ProductoTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var categoriaNames = LookupCatalogBuilder.DistinctValues(validRows, r => TextNormalization.OrSentinel(r.Record.Categoria));
        var categoriaIds = await LoadCategoriasAsync(categoriaNames, context);

        var productoIdsAssigned = await context.IdentityAllocator.AllocateRangeAsync(
            "Productos", "IdProducto", validRows.Count, context.CancellationToken);

        var productos = new List<Producto>(validRows.Count);
        var productoIds = new Dictionary<int, int>(validRows.Count);

        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, rowNumber) = validRows[i];
            var result = transformer.Transform(record, rowNumber, productoIdsAssigned[i], categoriaIds);
            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            productos.Add(result.Value);
            productoIds[int.Parse(record.IdProducto!)] = result.Value.IdProducto;
        }

        await context.BulkLoader.BulkInsertAsync(
            context.Connection,
            context.Transaction,
            "Productos",
            ["IdProducto", "IdCategoria", "Nombre"],
            productos,
            p => [p.IdProducto, p.IdCategoria, p.Nombre],
            context.CancellationToken);

        stats.RecordInserted(productos.Count);

        return new ProductosLoadResult(productoIds);
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadCategoriasAsync(
        IReadOnlyList<string> categoriaNames, EtlRunContext context)
    {
        var ids = await context.IdentityAllocator.AllocateRangeAsync(
            "Categorias", "IdCategoria", categoriaNames.Count, context.CancellationToken);

        var categorias = categoriaNames
            .Select((name, i) => new Categoria { IdCategoria = ids[i], Nombre = TextNormalization.Truncate(name, 50) })
            .ToList();

        await context.BulkLoader.BulkInsertAsync(
            context.Connection,
            context.Transaction,
            "Categorias",
            ["IdCategoria", "Nombre"],
            categorias,
            c => [c.IdCategoria, c.Nombre],
            context.CancellationToken);

        return LookupCatalogBuilder.ToIdMap(categoriaNames, ids);
    }
}
