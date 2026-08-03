using CustomerFeedbackSystem.OLAP.Api.Configuration;
using CustomerFeedbackSystem.OLAP.Api.Endpoints;
using CustomerFeedbackSystem.OLAP.Api.Persistence;
using CustomerFeedbackSystem.OLAP.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Each provider overrides the previous one: JSON as the baseline, User Secrets on top in
// development, environment variables last. No code changes between environments.
builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("CustomerReviewSystemData");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fail at startup with the exact command to fix it, rather than with a
    // NullReferenceException on the first request.
    throw new InvalidOperationException(
        "Missing connection string 'CustomerReviewSystemData'. Configure it with:\n"
        + "  dotnet user-secrets set \"ConnectionStrings:CustomerReviewSystemData\" \"<your connection string>\" "
        + "--project CustomerFeedbackSystem.OLAP.Api");
}

builder.Services.AddDbContext<OltpReadDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services
    .AddOptions<PagingOptions>()
    .Bind(builder.Configuration.GetSection(PagingOptions.SectionName))
    .Validate(o => o.DefaultPageSize is > 0 and <= 1000, "Paging:DefaultPageSize must be between 1 and 1000.")
    .Validate(o => o.MaxPageSize is > 0 and <= 1000, "Paging:MaxPageSize must be between 1 and 1000.")
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CustomerFeedbackSystem OLAP — Social comments source",
        Version = "v1",
        Description = "Publishes dbo.ComentariosSociales of the OLTP database as the REST source of the OLAP ETL.",
    });

    options.AddSecurityDefinition(ApiKeyMiddleware.HeaderName, new OpenApiSecurityScheme
    {
        Name = ApiKeyMiddleware.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Shared key. Configured through User Secrets, never in appsettings.json.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyMiddleware.HeaderName },
        }] = Array.Empty<string>(),
    });
});

var app = builder.Build();

// Development only. In production an endpoint that publishes the full contract is attack
// surface with no benefit.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Before every endpoint, so no request reaches the database unauthenticated.
app.UseMiddleware<ApiKeyMiddleware>();

app.MapSocialCommentEndpoints();

app.Run();

/// <summary>
/// Named so <c>AddUserSecrets&lt;Program&gt;</c> has a type to anchor the secrets id to.
/// </summary>
public partial class Program;
