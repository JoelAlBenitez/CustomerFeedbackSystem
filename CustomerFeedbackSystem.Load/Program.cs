using CustomerFeedbackSystem.Load.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
logger.LogInformation("Connection string configured: {IsConfigured}", !string.IsNullOrWhiteSpace(connectionString));

// The ETL pipeline is wired up in a later step; for now this confirms the
// host, configuration and User Secrets are correctly plumbed end to end.

return 0;
