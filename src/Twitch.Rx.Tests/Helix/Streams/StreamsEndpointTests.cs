using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Streams;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Streams;

public sealed class StreamsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetStreamsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(StreamsResponse("s1", "uid1", "login1", "User1"));

        var page = await endpoint.GetStreamsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("s1", page.Items[0].Id);
        Assert.Equal("uid1", page.Items[0].UserId);
        Assert.Equal("User1", page.Items[0].UserName);
    }

    [Fact]
    public async Task GetAllStreamsAsync_IteratesAllPages()
    {
        var page1 = PaginatedStreamsResponse("s1", "cursor123");
        var page2 = PaginatedStreamsResponse("s2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var streams = new List<TwitchStream>();
        await foreach (var s in endpoint.GetAllStreamsAsync(ct: TestContext.Current.CancellationToken))
            streams.Add(s);

        Assert.Equal(2, streams.Count);
        Assert.Equal("s1", streams[0].Id);
        Assert.Equal("s2", streams[1].Id);
    }

    [Fact]
    public async Task GetFollowedStreamsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(StreamsResponse("s1", "uid1", "login1", "User1"));

        var page = await endpoint.GetFollowedStreamsAsync("uid1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("s1", page.Items[0].Id);
    }

    [Fact]
    public async Task CreateMarkerAsync_ReturnsMarker()
    {
        var json = """{"data":[{"id":"123","created_at":"2021-01-01T00:00:00Z","description":"test","position_seconds":42}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var marker = await endpoint.CreateMarkerAsync("uid1", "test", TestContext.Current.CancellationToken);

        Assert.NotNull(marker);
        Assert.Equal("123", marker.Id);
        Assert.Equal("test", marker.Description);
        Assert.Equal(42, marker.PositionSeconds);
    }

    [Fact]
    public async Task GetMarkersAsync_ReturnsPage()
    {
        var json = """{"data":[{"user_id":"uid1","user_name":"UserName","user_login":"userlogin","videos":[{"video_id":"v1","markers":[{"id":"m1","created_at":"2021-01-01T00:00:00Z","description":"test","position_seconds":10}]}]}],"pagination":{}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetMarkersAsync("uid1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        var group = page.Items[0];
        Assert.Equal("uid1", group.UserId);
        Assert.Single(group.Videos);
        Assert.Equal("v1", group.Videos[0].VideoId);
        Assert.Single(group.Videos[0].Markers);
        Assert.Equal("m1", group.Videos[0].Markers[0].Id);
        Assert.Equal(10, group.Videos[0].Markers[0].PositionSeconds);
    }

    [Fact]
    public async Task GetStreamsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetStreamsAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private StreamsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new StreamsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage StreamsResponse(string id, string userId, string userLogin, string userName) =>
        JsonResponse($$"""{"data":[{"id":"{{id}}","user_id":"{{userId}}","user_login":"{{userLogin}}","user_name":"{{userName}}","game_id":"g1","game_name":"Game1","type":"live","title":"Title","viewer_count":100,"started_at":"2021-01-01T00:00:00Z","language":"en","thumbnail_url":"https://example.com/t.jpg","is_mature":false}]}""");

    private static HttpResponseMessage PaginatedStreamsResponse(string id, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse(
            $$"""{"data":[{"id":"{{id}}","user_id":"uid1","user_login":"login1","user_name":"User1","game_id":"g1","game_name":"Game1","type":"live","title":"Title","viewer_count":100,"started_at":"2021-01-01T00:00:00Z","language":"en","thumbnail_url":"https://example.com/t.jpg","is_mature":false}],"pagination":""" + paginationJson + "}");
    }

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
