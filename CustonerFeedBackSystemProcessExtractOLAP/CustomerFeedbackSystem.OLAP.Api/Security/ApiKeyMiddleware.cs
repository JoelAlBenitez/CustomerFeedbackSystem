using System.Security.Cryptography;
using System.Text;

namespace CustomerFeedbackSystem.OLAP.Api.Security;

/// <summary>
/// Validates the <c>X-Api-Key</c> header before any endpoint touches the database.
/// This is what answers the Actividad 1 §1 requirement "secure handling of connection
/// credentials and API calls" on the server side.
/// </summary>
public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    /// <summary>Swagger and the health probe are exempt: neither reads data.</summary>
    private static readonly string[] ExemptPrefixes = ["/swagger", "/health"];

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly byte[] _expectedKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Comes from User Secrets in development and an environment variable on a server,
        // never from appsettings.json.
        _expectedKey = Encoding.UTF8.GetBytes(configuration["Security:ApiKey"] ?? string.Empty);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (Array.Exists(ExemptPrefixes, prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || !IsValid(provided!))
        {
            // The received value is never logged — not even masked. A log showing the first
            // four characters of a secret is still a leak (doc 12 §6).
            _logger.LogWarning(
                "Rejected a request to {Path} from {RemoteIp}: missing or invalid API key.",
                path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = $"A valid {HeaderName} header is required." });
            return;
        }

        await _next(context);
    }

    private bool IsValid(string? provided)
    {
        if (_expectedKey.Length == 0 || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        // FixedTimeEquals, not string ==. Comparing secrets in constant time is what stops an
        // attacker from recovering the key one character at a time by measuring how long the
        // comparison takes.
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), _expectedKey);
    }
}
