using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using CustomerFeedbackSystem.OLAP.Core.Abstractions;
using CustomerFeedbackSystem.OLAP.Core.Common;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Core.Staging;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Api;

/// <summary>
/// Pages through the social comments API into Staging.stgRedesSocialesAPI.
/// <para>
/// The only source that crosses the network, and therefore the only one that needs retries,
/// timeouts and an explicit policy for what to do when the other side stops answering.
/// </para>
/// </summary>
public sealed class ApiSocialCommentExtractor : IExtractor<StagingComentarioSocial>
{
    private const int PostIdWidth = 100;
    private const int UserWidth = 100;
    private const int PlatformWidth = 50;
    private const int DateWidth = 50;
    private const int InteractionsWidth = 20;
    private const int EndpointWidth = 255;
    private const int Unbounded = 0;   // TextoComentarioRaw is NVARCHAR(MAX)

    /// <summary>
    /// "0" and not "-". The field is semantically numeric and the T phase parses it straight
    /// to an integer; "-" would force a special case there, while "0" reads correctly as
    /// "no interactions recorded" (doc 14 §5.3).
    /// </summary>
    private const string InteractionsSentinel = "0";

    private readonly SocialCommentApiClient _client;
    private readonly ApiSourceOptions _options;
    private readonly ITextSanitizer _sanitizer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ApiSocialCommentExtractor> _logger;

    public ApiSocialCommentExtractor(
        SocialCommentApiClient client,
        IOptions<ApiSourceOptions> options,
        ITextSanitizer sanitizer,
        TimeProvider timeProvider,
        ILogger<ApiSocialCommentExtractor> logger)
    {
        _client = client;
        _options = options.Value;
        _sanitizer = sanitizer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string SourceName => SocialCommentApiClient.SourceName;

    public async IAsyncEnumerable<Result<StagingComentarioSocial>> ExtractAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            yield return Result<StagingComentarioSocial>.Failure(new SourceUnavailableError(
                SourceName, "no API key is configured — set Sources:Api:ApiKey in User Secrets"));
            yield break;
        }

        var loadedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var stopwatch = Stopwatch.StartNew();

        // Paging is not necessarily stable — a concurrent insert at the origin shifts rows
        // between pages. The API's explicit ORDER BY prevents that, but the extractor does
        // not take the source's word for it: it verifies.
        var seenPostIds = new HashSet<string>(StringComparer.Ordinal);
        var fields = new RecordFieldSanitizer(_sanitizer, SourceName);

        var page = 1;
        var pagesRead = 0;
        var emitted = 0L;
        var recordNumber = 0L;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pagesRead >= _options.MaxPages)
            {
                _logger.LogWarning(
                    "Stopped after the configured MaxPages ({MaxPages}). The source may be reporting a wrong TotalPages.",
                    _options.MaxPages);
                break;
            }

            var relativePath = _client.BuildRelativePath(page, _options.PageSize);
            var pageResult = await _client.GetPageAsync(page, _options.PageSize, cancellationToken);

            // A transport error after the retries means the source is not there. Asking for
            // the next page would be pointless.
            if (pageResult.IsFailure)
            {
                yield return Result<StagingComentarioSocial>.Failure(pageResult.Errors[0]);
                yield break;
            }

            var payload = pageResult.Value;
            pagesRead++;

            // The empty-Items check is the guard against an infinite loop: a badly implemented
            // API reporting a wrong TotalPages would never satisfy the TotalPages condition
            // alone.
            if (payload.Items.Count == 0)
            {
                break;
            }

            foreach (var item in payload.Items)
            {
                recordNumber++;

                foreach (var result in ProjectItem(item, recordNumber, relativePath, loadedAt, seenPostIds, fields))
                {
                    if (result.IsSuccess)
                    {
                        emitted++;
                    }

                    yield return result;
                }
            }

            if (page >= payload.TotalPages)
            {
                break;
            }

            page++;
        }

        stopwatch.Stop();
        var averagePerPage = pagesRead > 0 ? stopwatch.Elapsed.TotalMilliseconds / pagesRead : 0;
        _logger.LogInformation(
            "Source {Source} streamed {Rows} rows over {Pages} page(s) in {Elapsed} ({AveragePerPage:F0} ms/page).",
            SourceName, emitted, pagesRead, stopwatch.Elapsed, averagePerPage);
    }

    private IEnumerable<Result<StagingComentarioSocial>> ProjectItem(
        SocialCommentItem item,
        long recordNumber,
        string relativePath,
        DateTime loadedAt,
        HashSet<string> seenPostIds,
        RecordFieldSanitizer fields)
    {
        fields.BeginRecord(recordNumber);

        var idPost = fields.Take(item.IdPost, PostIdWidth, "idPost");
        if (RecordFieldSanitizer.IsMissing(idPost))
        {
            yield return Result<StagingComentarioSocial>.Failure(
                new RecordValidationError(SourceName, recordNumber, "idPost", "is empty"));
            yield break;
        }

        if (!seenPostIds.Add(idPost))
        {
            yield return Result<StagingComentarioSocial>.Failure(
                new RecordValidationError(SourceName, recordNumber, "idPost", "is duplicated"));
            yield break;
        }

        // A social comment with no text contributes nothing to an opinion analysis.
        var texto = fields.Take(item.TextoComentario, Unbounded, "textoComentario");
        if (RecordFieldSanitizer.IsMissing(texto))
        {
            yield return Result<StagingComentarioSocial>.Failure(
                new RecordValidationError(SourceName, recordNumber, "textoComentario", "is empty"));
            yield break;
        }

        var entity = new StagingComentarioSocial
        {
            IdPostRaw = idPost,
            UsuarioRedSocialRaw = fields.Take(item.UsuarioRedSocial, UserWidth, "usuarioRedSocial"),
            PlataformaRaw = fields.Take(item.Plataforma, PlatformWidth, "plataforma"),
            FechaPostRaw = fields.Take(
                item.FechaPost?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), DateWidth, "fechaPost"),
            TextoComentarioRaw = texto,
            InteraccionesRaw = fields.Take(
                item.Interacciones, InteractionsWidth, "interacciones", InteractionsSentinel),

            // Relative, never absolute. The absolute URL would carry host and port — pure
            // infrastructure with no analytical value, and in other setups a place where
            // credentials end up in a query string.
            EndpointApiMeta = fields.Take(relativePath, EndpointWidth, "EndpointAPIMeta"),

            FechaCargaMeta = loadedAt,
        };

        foreach (var truncation in fields.Truncations)
        {
            yield return Result<StagingComentarioSocial>.Failure(truncation);
        }

        yield return Result<StagingComentarioSocial>.Success(entity);
    }
}
