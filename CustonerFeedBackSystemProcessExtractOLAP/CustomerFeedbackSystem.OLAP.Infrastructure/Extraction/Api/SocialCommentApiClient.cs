using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Api;

public sealed class SocialCommentApiClient
{
    public const string SourceName = "SocialCommentsApi";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ApiSourceOptions _options;
    private readonly ILogger<SocialCommentApiClient> _logger;

    public SocialCommentApiClient(
        HttpClient httpClient,
        IOptions<ApiSourceOptions> options,
        ILogger<SocialCommentApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildRelativePath(int page, int pageSize) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{_options.SocialCommentsPath}?page={page}&pageSize={pageSize}");

   
    public async Task<Result<SocialCommentApiResponse>> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var relativePath = BuildRelativePath(page, pageSize);

        try
        {
            using var response = await _httpClient.GetAsync(relativePath, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
              
                return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                    SourceName, "authentication failed — check X-Api-Key"));
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                    SourceName, $"bad request for '{relativePath}'"));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                    SourceName, $"responded {(int)response.StatusCode} for '{relativePath}'"));
            }

            var payload = await response.Content.ReadFromJsonAsync<SocialCommentApiResponse>(
                JsonOptions, cancellationToken);

            return payload is null
                ? Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                    SourceName, $"returned an empty body for '{relativePath}'"))
                : Result<SocialCommentApiResponse>.Success(payload);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Request to {Path} failed: {Reason}", relativePath, ex.Message);
            return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(SourceName, ex.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            
            _logger.LogWarning("Request to {Path} timed out after {Timeout}s.", relativePath, _options.TimeoutSeconds);
            return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                SourceName, $"timed out after {_options.TimeoutSeconds}s"));
        }
        catch (JsonException ex)
        {
            return Result<SocialCommentApiResponse>.Failure(new SourceUnavailableError(
                SourceName, $"returned a body that is not valid JSON: {ex.Message}"));
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(_options.HealthPath, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
