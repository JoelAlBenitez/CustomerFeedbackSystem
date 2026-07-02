using CustomerFeedbackSystem.Data.Configuration;
using CustomerFeedbackSystem.Data.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var exitCode = await RunAsync(args);

// Keeps the window open when launched interactively (F5, double-click) so the
// load summary can actually be read. Skipped when input is redirected, which
// is how Task Scheduler / unattended runs are expected to be configured —
// otherwise a scheduled run would hang forever waiting for a keypress.
if (!Console.IsInputRedirected)
{
    Console.WriteLine();
    Console.WriteLine("Presiona una tecla para salir...");
    Console.ReadKey();
}

return exitCode;

static async Task<int> RunAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    builder.Services
        .AddOptions<CsvSourcesOptions>()
        .Bind(builder.Configuration.GetSection(CsvSourcesOptions.SectionName));

    using var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        var connectionString = host.Services
            .GetRequiredService<IConfiguration>()
            .GetConnectionString(ConnectionOptions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError(
                "Missing connection string '{ConnectionStringName}'. Configure it with: dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<your connection string>\"",
                ConnectionOptions.ConnectionStringName,
                ConnectionOptions.ConnectionStringName);
            return 1;
        }

        var csvOptions = host.Services.GetRequiredService<IOptions<CsvSourcesOptions>>().Value;
        csvOptions.BaseDirectory = CsvDirectoryResolver.Resolve(csvOptions.BaseDirectory);

        logger.LogInformation("CustomerFeedbackSystem.Load starting up.");
        logger.LogInformation("CSV source directory: {CsvDirectory}", csvOptions.BaseDirectory);

        var pipeline = new EtlPipeline(connectionString, csvOptions, host.Services.GetRequiredService<ILogger<EtlPipeline>>());
        var report = await pipeline.RunAsync();

        return report.TotalInserted > 0 || report.TotalRead == 0 ? 0 : 1;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "The load run failed and was rolled back. No data was changed.");
        return 1;
    }
}
