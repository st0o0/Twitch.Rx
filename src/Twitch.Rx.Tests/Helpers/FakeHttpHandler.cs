using System.Net;

namespace Twitch.Rx.Tests.Helpers;

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public HttpRequestMessage? LastRequest { get; private set; }
    public int RequestCount { get; private set; }
    public List<HttpRequestMessage> AllRequests { get; } = [];

    public FakeHttpHandler(params HttpResponseMessage[] responses)
    {
        foreach (var r in responses) _responses.Enqueue(r);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        RequestCount++;
        AllRequests.Add(request);
        return Task.FromResult(_responses.Count > 0
            ? _responses.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
