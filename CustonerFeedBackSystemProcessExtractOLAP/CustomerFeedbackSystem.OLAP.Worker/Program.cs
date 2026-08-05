using System.Text;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Worker;
using CustomerFeedbackSystem.OLAP.Worker.DependencyInjection;
using Serilog;


Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);


builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

var olapConnectionString = builder.Configuration.GetConnectionString(OlapConnectionOptions.ConnectionStringName);
if (string.IsNullOrWhiteSpace(olapConnectionString))
{
    Console.Error.WriteLine(
        $"Missing connection string '{OlapConnectionOptions.ConnectionStringName}'. Configure it with:\n"
        + $"  dotnet user-secrets set \"ConnectionStrings:{OlapConnectionOptions.ConnectionStringName}\" "
        + "\"<your connection string>\" --project CustomerFeedbackSystem.OLAP.Worker");
    return 1;
}


builder.Services.AddSerilog((_, config) => config
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    
    .WriteTo.File(
        "logs/extraction-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30));

// --phase Extract | Load | Full   (default Full: E and then L in one run)
var phase = EtlPhaseParser.Parse(builder.Configuration[EtlPhaseParser.ConfigurationKey]);

builder.Services.AddExtractionPipeline(builder.Configuration);
builder.Services.AddDimensionLoad(builder.Configuration);
builder.Services.AddEtlWorker(phase);

var host = builder.Build();
await host.RunAsync();


return Environment.ExitCode;

public partial class Program;
