using CustomerFeedbackSystem.Data.Configuration;
using CustomerFeedbackSystem.Data.Orchestration.Steps;
using CustomerFeedbackSystem.Data.Persistence;
using CustomerFeedbackSystem.Data.Reporting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.Data.Orchestration;

public sealed class EtlPipeline
{
    private readonly string _connectionString;
    private readonly CsvSourcesOptions _csvSources;
    private readonly ILogger<EtlPipeline> _logger;

    public EtlPipeline(string connectionString, CsvSourcesOptions csvSources, ILogger<EtlPipeline> logger)
    {
        _connectionString = connectionString;
        _csvSources = csvSources;
        _logger = logger;
    }

    public async Task<LoadReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var report = new LoadReport();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var context = new EtlRunContext
        {
            Connection = connection,
            Transaction = transaction,
            IdentityAllocator = new IdentityAllocator(connection, transaction),
            BulkLoader = new SqlBulkLoader(),
            Report = report,
            Logger = _logger,
            CancellationToken = cancellationToken,
        };

        try
        {
          
            _logger.LogDebug("Resetting owned tables for a full refresh...");
            await new TableResetService().ResetAllAsync(connection, transaction, cancellationToken);

            _logger.LogDebug("Loading catalogs: Clientes, Productos, FuentesDatos...");
            var clientes = await new ClientesLoadStep().RunAsync(_csvSources.ResolveClientsPath(), context);
            var productos = await new ProductosLoadStep().RunAsync(_csvSources.ResolveProductsPath(), context);
            await new FuentesDatosLoadStep().RunAsync(_csvSources.ResolveDataSourcesPath(), context);

            _logger.LogDebug("Loading facts: web reviews, surveys, social comments...");
            await new WebReviewsLoadStep().RunAsync(
                _csvSources.ResolveWebReviewsPath(), clientes.ClienteIds, productos.ProductoIds, context);
            await new SurveysLoadStep().RunAsync(
                _csvSources.ResolveSurveysPath(), clientes.ClienteIds, productos.ProductoIds, context);
            await new SocialCommentsLoadStep().RunAsync(
                _csvSources.ResolveSocialCommentsPath(), clientes.ClienteIds, productos.ProductoIds,
                clientes.SentinelClienteId, context);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load run failed — rolling back, database left unchanged.");

            if (connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "Rollback itself failed; the connection was likely already lost.");
                }
            }

            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        report.Elapsed = stopwatch.Elapsed;
        return report;
    }
}
