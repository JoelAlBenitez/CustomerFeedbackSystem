using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Orchestration;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;
using CustomerFeedbackSystem.OLAP.Worker.Presentation;

namespace CustomerFeedbackSystem.OLAP.Worker;


public sealed class ExtractionWorker : BackgroundService
{
    private const string ReportTitle = "CustomerFeedbackSystem OLAP — Proceso de Extracción";

    private readonly ExtractionPipeline _pipeline;
    private readonly OltpAvailabilityProbe _probe;
    private readonly ITokenAnalyzer _tokenAnalyzer;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(
        ExtractionPipeline pipeline,
        OltpAvailabilityProbe probe,
        ITokenAnalyzer tokenAnalyzer,
        IHostApplicationLifetime lifetime,
        ILogger<ExtractionWorker> logger)
    {
        _pipeline = pipeline;
        _probe = probe;
        _tokenAnalyzer = tokenAnalyzer;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
           
            await _probe.WarnIfOltpEmptyAsync(stoppingToken);

            
            await _tokenAnalyzer.WarmUpAsync(stoppingToken);

            ExtractionReport report;
            await using (new ConsoleSpinner("Ejecutando proceso de extracción..."))
            {
                report = await _pipeline.RunAsync(stoppingToken);
            }

            ConsoleReportRenderer.Render(report, ReportTitle, DateTime.Now);
            Environment.ExitCode = report.AnySourceFailed ? 1 : 0;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Extraction cancelled by request.");
            Environment.ExitCode = 2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction run failed unexpectedly.");
            Environment.ExitCode = 1;
        }
        finally
        {
            
            _lifetime.StopApplication();
        }
    }
}
