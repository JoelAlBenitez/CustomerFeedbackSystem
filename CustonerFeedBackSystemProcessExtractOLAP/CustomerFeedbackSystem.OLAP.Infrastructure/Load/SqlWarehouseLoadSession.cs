using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Load;

public sealed class SqlWarehouseLoadSession : IWarehouseLoadSession
{
    private readonly string _connectionString;
    private readonly ILogger<SqlWarehouseLoadSession> _logger;

    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private bool _completed;

    public SqlWarehouseLoadSession(
        IOptions<OlapConnectionOptions> connectionOptions,
        ILogger<SqlWarehouseLoadSession> logger)
    {
        _connectionString = connectionOptions.Value.ConnectionString;
        _logger = logger;
    }

    public SqlConnection Connection => _connection
        ?? throw new InvalidOperationException("The load session has not been opened.");

    public SqlTransaction Transaction => _transaction
        ?? throw new InvalidOperationException("The load session has not been opened.");

    public async Task<Result> OpenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync(cancellationToken);
            _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(cancellationToken);

            _logger.LogDebug("Load transaction opened against the OLAP database.");
            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Could not open the load transaction; SQL error {Number}.", ex.Number);
            return Result.Failure(new PreconditionError("OLAP connection", ex.Message));
        }
    }

    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return Result.Failure(new PreconditionError("load transaction", "was never opened"));
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;

            _logger.LogDebug("Load transaction committed.");
            return Result.Success();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Commit failed; SQL error {Number}.", ex.Number);
            return Result.Failure(new DimensionLoadError("commit", ex.Message));
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null || _completed)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;

            _logger.LogWarning("Load transaction rolled back — the warehouse keeps its previous content.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rollback itself failed; the connection was likely already lost.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            if (!_completed)
            {
                await RollbackAsync(CancellationToken.None);
            }

            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
