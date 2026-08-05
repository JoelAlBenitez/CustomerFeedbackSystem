using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Orchestration;
using CustomerFeedbackSystem.OLAP.Core.Reporting;
using CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;
using CustomerFeedbackSystem.OLAP.Worker.Presentation;

namespace CustomerFeedbackSystem.OLAP.Worker;

public sealed class EtlWorker : BackgroundService
{
    private const string ExtractionTitle = "CustomerFeedbackSystem OLAP — Proceso de Extracción";
    private const string LoadTitle = "CustomerFeedbackSystem OLAP — Carga de Dimensiones";

    private readonly EtlPhase _phase;
    private readonly ExtractionPipeline _extractionPipeline;
    private readonly DimensionLoadPipeline _dimensionLoadPipeline;
    private readonly OltpAvailabilityProbe _oltpProbe;
    private readonly StagingReadinessProbe _stagingProbe;
    private readonly ITokenAnalyzer _tokenAnalyzer;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<EtlWorker> _logger;

    public EtlWorker(
        EtlRunOptions runOptions,
        ExtractionPipeline extractionPipeline,
        DimensionLoadPipeline dimensionLoadPipeline,
        OltpAvailabilityProbe oltpProbe,
        StagingReadinessProbe stagingProbe,
        ITokenAnalyzer tokenAnalyzer,
        IHostApplicationLifetime lifetime,
        ILogger<EtlWorker> logger)
    {
        _phase = runOptions.Phase;
        _extractionPipeline = extractionPipeline;
        _dimensionLoadPipeline = dimensionLoadPipeline;
        _oltpProbe = oltpProbe;
        _stagingProbe = stagingProbe;
        _tokenAnalyzer = tokenAnalyzer;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Running phase {Phase}.", _phase);

            await _tokenAnalyzer.WarmUpAsync(stoppingToken);

            if (_phase.IncludesExtract() && !await RunExtractionAsync(stoppingToken))
            {
                return;
            }

            if (_phase.IncludesLoad())
            {
                await RunDimensionLoadAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Run cancelled by request.");
            Environment.ExitCode = 2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run failed unexpectedly.");
            Environment.ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task<bool> RunExtractionAsync(CancellationToken cancellationToken)
    {
        await _oltpProbe.WarnIfOltpEmptyAsync(cancellationToken);

        ExtractionReport report;
        await using (new ConsoleSpinner("Ejecutando proceso de extracción..."))
        {
            report = await _extractionPipeline.RunAsync(cancellationToken);
        }

        ConsoleReportRenderer.Render(report, ExtractionTitle, DateTime.Now);

        if (!report.AnySourceFailed)
        {
            return true;
        }

        Environment.ExitCode = 1;

        if (_phase == EtlPhase.Full)
        {
            _logger.LogError(
                "At least one source failed extracting, so the dimension load was skipped. "
                + "Fix the source and rerun, or force the load with --phase Load.");
        }

        return false;
    }

    private async Task RunDimensionLoadAsync(CancellationToken cancellationToken)
    {
        var ready = await _stagingProbe.EnsurePopulatedAsync(cancellationToken);
        if (ready.IsFailure)
        {
            _logger.LogError("Dimension load precondition not met: {Error}", ready.Errors[0]);
            Environment.ExitCode = 1;
            return;
        }

        DimensionLoadReport report;
        await using (new ConsoleSpinner("Cargando dimensiones del Data Warehouse..."))
        {
            report = await _dimensionLoadPipeline.RunAsync(cancellationToken);
        }

        ConsoleDimensionReportRenderer.Render(report, LoadTitle, DateTime.Now);

        if (!report.Committed)
        {
            Environment.ExitCode = 1;
        }
    }
}
