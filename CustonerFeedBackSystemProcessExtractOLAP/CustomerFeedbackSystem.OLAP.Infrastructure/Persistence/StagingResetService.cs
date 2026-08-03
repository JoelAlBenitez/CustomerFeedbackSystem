using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

public sealed class StagingResetService : IStagingResetService
{
    private const int CommandTimeoutSeconds = 120;

    private readonly string _connectionString;
    private readonly ILogger<StagingResetService> _logger;

    public StagingResetService(
        IOptions<OlapConnectionOptions> connectionOptions,
        ILogger<StagingResetService> logger)
    {
        _connectionString = connectionOptions.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<Result> ResetAsync(string tableName, CancellationToken cancellationToken = default)
    {
       
        if (!StagingDescriptors.AllQualifiedNames.Contains(tableName))
        {
            return Result.Failure(new StagingWriteError(tableName, "table is not a known staging table"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandTimeout = CommandTimeoutSeconds;

        
            command.CommandText = $"TRUNCATE TABLE {tableName}";
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Staging table {Table} truncated for a full refresh.", tableName);
            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not truncate {Table}; SQL error {Number}.", tableName, ex.Number);
            return Result.Failure(new StagingWriteError(tableName, ex.Message));
        }
    }
}
