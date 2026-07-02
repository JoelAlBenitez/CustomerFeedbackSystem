using System.Data;
using Microsoft.Data.SqlClient;

namespace CustomerFeedbackSystem.Data.Persistence;
public sealed class SqlBulkLoader : ISqlBulkLoader
{
    public async Task BulkInsertAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableName,
        IReadOnlyList<string> columnNames,
        IReadOnlyCollection<TEntity> rows,
        Func<TEntity, object[]> rowMapper,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var table = new DataTable();
        foreach (var column in columnNames)
        {
            table.Columns.Add(column);
        }

        foreach (var row in rows)
        {
            table.Rows.Add(rowMapper(row));
        }

        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction)
        {
            DestinationTableName = $"dbo.[{tableName}]",
            BulkCopyTimeout = 120,
        };

        foreach (var column in columnNames)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }
}
