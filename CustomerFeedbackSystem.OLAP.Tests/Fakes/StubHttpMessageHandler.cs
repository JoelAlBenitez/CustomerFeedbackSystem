using System.Net;
using System.Text;

namespace CustomerFeedbackSystem.OLAP.Tests.Fakes;

/// <summary>
/// HttpClient is substitutable through its handler, which is what makes the API extractor
/// fully testable without a network.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<string> RequestedPaths { get; } = [];

    public int RequestCount => RequestedPaths.Count;

    public StubHttpMessageHandler RespondWithJson(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

        return this;
    }

    public StubHttpMessageHandler RespondWithStatus(HttpStatusCode status, int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            _responses.Enqueue(_ => new HttpResponseMessage(status));
        }

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);

        // When the script runs out, keep replaying the last behaviour — that is what makes
        // "503 forever" testable without enqueuing an unknown number of responses.
        var factory = _responses.Count > 0
            ? _responses.Dequeue()
            : _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        return Task.FromResult(factory(request));
    }
}
