using System.Data;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;

public sealed class SqlBulkStagingWriter<T> : IStagingWriter<T>
{
    private const int BulkCopyTimeoutSeconds = 300;

    private readonly StagingTableDescriptor<T> _descriptor;
    private readonly string _connectionString;
    private readonly ILogger<SqlBulkStagingWriter<T>> _logger;

    public SqlBulkStagingWriter(
        StagingTableDescriptor<T> descriptor,
        IOptions<OlapConnectionOptions> connectionOptions,
        ILogger<SqlBulkStagingWriter<T>> logger)
    {
        _descriptor = descriptor;
        _connectionString = connectionOptions.Value.ConnectionString;
        _logger = logger;
    }

    public string TableName => _descriptor.QualifiedName;

    public async Task<Result<int>> WriteBatchAsync(
        IReadOnlyCollection<T> batch,
        CancellationToken cancellationToken = default)
    {
        if (batch.Count == 0)
        {
            return Result<int>.Success(0);
        }

        using var table = new DataTable();
        foreach (var column in _descriptor.ColumnNames)
        {
            table.Columns.Add(column);
        }

        foreach (var row in batch)
        {
            table.Rows.Add(_descriptor.ValueSelector(row));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
              
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction)
                {
                    DestinationTableName = _descriptor.QualifiedName,
                    BatchSize = batch.Count,
                    BulkCopyTimeout = BulkCopyTimeoutSeconds,
                };

              
                foreach (var column in _descriptor.ColumnNames)
                {
                    bulkCopy.ColumnMappings.Add(column, column);
                }

                await bulkCopy.WriteToServerAsync(table, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogDebug("Batch of {Count} rows written to {Table}.", batch.Count, TableName);
                return Result<int>.Success(batch.Count);
            }
            catch (SqlException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (SqlException ex)
        {
           
            _logger.LogError(ex, "Bulk write to {Table} failed with SQL error {Number}.", TableName, ex.Number);

         
            return Result<int>.Failure(new StagingWriteError(TableName, ex.Message));
        }
    }
}
