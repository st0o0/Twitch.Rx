using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Channels;

public sealed class ChannelsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetInfoAsync (single) ─────────────────────────────

    [Fact]
    public async Task GetInfoAsync_Single_ReturnsChannelInfo()
    {
        var endpoint = CreateEndpoint(ChannelInfoResponse("123", "streamer", "Streamer", "en", "game1", "Fortnite", "Playing Fortnite"));

        var info = await endpoint.GetInfoAsync("123", TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Equal("123", info!.BroadcasterId);
        Assert.Equal("streamer", info.BroadcasterLogin);
        Assert.Equal("Streamer", info.BroadcasterName);
        Assert.Equal("en", info.BroadcasterLanguage);
        Assert.Equal("game1", info.GameId);
        Assert.Equal("Fortnite", info.GameName);
        Assert.Equal("Playing Fortnite", info.Title);
    }

    [Fact]
    public async Task GetInfoAsync_Single_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(EmptyResponse());

        var info = await endpoint.GetInfoAsync("notexist", TestContext.Current.CancellationToken);

        Assert.Null(info);
    }

    // ── GetInfoAsync (multi) ──────────────────────────────

    [Fact]
    public async Task GetInfoAsync_Multi_ReturnsMultipleChannels()
    {
        var json = """{"data":[{"broadcaster_id":"1","broadcaster_login":"a","broadcaster_name":"A","broadcaster_language":"en","game_id":"g1","game_name":"Game1","title":"T1","delay":0,"tags":[],"is_branded_content":false},{"broadcaster_id":"2","broadcaster_login":"b","broadcaster_name":"B","broadcaster_language":"de","game_id":"g2","game_name":"Game2","title":"T2","delay":0,"tags":[],"is_branded_content":false}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var channels = await endpoint.GetInfoAsync(["1", "2"], TestContext.Current.CancellationToken);

        Assert.Equal(2, channels.Count);
        Assert.Equal("1", channels[0].BroadcasterId);
        Assert.Equal("2", channels[1].BroadcasterId);
    }

    // ── ModifyAsync ───────────────────────────────────────

    [Fact]
    public async Task ModifyAsync_SendsPatchRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var endpoint = CreateEndpoint(handler);

        await endpoint.ModifyAsync(new ModifyChannelRequest(Title: "New Title"), TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest?.Method);
    }

    // ── GetEditorsAsync ───────────────────────────────────

    [Fact]
    public async Task GetEditorsAsync_ReturnsEditors()
    {
        var json = """{"data":[{"user_id":"42","user_name":"EditorOne","created_at":"2021-01-01T00:00:00Z"},{"user_id":"43","user_name":"EditorTwo","created_at":"2021-01-02T00:00:00Z"}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var editors = await endpoint.GetEditorsAsync("123", TestContext.Current.CancellationToken);

        Assert.Equal(2, editors.Count);
        Assert.Equal("42", editors[0].UserId);
        Assert.Equal("EditorOne", editors[0].UserName);
    }

    // ── GetFollowersAsync (paginated) ─────────────────────

    [Fact]
    public async Task GetFollowersAsync_ReturnsPage()
    {
        var json = """{"data":[{"user_id":"10","user_login":"fan","user_name":"Fan","followed_at":"2022-01-01T00:00:00Z"}],"pagination":{"cursor":"abc123"}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetFollowersAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("10", page.Items[0].UserId);
        Assert.Equal("fan", page.Items[0].UserLogin);
        Assert.Equal("abc123", page.Cursor);
        Assert.True(page.HasMore);
    }

    // ── GetAllFollowersAsync ──────────────────────────────

    [Fact]
    public async Task GetAllFollowersAsync_IteratesAllPages()
    {
        var page1 = """{"data":[{"user_id":"1","user_login":"a","user_name":"A","followed_at":"2022-01-01T00:00:00Z"}],"pagination":{"cursor":"next"}}""";
        var page2 = """{"data":[{"user_id":"2","user_login":"b","user_name":"B","followed_at":"2022-01-02T00:00:00Z"}],"pagination":{}}""";
        var endpoint = CreateEndpoint(JsonResponse(page1), JsonResponse(page2));

        var followers = new List<Follower>();
        await foreach (var f in endpoint.GetAllFollowersAsync("123", TestContext.Current.CancellationToken))
            followers.Add(f);

        Assert.Equal(2, followers.Count);
        Assert.Equal("1", followers[0].UserId);
        Assert.Equal("2", followers[1].UserId);
    }

    // ── GetFollowedChannelsAsync ──────────────────────────

    [Fact]
    public async Task GetFollowedChannelsAsync_ReturnsPage()
    {
        var json = """{"data":[{"broadcaster_id":"99","broadcaster_login":"mychannel","broadcaster_name":"MyChannel","followed_at":"2022-06-01T00:00:00Z"}],"pagination":{}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetFollowedChannelsAsync("42", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("99", page.Items[0].BroadcasterId);
        Assert.Equal("mychannel", page.Items[0].BroadcasterLogin);
        Assert.False(page.HasMore);
    }

    // ── Error handling ────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetInfoAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    [Fact]
    public async Task ModifyAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(403, "Forbidden", "Not authorized"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.ModifyAsync(new ModifyChannelRequest(Title: "X"), TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(403, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private ChannelsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        return CreateEndpoint(handler);
    }

    private ChannelsEndpoint CreateEndpoint(FakeHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ChannelsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage ChannelInfoResponse(
        string id, string login, string name, string lang, string gameId, string gameName, string title) =>
        JsonResponse($$"""{"data":[{"broadcaster_id":"{{id}}","broadcaster_login":"{{login}}","broadcaster_name":"{{name}}","broadcaster_language":"{{lang}}","game_id":"{{gameId}}","game_name":"{{gameName}}","title":"{{title}}","delay":0,"tags":[],"is_branded_content":false}]}""");

    private static HttpResponseMessage EmptyResponse() => JsonResponse("""{"data":[]}""");

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
