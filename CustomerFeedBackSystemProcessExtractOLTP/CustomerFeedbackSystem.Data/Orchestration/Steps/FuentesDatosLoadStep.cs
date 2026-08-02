using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Dto;
using CustomerFeedbackSystem.Data.Dto.Maps;
using CustomerFeedbackSystem.Data.Sources;
using CustomerFeedbackSystem.Data.Transformation;
using CustomerFeedbackSystem.Data.Validation;

namespace CustomerFeedbackSystem.Data.Orchestration.Steps;


public sealed class FuentesDatosLoadStep
{
    private const string SourceName = "fuente_datos.csv";

    public async Task RunAsync(string filePath, EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<FuenteDatoCsvRecord, FuenteDatoCsvRecordMap>(filePath, SourceName);
        var validator = new FuenteDatoRecordValidator();
        var transformer = new FuenteDatoTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var tipoFuenteNames = LookupCatalogBuilder.DistinctValues(validRows, r => TextNormalization.OrSentinel(r.Record.TipoFuente));
        var tipoFuenteIds = await LoadTipoFuentesAsync(tipoFuenteNames, context);

        var assignedIds = await context.IdentityAllocator.AllocateRangeAsync(
            "FuentesDatos", "IdFuenteDatos", validRows.Count, context.CancellationToken);

        var fuentesDatos = new List<FuentesDato>(validRows.Count);
        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, rowNumber) = validRows[i];
            var result = transformer.Transform(record, rowNumber, assignedIds[i], tipoFuenteIds);
            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            fuentesDatos.Add(result.Value);
        }

        await context.BulkLoader.BulkInsertAsync(
            context.Connection,
            context.Transaction,
            "FuentesDatos",
            ["IdFuenteDatos", "IdTipoFuentes", "FechaCarga"],
            fuentesDatos,
            f => [f.IdFuenteDatos, f.IdTipoFuentes, f.FechaCarga],
            context.CancellationToken);

        stats.RecordInserted(fuentesDatos.Count);
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadTipoFuentesAsync(
        IReadOnlyList<string> tipoFuenteNames, EtlRunContext context)
    {
        var ids = await context.IdentityAllocator.AllocateRangeAsync(
            "TipoFuentesDatos", "IdTipoFuentes", tipoFuenteNames.Count, context.CancellationToken);

        var tiposFuente = tipoFuenteNames
            .Select((name, i) => new TipoFuentesDato { IdTipoFuentes = ids[i], TipoFuente = TextNormalization.Truncate(name, 50) })
            .ToList();

        await context.BulkLoader.BulkInsertAsync(
            context.Connection,
            context.Transaction,
            "TipoFuentesDatos",
            ["IdTipoFuentes", "TipoFuente"],
            tiposFuente,
            t => [t.IdTipoFuentes, t.TipoFuente],
            context.CancellationToken);

        return LookupCatalogBuilder.ToIdMap(tipoFuenteNames, ids);
    }
}
