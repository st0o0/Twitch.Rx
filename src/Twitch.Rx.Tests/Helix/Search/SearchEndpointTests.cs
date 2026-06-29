using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Search;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Search;

public sealed class SearchEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── SearchCategoriesAsync ──────────────────────────────

    [Fact]
    public async Task SearchCategoriesAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(CategoriesPageResponse("12345", "Fortnite", null));

        var page = await endpoint.SearchCategoriesAsync("Fortni", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("12345", page.Items[0].Id);
        Assert.Equal("Fortnite", page.Items[0].Name);
        Assert.Null(page.Cursor);
    }

    // ── SearchChannelsAsync ───────────────────────────────

    [Fact]
    public async Task SearchChannelsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(ChannelsPageResponse("99999", "broadcaster1", null));

        var page = await endpoint.SearchChannelsAsync("broad", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("99999", page.Items[0].Id);
        Assert.Equal("broadcaster1", page.Items[0].BroadcasterLogin);
        Assert.Null(page.Cursor);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task SearchCategoriesAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.SearchCategoriesAsync("test", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private SearchEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new SearchEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage CategoriesPageResponse(string id, string name, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{CategoryJson(id, name)}}],"pagination":{{paginationJson}}}""");
    }

    private static string CategoryJson(string id, string name) => $$"""
        {
          "id": "{{id}}",
          "name": "{{name}}",
          "box_art_url": "https://example.com/art.jpg"
        }
        """;

    private static HttpResponseMessage ChannelsPageResponse(string id, string login, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{ChannelJson(id, login)}}],"pagination":{{paginationJson}}}""");
    }

    private static string ChannelJson(string id, string login) => $$"""
        {
          "id": "{{id}}",
          "broadcaster_login": "{{login}}",
          "display_name": "Broadcaster1",
          "game_id": "33214",
          "game_name": "Fortnite",
          "is_live": true,
          "thumbnail_url": "https://example.com/thumb.jpg",
          "title": "Playing games!",
          "started_at": "2024-01-01T00:00:00Z",
          "broadcaster_language": "en"
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
