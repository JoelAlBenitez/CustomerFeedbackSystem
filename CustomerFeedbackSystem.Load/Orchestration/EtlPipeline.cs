using CustomerFeedbackSystem.Load.Configuration;
using CustomerFeedbackSystem.Load.Orchestration.Steps;
using CustomerFeedbackSystem.Load.Persistence;
using CustomerFeedbackSystem.Load.Reporting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CustomerFeedbackSystem.Load.Orchestration;

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
            _logger.LogInformation("Resetting owned tables for a full refresh...");
            await new TableResetService().ResetAllAsync(connection, transaction, cancellationToken);

            _logger.LogInformation("Loading catalogs: Clientes, Productos, FuentesDatos...");
            var clientes = await new ClientesLoadStep().RunAsync(_csvSources.ResolveClientsPath(), context);
            var productos = await new ProductosLoadStep().RunAsync(_csvSources.ResolveProductsPath(), context);
            await new FuentesDatosLoadStep().RunAsync(_csvSources.ResolveDataSourcesPath(), context);

            _logger.LogInformation("Loading facts: web reviews, surveys, social comments...");
            await new WebReviewsLoadStep().RunAsync(
                _csvSources.ResolveWebReviewsPath(), clientes.ClienteIds, productos.ProductoIds, context);
            await new SurveysLoadStep().RunAsync(
                _csvSources.ResolveSurveysPath(), clientes.ClienteIds, productos.ProductoIds, context);
            await new SocialCommentsLoadStep().RunAsync(
                _csvSources.ResolveSocialCommentsPath(), clientes.ClienteIds, productos.ProductoIds,
                clientes.SentinelClienteId, context);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Transaction committed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load run failed — rolling back, database left unchanged.");
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        report.Print(_logger, stopwatch.Elapsed);
        return report;
    }
}
