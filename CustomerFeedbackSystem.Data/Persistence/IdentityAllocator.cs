using Microsoft.Data.SqlClient;

namespace CustomerFeedbackSystem.Data.Persistence;

public sealed class IdentityAllocator
{
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;

    public IdentityAllocator(SqlConnection connection, SqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<int> AllocateNextAsync(string tableName, string identityColumn, CancellationToken cancellationToken = default)
    {
        var range = await AllocateRangeAsync(tableName, identityColumn, 1, cancellationToken);
        return range[0];
    }

    public async Task<int[]> AllocateRangeAsync(string tableName, string identityColumn, int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return [];
        }

        await using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandTimeout = 120;
        command.CommandText = $"SELECT ISNULL(MAX([{identityColumn}]), 0) FROM dbo.[{tableName}]";

        var maxValue = await command.ExecuteScalarAsync(cancellationToken);
        var max = Convert.ToInt32(maxValue);

        var ids = new int[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = max + i + 1;
        }

        return ids;
    }
}
