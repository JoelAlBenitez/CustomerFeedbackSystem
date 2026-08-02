using CustomerFeedbackSystem.Data.Models;
using CustomerFeedbackSystem.Data.Dto;
using CustomerFeedbackSystem.Data.Dto.Maps;
using CustomerFeedbackSystem.Data.Sources;
using CustomerFeedbackSystem.Data.Transformation;
using CustomerFeedbackSystem.Data.Validation;

namespace CustomerFeedbackSystem.Data.Orchestration.Steps;

public sealed record ClientesLoadResult(IReadOnlyDictionary<int, int> ClienteIds, int SentinelClienteId);

public sealed class ClientesLoadStep
{
    private const string SourceName = "clients.csv";

    public async Task<ClientesLoadResult> RunAsync(string filePath, EtlRunContext context)
    {
        var stats = context.Report.ForSource(SourceName);
        var reader = new CsvDataSourceReader<ClienteCsvRecord, ClienteCsvRecordMap>(filePath, SourceName);
        var validator = new ClienteRecordValidator();
        var transformer = new ClienteTransformer();

        var validRows = await SourceLoader.ReadAndValidateAsync(reader, validator, stats, SourceName, context.CancellationToken);

        var ids = await context.IdentityAllocator.AllocateRangeAsync("Clientes", "IdCliente", validRows.Count + 1, context.CancellationToken);
        var sentinelId = ids[^1];

        var clientes = new List<Cliente>(validRows.Count + 1);
        var clienteIds = new Dictionary<int, int>(validRows.Count);

        for (var i = 0; i < validRows.Count; i++)
        {
            var (record, _) = validRows[i];
            var entity = transformer.Transform(record, ids[i]).Value;
            clientes.Add(entity);
            clienteIds[int.Parse(record.IdCliente!)] = entity.IdCliente;
        }

        clientes.Add(ClienteTransformer.BuildSentinel(sentinelId));

        await context.BulkLoader.BulkInsertAsync(
            context.Connection,
            context.Transaction,
            "Clientes",
            ["IdCliente", "Nombre", "Email"],
            clientes,
            c => [c.IdCliente, c.Nombre, c.Email],
            context.CancellationToken);

        stats.RecordInserted(clientes.Count);

        return new ClientesLoadResult(clienteIds, sentinelId);
    }
}
