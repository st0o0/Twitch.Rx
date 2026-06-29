using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Teams;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Teams;

public sealed class TeamsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetByIdAsync ───────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsTeam()
    {
        var endpoint = CreateEndpoint(TeamListResponse("team-1", "monstercat"));

        var team = await endpoint.GetByIdAsync("team-1", TestContext.Current.CancellationToken);

        Assert.NotNull(team);
        Assert.Equal("team-1", team!.Id);
        Assert.Equal("monstercat", team.TeamName);
    }

    // ── GetByNameAsync ─────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_ReturnsTeam()
    {
        var endpoint = CreateEndpoint(TeamListResponse("team-2", "monstercat"));

        var team = await endpoint.GetByNameAsync("monstercat", TestContext.Current.CancellationToken);

        Assert.NotNull(team);
        Assert.Equal("team-2", team!.Id);
    }

    // ── GetChannelTeamsAsync ───────────────────────────────

    [Fact]
    public async Task GetChannelTeamsAsync_ReturnsList()
    {
        var endpoint = CreateEndpoint(ChannelTeamListResponse("123", "team-1", "monstercat"));

        var teams = await endpoint.GetChannelTeamsAsync("123", TestContext.Current.CancellationToken);

        Assert.Single(teams);
        Assert.Equal("team-1", teams[0].Id);
        Assert.Equal("123", teams[0].BroadcasterId);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetByIdAsync("team-1", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private TeamsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new TeamsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage TeamListResponse(string id, string teamName) =>
        JsonResponse($$"""{"data":[{{TeamJson(id, teamName)}}]}""");

    private static string TeamJson(string id, string teamName) => $$"""
        {
          "id": "{{id}}",
          "team_name": "{{teamName}}",
          "team_display_name": "Monstercat",
          "info": "A gaming team",
          "thumbnail_url": "https://example.com/thumb.jpg",
          "background_image_url": "https://example.com/bg.jpg",
          "banner": "https://example.com/banner.jpg",
          "created_at": "2024-01-01T00:00:00Z",
          "updated_at": "2024-01-01T00:00:00Z"
        }
        """;

    private static HttpResponseMessage ChannelTeamListResponse(string broadcasterId, string teamId, string teamName) =>
        JsonResponse($$"""{"data":[{{ChannelTeamJson(broadcasterId, teamId, teamName)}}]}""");

    private static string ChannelTeamJson(string broadcasterId, string teamId, string teamName) => $$"""
        {
          "id": "{{teamId}}",
          "team_name": "{{teamName}}",
          "team_display_name": "Monstercat",
          "info": "A gaming team",
          "thumbnail_url": "https://example.com/thumb.jpg",
          "background_image_url": "https://example.com/bg.jpg",
          "banner": "https://example.com/banner.jpg",
          "created_at": "2024-01-01T00:00:00Z",
          "updated_at": "2024-01-01T00:00:00Z",
          "broadcaster_id": "{{broadcasterId}}",
          "broadcaster_login": "broadcaster",
          "broadcaster_name": "Broadcaster"
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
