using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Clips;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Clips;

public sealed class ClipsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── CreateAsync ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsCreatedClip()
    {
        var handler = new FakeHttpHandler(JsonResponse("""{"data":[{"id":"new-clip","edit_url":"https://clips.twitch.tv/new-clip/edit"}]}"""));
        var endpoint = CreateEndpoint(handler);

        var clip = await endpoint.CreateAsync("123", TestContext.Current.CancellationToken);

        Assert.Equal("new-clip", clip.Id);
        Assert.Equal("https://clips.twitch.tv/new-clip/edit", clip.EditUrl);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    // ── GetByIdAsync ──────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsClip()
    {
        var endpoint = CreateEndpoint(JsonResponse($$"""{"data":[{{ClipJson("clip-1")}}]}"""));

        var clip = await endpoint.GetByIdAsync("clip-1", TestContext.Current.CancellationToken);

        Assert.NotNull(clip);
        Assert.Equal("clip-1", clip!.Id);
        Assert.Equal("My Clip", clip.Title);
        Assert.Equal(60.0f, clip.Duration);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var clip = await endpoint.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        Assert.Null(clip);
    }

    // ── GetByBroadcasterAsync ─────────────────────────────

    [Fact]
    public async Task GetByBroadcasterAsync_ReturnsPage()
    {
        var json = $$$"""{"data":[{{{ClipJson("clip-1")}}}],"pagination":{"cursor":"abc123"}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetByBroadcasterAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("clip-1", page.Items[0].Id);
        Assert.Equal("abc123", page.Cursor);
        Assert.True(page.HasMore);
    }

    // ── GetAllByBroadcasterAsync ──────────────────────────

    [Fact]
    public async Task GetAllByBroadcasterAsync_IteratesAllPages()
    {
        var page1 = JsonResponse($$$"""{"data":[{{{ClipJson("clip-1")}}}],"pagination":{"cursor":"next"}}""");
        var page2 = JsonResponse($$$"""{"data":[{{{ClipJson("clip-2")}}}],"pagination":{}}""");
        var endpoint = CreateEndpoint(page1, page2);

        var clips = new List<Clip>();
        await foreach (var c in endpoint.GetAllByBroadcasterAsync("123", TestContext.Current.CancellationToken))
            clips.Add(c);

        Assert.Equal(2, clips.Count);
        Assert.Equal("clip-1", clips[0].Id);
        Assert.Equal("clip-2", clips[1].Id);
    }

    // ── GetByGameAsync ────────────────────────────────────

    [Fact]
    public async Task GetByGameAsync_ReturnsPage()
    {
        var json = $$$"""{"data":[{{{ClipJson("clip-1")}}}],"pagination":{}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetByGameAsync("488191", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("clip-1", page.Items[0].Id);
        Assert.False(page.HasMore);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetByIdAsync("clip-1", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private ClipsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        return CreateEndpoint(handler);
    }

    private ClipsEndpoint CreateEndpoint(FakeHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ClipsEndpoint(httpClient, _errors);
    }

    private static string ClipJson(string id) => $$"""
        {
          "id": "{{id}}",
          "url": "https://clips.twitch.tv/{{id}}",
          "embed_url": "https://clips.twitch.tv/embed?clip={{id}}",
          "broadcaster_id": "67955580",
          "broadcaster_name": "Broadcaster",
          "creator_id": "53834192",
          "creator_name": "Creator",
          "video_id": "205586603",
          "game_id": "488191",
          "language": "en",
          "title": "My Clip",
          "view_count": 100,
          "created_at": "2024-01-01T00:00:00Z",
          "thumbnail_url": "https://example.com/thumb.jpg",
          "duration": 60.0,
          "vod_offset": 480
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
