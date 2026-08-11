using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class FactResetService : IFactResetService
{
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

    public async Task<Result> ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync($"DELETE FROM {WarehouseTables.HechoOpinionPalabra};", cancellationToken);
            await ExecuteAsync($"DELETE FROM {WarehouseTables.HechoOpiniones};", cancellationToken);

            await SqlWarehouseWriter.ReseedAsync(
                _session, WarehouseTables.HechoOpiniones, 0, _options.CommandTimeoutSeconds, cancellationToken);

            _logger.LogInformation("Both fact tables emptied for a full refresh.");
            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fact reset failed; SQL error {Number}.", ex.Number);
            return Result.Failure(new FactLoadError("reset", ex.Message));
        }
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
