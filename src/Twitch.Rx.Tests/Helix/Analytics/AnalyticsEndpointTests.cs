using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Analytics;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Analytics;

public sealed class AnalyticsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetExtensionAnalyticsAsync ────────────────────────

    [Fact]
    public async Task GetExtensionAnalyticsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(ExtensionAnalyticsPageResponse("ext-1", null));

        var page = await endpoint.GetExtensionAnalyticsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("ext-1", page.Items[0].ExtensionId);
        Assert.Null(page.Cursor);
    }

    // ── GetGameAnalyticsAsync ─────────────────────────────

    [Fact]
    public async Task GetGameAnalyticsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(GameAnalyticsPageResponse("game-1", null));

        var page = await endpoint.GetGameAnalyticsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("game-1", page.Items[0].GameId);
        Assert.Null(page.Cursor);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetExtensionAnalyticsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetExtensionAnalyticsAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private AnalyticsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new AnalyticsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage ExtensionAnalyticsPageResponse(string extensionId, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{ExtensionAnalyticsJson(extensionId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string ExtensionAnalyticsJson(string extensionId) => $$"""
        {
          "extension_id": "{{extensionId}}",
          "URL": "https://example.com/report",
          "type": "overview_v2",
          "date_range": {
            "started_at": "2024-01-01T00:00:00Z",
            "ended_at": "2024-02-01T00:00:00Z"
          }
        }
        """;

    private static HttpResponseMessage GameAnalyticsPageResponse(string gameId, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{GameAnalyticsJson(gameId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string GameAnalyticsJson(string gameId) => $$"""
        {
          "game_id": "{{gameId}}",
          "URL": "https://example.com/report",
          "type": "overview_v2",
          "date_range": {
            "started_at": "2024-01-01T00:00:00Z",
            "ended_at": "2024-02-01T00:00:00Z"
          }
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
