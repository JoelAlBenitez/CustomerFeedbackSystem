using System.Net;
using CustomerFeedbackSystem.OLAP.Core.Common.Errors;
using CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Api;
using CustomerFeedbackSystem.OLAP.Infrastructure.Text;
using CustomerFeedbackSystem.OLAP.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

public sealed class ApiSocialCommentExtractorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 3, 14, 32, 8, TimeSpan.Zero);

    private static string Page(int page, int totalCount, int totalPages, params string[] items) =>
        $$"""
        {
          "page": {{page}},
          "pageSize": 2,
          "totalCount": {{totalCount}},
          "totalPages": {{totalPages}},
          "items": [{{string.Join(",", items)}}]
        }
        """;

    private static string Item(
        string idPost,
        string texto = "Muy buena calidad",
        string? interacciones = null,
        string usuario = "Cliente_19") =>
        $$"""
        {
          "idPost": "{{idPost}}",
          "usuarioRedSocial": "{{usuario}}",
          "plataforma": "Instagram",
          "fechaPost": "2025-06-15T00:00:00",
          "textoComentario": "{{texto}}",
          "interacciones": {{(interacciones is null ? "null" : $"\"{interacciones}\"")}}
        }
        """;

    private static ApiSocialCommentExtractor BuildExtractor(
        StubHttpMessageHandler handler,
        ApiSourceOptions? options = null)
    {
        options ??= new ApiSourceOptions { PageSize = 2, ApiKey = "test-key" };
        options.ApiKey = string.IsNullOrEmpty(options.ApiKey) ? "test-key" : options.ApiKey;

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7053") };
        var wrapped = Options.Create(options);

        var client = new SocialCommentApiClient(httpClient, wrapped, NullLogger<SocialCommentApiClient>.Instance);

        return new ApiSocialCommentExtractor(
            client,
            wrapped,
            new RawTextSanitizer(),
            new FakeTimeProvider(FixedNow),
            NullLogger<ApiSocialCommentExtractor>.Instance);
    }

    [Fact]
    public async Task ExtractAsync_WithOnePageOfThreeItems_YieldsThreeEntitiesInOneRequest()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 3, 1, Item("CS000001"), Item("CS000002"), Item("CS000003")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsSuccess);
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractAsync_WithThreePages_WalksThemAll()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 6, 3, Item("CS000001"), Item("CS000002")))
            .RespondWithJson(Page(2, 6, 3, Item("CS000003"), Item("CS000004")))
            .RespondWithJson(Page(3, 6, 3, Item("CS000005"), Item("CS000006")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Count(r => r.IsSuccess).Should().Be(6);
        handler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ExtractAsync_WhenUnauthorized_FailsImmediatelyWithoutRetrying()
    {
        // A wrong key is still wrong on the third attempt; retrying only delays the diagnosis.
        var handler = new StubHttpMessageHandler().RespondWithStatus(HttpStatusCode.Unauthorized);

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Should().ContainSingle();
        results[0].Errors[0].Should().BeOfType<SourceUnavailableError>()
            .Which.Reason.Should().Contain("X-Api-Key");
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractAsync_WhenServiceUnavailable_YieldsSourceUnavailableWithoutThrowing()
    {
        // The retry policy lives on the typed client in the composition root, so here the
        // handler's failure surfaces directly — what matters is that it becomes a Result,
        // never an escaping exception.
        var handler = new StubHttpMessageHandler().RespondWithStatus(HttpStatusCode.ServiceUnavailable);

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Should().ContainSingle();
        results[0].Errors[0].Should().BeOfType<SourceUnavailableError>();
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyItemsButHighTotalPages_StopsInsteadOfLoopingForever()
    {
        // The guard against a badly implemented API: TotalPages alone would never end the loop.
        var handler = new StubHttpMessageHandler().RespondWithJson(Page(1, 999, 500));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ExtractAsync_WhenMaxPagesIsReached_StopsThere()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 1; i <= 10; i++)
        {
            handler.RespondWithJson(Page(i, 100, 50, Item($"CS{i:D6}")));
        }

        var options = new ApiSourceOptions { PageSize = 2, ApiKey = "k", MaxPages = 3 };
        var results = await BuildExtractor(handler, options).ExtractAsync().DrainAsync();

        handler.RequestCount.Should().Be(3);
        results.Count(r => r.IsSuccess).Should().Be(3);
    }

    [Fact]
    public async Task ExtractAsync_WithNullInteracciones_UsesTheZeroSentinel()
    {
        // "0" and not "-": the field is semantically numeric and the T phase parses it directly.
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 1, 1, Item("CS000001", interacciones: null)));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results[0].Value.InteraccionesRaw.Should().Be("0");
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyTexto_RejectsThatItemAndKeepsTheOthers()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 2, 1, Item("CS000001", texto: ""), Item("CS000002")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results.Should().HaveCount(2);
        results[0].Errors[0].As<RecordValidationError>().Field.Should().Be("textoComentario");
        results[1].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_WithADuplicateIdPostAcrossPages_DiscardsTheSecond()
    {
        // The API orders explicitly, but the extractor does not take its word for it.
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 2, 2, Item("CS000001")))
            .RespondWithJson(Page(2, 2, 2, Item("CS000001")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results[0].IsSuccess.Should().BeTrue();
        results[1].Errors[0].As<RecordValidationError>().Reason.Should().Be("is duplicated");
    }

    [Fact]
    public async Task ExtractAsync_RecordsTheRelativePathOfThePageEachRowCameFrom()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWithJson(Page(1, 2, 2, Item("CS000001")))
            .RespondWithJson(Page(2, 2, 2, Item("CS000002")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results[0].Value.EndpointApiMeta.Should().Be("/api/v1/social-comments?page=1&pageSize=2");
        results[1].Value.EndpointApiMeta.Should().Be("/api/v1/social-comments?page=2&pageSize=2");

        // Relative, never absolute: host and port carry no analytical value.
        results[0].Value.EndpointApiMeta.Should().NotContain("localhost");
    }

    [Fact]
    public async Task ExtractAsync_FormatsFechaPostAsIsoDate()
    {
        var handler = new StubHttpMessageHandler().RespondWithJson(Page(1, 1, 1, Item("CS000001")));

        var results = await BuildExtractor(handler).ExtractAsync().DrainAsync();

        results[0].Value.FechaPostRaw.Should().Be("2025-06-15");
    }

    [Fact]
    public async Task ExtractAsync_WithoutAnApiKey_FailsBeforeMakingAnyRequest()
    {
        var handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7053") };
        var options = Options.Create(new ApiSourceOptions { PageSize = 2, ApiKey = string.Empty });

        var extractor = new ApiSocialCommentExtractor(
            new SocialCommentApiClient(httpClient, options, NullLogger<SocialCommentApiClient>.Instance),
            options,
            new RawTextSanitizer(),
            new FakeTimeProvider(FixedNow),
            NullLogger<ApiSocialCommentExtractor>.Instance);

        var results = await extractor.ExtractAsync().DrainAsync();

        results.Should().ContainSingle();
        handler.RequestCount.Should().Be(0);
    }
}
