using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class DimensionResetService : IDimensionResetService
{
    private const string FactCountQuery = $"""
        SELECT
            (SELECT COUNT(*) FROM {DimensionTables.HechoOpiniones})      AS Opiniones,
            (SELECT COUNT(*) FROM {DimensionTables.HechoOpinionPalabra}) AS Palabras;
        """;

    private readonly SqlDimensionLoadSession _session;
    private readonly DimensionLoadOptions _options;
    private readonly ILogger<DimensionResetService> _logger;

    public DimensionResetService(
        SqlDimensionLoadSession session,
        IOptions<DimensionLoadOptions> options,
        ILogger<DimensionResetService> logger)
    {
        _session = session;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var guard = await EnsureNoFactsAsync(cancellationToken);
            if (guard.IsFailure)
            {
                return guard;
            }

            // TRUNCATE is not an option: every one of these tables is referenced by a foreign key
            // from Hechos, and SQL Server refuses to truncate a referenced table even when the
            // referencing one is empty.
            foreach (var (table, identityColumn) in DimensionTables.IdentityDimensions)
            {
                await ExecuteAsync($"DELETE FROM {table};", cancellationToken);
                await SqlDimensionWriter.ReseedAsync(
                    _session, table, 0, _options.CommandTimeoutSeconds, cancellationToken);

                _logger.LogDebug("{Table} emptied and {Column} reseeded.", table, identityColumn);
            }

            await ExecuteAsync($"DELETE FROM {DimensionTables.DimFecha};", cancellationToken);

            _logger.LogInformation("All six dimensions emptied for a full refresh.");
            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Dimension reset failed; SQL error {Number}.", ex.Number);
            return Result.Failure(new DimensionLoadError("reset", ex.Message));
        }
    }

    // A full dimension refresh reassigns every surrogate key, which would leave any existing fact
    // row pointing at the wrong member. Refusing is better than silently corrupting or silently
    // deleting somebody else's data.
    private async Task<Result> EnsureNoFactsAsync(CancellationToken cancellationToken)
    {
        await using var command = _session.Connection.CreateCommand();
        command.Transaction = _session.Transaction;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.CommandText = FactCountQuery;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Result.Success();
        }

        var opiniones = reader.GetInt32(0);
        var palabras = reader.GetInt32(1);

        if (opiniones == 0 && palabras == 0)
        {
            return Result.Success();
        }

        return Result.Failure(new PreconditionError(
            "empty fact tables",
            $"{DimensionTables.HechoOpiniones} has {opiniones} row(s) and "
            + $"{DimensionTables.HechoOpinionPalabra} has {palabras}. Reloading the dimensions "
            + "reassigns every surrogate key, so the facts must be emptied first."));
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
