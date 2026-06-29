using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Games;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Games;

public sealed class GamesEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetTopGamesAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(GamesResponse("g1", "Game1"));

        var page = await endpoint.GetTopGamesAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("g1", page.Items[0].Id);
        Assert.Equal("Game1", page.Items[0].Name);
    }

    [Fact]
    public async Task GetAllTopGamesAsync_IteratesAllPages()
    {
        var page1 = PaginatedGamesResponse("g1", "cursor123");
        var page2 = PaginatedGamesResponse("g2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var games = new List<Game>();
        await foreach (var g in endpoint.GetAllTopGamesAsync(TestContext.Current.CancellationToken))
            games.Add(g);

        Assert.Equal(2, games.Count);
        Assert.Equal("g1", games[0].Id);
        Assert.Equal("g2", games[1].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsGame()
    {
        var endpoint = CreateEndpoint(SingleGameResponse("g1", "Game1"));

        var game = await endpoint.GetByIdAsync("g1", TestContext.Current.CancellationToken);

        Assert.NotNull(game);
        Assert.Equal("g1", game!.Id);
        Assert.Equal("Game1", game.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var game = await endpoint.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        Assert.Null(game);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsGame()
    {
        var endpoint = CreateEndpoint(SingleGameResponse("g1", "Fortnite"));

        var game = await endpoint.GetByNameAsync("Fortnite", TestContext.Current.CancellationToken);

        Assert.NotNull(game);
        Assert.Equal("Fortnite", game!.Name);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMultipleGames()
    {
        var json = """{"data":[{"id":"g1","name":"Game1","box_art_url":"https://example.com/g1.jpg","igdb_id":"ig1"},{"id":"g2","name":"Game2","box_art_url":"https://example.com/g2.jpg","igdb_id":"ig2"}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var games = await endpoint.GetByIdsAsync(["g1", "g2"], TestContext.Current.CancellationToken);

        Assert.Equal(2, games.Count);
        Assert.Equal("g1", games[0].Id);
        Assert.Equal("g2", games[1].Id);
    }

    [Fact]
    public async Task GetTopGamesAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetTopGamesAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private GamesEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new GamesEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage GamesResponse(string id, string name) =>
        JsonResponse($$"""{"data":[{"id":"{{id}}","name":"{{name}}","box_art_url":"https://example.com/art.jpg","igdb_id":"ig1"}]}""");

    private static HttpResponseMessage SingleGameResponse(string id, string name) =>
        JsonResponse($$"""{"data":[{"id":"{{id}}","name":"{{name}}","box_art_url":"https://example.com/art.jpg","igdb_id":"ig1"}]}""");

    private static HttpResponseMessage PaginatedGamesResponse(string id, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse(
            $$"""{"data":[{"id":"{{id}}","name":"Game","box_art_url":"https://example.com/art.jpg","igdb_id":"ig1"}],"pagination":""" + paginationJson + "}");
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
