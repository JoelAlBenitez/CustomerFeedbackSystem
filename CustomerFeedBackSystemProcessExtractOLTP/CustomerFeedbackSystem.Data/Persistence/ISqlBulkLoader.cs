using Microsoft.Data.SqlClient;

namespace CustomerFeedbackSystem.Data.Persistence;

public interface ISqlBulkLoader
{
    Task BulkInsertAsync<TEntity>(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableName,
        IReadOnlyList<string> columnNames,
        IReadOnlyCollection<TEntity> rows,
        Func<TEntity, object[]> rowMapper,
        CancellationToken cancellationToken = default);
}
