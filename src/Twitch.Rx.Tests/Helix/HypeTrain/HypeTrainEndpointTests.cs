using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.HypeTrain;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.HypeTrain;

public sealed class HypeTrainEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetEventsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(EventsPageResponse("evt-1", "123", null));

        var page = await endpoint.GetEventsAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("evt-1", page.Items[0].Id);
        Assert.Equal("123", page.Items[0].BroadcasterId);
        Assert.Null(page.Cursor);
    }

    // ── GetAllEventsAsync ──────────────────────────────────

    [Fact]
    public async Task GetAllEventsAsync_IteratesAllPages()
    {
        var page1 = EventsPageResponse("evt-1", "123", "cursor-next");
        var page2 = EventsPageResponse("evt-2", "123", null);
        var endpoint = CreateEndpoint(page1, page2);

        var events = new List<HypeTrainEvent>();
        await foreach (var e in endpoint.GetAllEventsAsync("123", TestContext.Current.CancellationToken))
            events.Add(e);

        Assert.Equal(2, events.Count);
        Assert.Equal("evt-1", events[0].Id);
        Assert.Equal("evt-2", events[1].Id);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetEventsAsync("123", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private HypeTrainEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new HypeTrainEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage EventsPageResponse(string id, string broadcasterId, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{EventJson(id, broadcasterId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string EventJson(string id, string broadcasterId) => $$"""
        {
          "id": "{{id}}",
          "broadcaster_id": "{{broadcasterId}}",
          "level": 2,
          "total": 5000,
          "started_at": "2024-01-01T00:00:00Z",
          "expires_at": "2024-01-01T01:00:00Z"
        }
        """;

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
