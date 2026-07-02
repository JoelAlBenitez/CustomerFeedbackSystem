using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Load.Dto;
using CustomerFeedbackSystem.Load.Dto.Maps;
using CustomerFeedbackSystem.Load.Sources;
using CustomerFeedbackSystem.Load.Transformation;
using CustomerFeedbackSystem.Load.Validation;

namespace CustomerFeedbackSystem.Load.Orchestration.Steps;
public sealed class SurveysLoadStep
{
    private const string SourceName = "surveys_part1.csv";

    public async Task RunAsync(
        string filePath,
        IReadOnlyDictionary<int, int> clienteIds,
        IReadOnlyDictionary<int, int> productoIds,
        EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<SurveyCsvRecord, SurveyCsvRecordMap>(filePath, SourceName);
        var validator = new SurveyRecordValidator();
        var transformer = new SurveyTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var clasificacionIds = await LoadClasificacionesAsync(validRows, context);

        var fuenteNames = LookupCatalogBuilder.DistinctValues(validRows, r => TextNormalization.OrSentinel(r.Record.Fuente));
        var fuenteEncuestaIds = await LoadFuenteEncuestasAsync(fuenteNames, context);

        var comentarioIds = await context.IdentityAllocator.AllocateRangeAsync(
            "Comentarios", "IdComentario", validRows.Count, context.CancellationToken);
        var encuestaIds = await context.IdentityAllocator.AllocateRangeAsync(
            "Encuestas", "IdOpinion", validRows.Count, context.CancellationToken);

        var comentarios = new List<Comentario>();
        var encuestas = new List<Encuesta>();

        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, rowNumber) = validRows[i];
            var result = transformer.Transform(
                record, rowNumber, comentarioIds[i], encuestaIds[i],
                clienteIds, productoIds, clasificacionIds, fuenteEncuestaIds);

            if (result.IsFailure)
            {
                foreach (var error in result.Errors)
                {
                    stats.RecordRejected(error);
                }

                continue;
            }

            comentarios.Add(result.Value.Comentario);
            encuestas.Add(result.Value.Fact);
        }

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Comentarios",
            ["IdComentario", "Comentarios"], comentarios,
            c => [c.IdComentario, c.Comentarios], context.CancellationToken);

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Encuestas",
            ["IdOpinion", "IdCliente", "IdComentario", "IdClasificacion", "IdFuenteEncuestas", "Fecha"], encuestas,
            e => [e.IdOpinion, e.IdCliente, e.IdComentario, e.IdClasificacion, e.IdFuenteEncuestas, e.Fecha],
            context.CancellationToken);

        stats.RecordInserted(encuestas.Count);
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadClasificacionesAsync(
        IReadOnlyList<(SurveyCsvRecord Record, int RowNumber)> validRows, EtlRunContext context)
    {
        var distinctPairs = validRows
            .Select(r => (Clasificacion: r.Record.Clasificacion!.Trim(), Puntaje: r.Record.PuntajeSatisfaccion!.Trim()))
            .Distinct()
            .OrderBy(p => p.Clasificacion, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Puntaje, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ids = await context.IdentityAllocator.AllocateRangeAsync(
            "Clasificaciones", "IdClasificacion", distinctPairs.Count, context.CancellationToken);

        var clasificaciones = distinctPairs
            .Select((p, i) => new Clasificacione
            {
                IdClasificacion = ids[i],
                Clasificacion = p.Clasificacion,
                PuntajeSastifacion = int.Parse(p.Puntaje),
            })
            .ToList();

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "Clasificaciones",
            ["IdClasificacion", "Clasificacion", "PuntajeSastifacion"], clasificaciones,
            c => [c.IdClasificacion, c.Clasificacion, c.PuntajeSastifacion], context.CancellationToken);

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < distinctPairs.Count; i++)
        {
            map[SurveyTransformer.BuildClasificacionKey(distinctPairs[i].Clasificacion, distinctPairs[i].Puntaje)] = ids[i];
        }

        return map;
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadFuenteEncuestasAsync(
        IReadOnlyList<string> fuenteNames, EtlRunContext context)
    {
        var ids = await context.IdentityAllocator.AllocateRangeAsync(
            "FuenteEncuestas", "IdFuenteEncuestas", fuenteNames.Count, context.CancellationToken);

        var fuentes = fuenteNames
            .Select((name, i) => new FuenteEncuesta { IdFuenteEncuestas = ids[i], Fuente = TextNormalization.Truncate(name, 50) })
            .ToList();

        await context.BulkLoader.BulkInsertAsync(
            context.Connection, context.Transaction, "FuenteEncuestas",
            ["IdFuenteEncuestas", "Fuente"], fuentes,
            f => [f.IdFuenteEncuestas, f.Fuente], context.CancellationToken);

        return LookupCatalogBuilder.ToIdMap(fuenteNames, ids);
    }
}
