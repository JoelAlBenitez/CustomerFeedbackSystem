using System.Data;
using Microsoft.Data.SqlClient;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

// Surrogate keys are assigned in the application and pushed with KeepIdentity, which is the
// approach already proven by IdentityAllocator in the OLTP project (doc 17 §3.2). It keeps the
// keys deterministic: "Desconocido" is always SK 1, run after run.
internal static class SqlDimensionWriter
{
    public static async Task WriteAsync<T>(
        SqlDimensionLoadSession session,
        string qualifiedTable,
        IReadOnlyList<string> columns,
        IReadOnlyCollection<T> rows,
        Func<T, object?[]> valueSelector,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        using var table = new DataTable();
        foreach (var column in columns)
        {
            // object, not string: these tables hold ints, bits, dates and nullable columns.
            table.Columns.Add(column, typeof(object));
        }

        foreach (var row in rows)
        {
            var values = valueSelector(row);
            for (var i = 0; i < values.Length; i++)
            {
                values[i] ??= DBNull.Value;
            }

            table.Rows.Add(values);
        }

        using var bulkCopy = new SqlBulkCopy(
            session.Connection,
            SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock,
            session.Transaction)
        {
            DestinationTableName = qualifiedTable,
            BatchSize = rows.Count,
            BulkCopyTimeout = timeoutSeconds,
        };

        foreach (var column in columns)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    // Leaves the IDENTITY counter where the explicit keys ended, so anything inserting without
    // KeepIdentity later continues the sequence instead of colliding with it.
    public static async Task ReseedAsync(
        SqlDimensionLoadSession session,
        string qualifiedTable,
        int lastAssignedId,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = $"DBCC CHECKIDENT ('{qualifiedTable}', RESEED, {lastAssignedId}) WITH NO_INFOMSGS;";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
