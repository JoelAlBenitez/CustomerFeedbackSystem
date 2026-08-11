using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class FactResetService : IFactResetService
{
    private const string FactCountQuery = $"""
        SELECT
            (SELECT COUNT_BIG(*) FROM {WarehouseTables.HechoOpiniones})      AS Opiniones,
            (SELECT COUNT_BIG(*) FROM {WarehouseTables.HechoOpinionPalabra}) AS Palabras;
        """;

    private readonly SqlWarehouseLoadSession _session;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<FactResetService> _logger;

    public FactResetService(
        SqlWarehouseLoadSession session,
        IOptions<DimensionLoadOptions> options,
        ILogger<FactResetService> logger)
    {
        _session = session;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<FactResetOutcome>> ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await CountAsync(cancellationToken);

            if (!existing.Cleared)
            {
                _logger.LogInformation("Both fact tables are already empty; nothing to delete before inserting.");
                return Result<FactResetOutcome>.Success(existing);
            }

            await ExecuteAsync($"DELETE FROM {WarehouseTables.HechoOpinionPalabra};", cancellationToken);
            await ExecuteAsync($"DELETE FROM {WarehouseTables.HechoOpiniones};", cancellationToken);

            await SqlWarehouseWriter.ReseedAsync(
                _session, WarehouseTables.HechoOpiniones, 0, _options.CommandTimeoutSeconds, cancellationToken);

            _logger.LogInformation(
                "Fact tables emptied for a full refresh: {Opiniones} opinion(s) and {Palabras} keyword link(s) removed.",
                existing.Opiniones, existing.Palabras);

            return Result<FactResetOutcome>.Success(existing);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fact reset failed; SQL error {Number}.", ex.Number);
            return Result<FactResetOutcome>.Failure(new FactLoadError("reset", ex.Message));
        }
    }

    private async Task<FactResetOutcome> CountAsync(CancellationToken cancellationToken)
    {
        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = FactCountQuery;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? new FactResetOutcome(reader.GetInt64(0), reader.GetInt64(1))
            : FactResetOutcome.Empty;
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
