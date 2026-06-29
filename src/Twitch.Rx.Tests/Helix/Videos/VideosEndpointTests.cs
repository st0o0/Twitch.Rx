using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Videos;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Videos;

public sealed class VideosEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetByIdAsync_ReturnsVideo()
    {
        var endpoint = CreateEndpoint(SingleVideoResponse("v1", "uid1", "login1", "User1", "My Video"));

        var video = await endpoint.GetByIdAsync("v1", TestContext.Current.CancellationToken);

        Assert.NotNull(video);
        Assert.Equal("v1", video!.Id);
        Assert.Equal("uid1", video.UserId);
        Assert.Equal("My Video", video.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var video = await endpoint.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        Assert.Null(video);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMultipleVideos()
    {
        var json = $$"""{"data":[{{VideoJson("v1","uid1","login1","User1","Video1")}},{{VideoJson("v2","uid2","login2","User2","Video2")}}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var videos = await endpoint.GetByIdsAsync(["v1", "v2"], TestContext.Current.CancellationToken);

        Assert.Equal(2, videos.Count);
        Assert.Equal("v1", videos[0].Id);
        Assert.Equal("v2", videos[1].Id);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(PaginatedVideosResponse("v1", null));

        var page = await endpoint.GetByUserAsync("uid1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("v1", page.Items[0].Id);
    }

    [Fact]
    public async Task GetAllByUserAsync_IteratesAllPages()
    {
        var page1 = PaginatedVideosResponse("v1", "cursor123");
        var page2 = PaginatedVideosResponse("v2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var videos = new List<Video>();
        await foreach (var v in endpoint.GetAllByUserAsync("uid1", TestContext.Current.CancellationToken))
            videos.Add(v);

        Assert.Equal(2, videos.Count);
        Assert.Equal("v1", videos[0].Id);
        Assert.Equal("v2", videos[1].Id);
    }

    [Fact]
    public async Task GetByGameAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(PaginatedVideosResponse("v1", null));

        var page = await endpoint.GetByGameAsync("g1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("v1", page.Items[0].Id);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        var endpoint = new VideosEndpoint(httpClient, _errors);

        await endpoint.DeleteAsync(["v1", "v2"], TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("id=v1", handler.LastRequest.RequestUri!.Query);
        Assert.Contains("id=v2", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task GetByIdAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetByIdAsync("v1", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private VideosEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new VideosEndpoint(httpClient, _errors);
    }

    private static string VideoJson(string id, string userId, string userLogin, string userName, string title) =>
        $$"""{"id":"{{id}}","stream_id":"sid1","user_id":"{{userId}}","user_login":"{{userLogin}}","user_name":"{{userName}}","title":"{{title}}","description":"","created_at":"2021-01-01T00:00:00Z","published_at":"2021-01-01T00:00:00Z","url":"https://twitch.tv/videos/{{id}}","thumbnail_url":"https://example.com/t.jpg","viewable":"public","view_count":0,"language":"en","type":"archive","duration":"1h0m0s"}""";

    private static HttpResponseMessage SingleVideoResponse(string id, string userId, string userLogin, string userName, string title) =>
        JsonResponse($$"""{"data":[{{VideoJson(id, userId, userLogin, userName, title)}}]}""");

    private static HttpResponseMessage PaginatedVideosResponse(string id, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse(
            """{"data":[""" + VideoJson(id, "uid1", "login1", "User1", "Video") + """],"pagination":""" + paginationJson + "}");
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
