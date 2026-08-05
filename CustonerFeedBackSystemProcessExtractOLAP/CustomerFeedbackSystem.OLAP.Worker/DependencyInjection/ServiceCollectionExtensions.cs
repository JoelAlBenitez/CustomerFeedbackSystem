using System.Net;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Orchestration;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Api;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Csv;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Database;
using CustomerFeedbackSystem.OLAP.Infrastructure.Load;
using CustomerFeedbackSystem.OLAP.Infrastructure.Persistence;
using CustomerFeedbackSystem.OLAP.Infrastructure.Text;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace CustomerFeedbackSystem.OLAP.Worker.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExtractionPipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddOptions(services, configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITextSanitizer, RawTextSanitizer>();
        services.AddSingleton<ITokenAnalyzer, CatalystSpanishAnalyzer>();
        services.AddSingleton<IStagingResetService, StagingResetService>();
        services.AddSingleton<OltpAvailabilityProbe>();

        services.AddSingleton(StagingDescriptors.Encuestas);
        services.AddSingleton(StagingDescriptors.Resenas);
        services.AddSingleton(StagingDescriptors.Sociales);

        // Writers.
        services.AddSingleton<IStagingWriter<StagingEncuestaCsv>, SqlBulkStagingWriter<StagingEncuestaCsv>>();
        services.AddSingleton<IStagingWriter<StagingResenaWeb>, SqlBulkStagingWriter<StagingResenaWeb>>();
        services.AddSingleton<IStagingWriter<StagingComentarioSocial>, SqlBulkStagingWriter<StagingComentarioSocial>>();

        // Extractors.
        services.AddSingleton<IExtractor<StagingEncuestaCsv>, CsvSurveyExtractor>();
        services.AddSingleton<IExtractor<StagingResenaWeb>, SqlWebReviewExtractor>();
        services.AddSingleton<IExtractor<StagingComentarioSocial>, ApiSocialCommentExtractor>();

        AddSocialCommentApiClient(services);

       
        AddSource<StagingEncuestaCsv>(services, sp => sp.GetRequiredService<IOptions<CsvSourceOptions>>().Value.Enabled);
        AddSource<StagingResenaWeb>(services, sp =>
            sp.GetRequiredService<IOptions<DatabaseSourceOptions>>().Value.Enabled
            && !string.IsNullOrWhiteSpace(sp.GetRequiredService<IOptions<OltpConnectionOptions>>().Value.ConnectionString));
        AddSource<StagingComentarioSocial>(services, sp =>
            sp.GetRequiredService<IOptions<ApiSourceOptions>>().Value.Enabled);

        services.AddSingleton<ExtractionPipeline>();

        return services;
    }

    public static IServiceCollection AddDimensionLoad(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DimensionLoadOptions>()
            .Bind(configuration.GetSection(DimensionLoadOptions.SectionName))
            .Validate(o => o.CommandTimeoutSeconds > 0, "Load:CommandTimeoutSeconds must be greater than zero.")
            .Validate(o => o.MinKeywordFrequency > 0, "Load:MinKeywordFrequency must be greater than zero.")
            .Validate(o => o.DateDimensionPaddingYears is >= 0 and <= 10,
                "Load:DateDimensionPaddingYears must be between 0 and 10.")
            .ValidateOnStart();

        services.AddSingleton<SqlDimensionLoadSession>();
        services.AddSingleton<IDimensionLoadSession>(sp => sp.GetRequiredService<SqlDimensionLoadSession>());
        services.AddSingleton<IDimensionResetService, DimensionResetService>();
        services.AddSingleton<StagingReadinessProbe>();

        services.AddSingleton<IDimensionLoader, DimFechaLoader>();
        services.AddSingleton<IDimensionLoader, DimFuenteLoader>();
        services.AddSingleton<IDimensionLoader, DimClasificacionLoader>();
        services.AddSingleton<IDimensionLoader, DimProductoLoader>();
        services.AddSingleton<IDimensionLoader, DimClienteLoader>();
        services.AddSingleton<IDimensionLoader, PalabraClaveLoader>();

        services.AddSingleton<DimensionLoadPipeline>();

        return services;
    }

    public static IServiceCollection AddEtlWorker(this IServiceCollection services, EtlPhase phase)
    {
        services.AddSingleton(new EtlRunOptions { Phase = phase });
        services.AddHostedService<EtlWorker>();

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
      
        services.Configure<OlapConnectionOptions>(o =>
            o.ConnectionString = configuration.GetConnectionString(OlapConnectionOptions.ConnectionStringName) ?? string.Empty);

        services.Configure<OltpConnectionOptions>(o =>
            o.ConnectionString = configuration.GetConnectionString(OltpConnectionOptions.ConnectionStringName) ?? string.Empty);

        services.AddOptions<ExtractionOptions>()
            .Bind(configuration.GetSection(ExtractionOptions.SectionName))
            .Validate(o => o.BatchSize is > 0 and <= 100_000, "Extraction:BatchSize must be between 1 and 100000.")
            .ValidateOnStart();

        services.AddOptions<CsvSourceOptions>()
            .Bind(configuration.GetSection(CsvSourceOptions.SectionName))
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.SurveysFile),
                "Sources:Csv:SurveysFile is required when the CSV source is enabled.")
            .ValidateOnStart();

        services.AddOptions<DatabaseSourceOptions>()
            .Bind(configuration.GetSection(DatabaseSourceOptions.SectionName))
            .Validate(o => o.CommandTimeoutSeconds > 0, "Sources:Database:CommandTimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

       
        services.AddOptions<ApiSourceOptions>()
            .Bind(configuration.GetSection(ApiSourceOptions.SectionName))
            .Validate(o => !o.Enabled || Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute),
                "Sources:Api:BaseUrl must be a valid absolute URL when the API source is enabled.")
            .Validate(o => o.PageSize is > 0 and <= 1000, "Sources:Api:PageSize must be between 1 and 1000.")
            .Validate(o => o.MaxPages > 0, "Sources:Api:MaxPages must be greater than zero.")
            .Validate(o => o.TimeoutSeconds > 0, "Sources:Api:TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();
    }

    private static void AddSocialCommentApiClient(IServiceCollection services)
    {
        services.AddHttpClient<SocialCommentApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiSourceOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        })
        .AddPolicyHandler(BuildRetryPolicy());
    }


    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static void AddSource<T>(IServiceCollection services, Func<IServiceProvider, bool> isEnabled) =>
        services.AddSingleton<IExtractionSource>(sp => new ExtractionSourceRunner<T>(
            sp.GetRequiredService<IExtractor<T>>(),
            sp.GetRequiredService<IStagingWriter<T>>(),
            sp.GetRequiredService<IStagingResetService>(),
            new ExtractionRunOptions
            {
                BatchSize = sp.GetRequiredService<IOptions<ExtractionOptions>>().Value.BatchSize,
                Enabled = isEnabled(sp),
            },
            sp.GetRequiredService<ILogger<ExtractionSourceRunner<T>>>()));
}
