using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Bits;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Bits;

public sealed class BitsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetLeaderboardAsync ───────────────────────────────

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsLeaderboard()
    {
        var endpoint = CreateEndpoint(LeaderboardResponse());

        var leaderboard = await endpoint.GetLeaderboardAsync(ct: TestContext.Current.CancellationToken);

        Assert.Equal(1, leaderboard.Total);
        Assert.Single(leaderboard.Entries);
        Assert.Equal("158010205", leaderboard.Entries[0].UserId);
        Assert.Equal(1, leaderboard.Entries[0].Rank);
        Assert.Equal(12543, leaderboard.Entries[0].Score);
        Assert.Equal("2018-02-05T08:00:00Z", leaderboard.DateRange.StartedAt);
    }

    // ── GetCheermotesAsync ────────────────────────────────

    [Fact]
    public async Task GetCheermotesAsync_ReturnsCheermotes()
    {
        var endpoint = CreateEndpoint(CheermotesResponse());

        var cheermotes = await endpoint.GetCheermotesAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(cheermotes);
        Assert.Equal("Cheer", cheermotes[0].Prefix);
        Assert.Equal("global_first_party", cheermotes[0].Type);
        Assert.Single(cheermotes[0].Tiers);
        Assert.Equal(1, cheermotes[0].Tiers[0].MinBits);
    }

    // ── GetExtensionTransactionsAsync ─────────────────────

    [Fact]
    public async Task GetExtensionTransactionsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(TransactionsPageResponse());

        var page = await endpoint.GetExtensionTransactionsAsync("ext-id", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("tx-1", page.Items[0].Id);
        Assert.Equal("BITS_IN_EXTENSION", page.Items[0].ProductType);
        Assert.Equal("large", page.Items[0].ProductData.Sku);
        Assert.Equal(256, page.Items[0].ProductData.Cost.Amount);
    }

    // ── GetAllExtensionTransactionsAsync ──────────────────

    [Fact]
    public async Task GetAllExtensionTransactionsAsync_IteratesAllPages()
    {
        var page1 = TransactionsPageResponseWithCursor("tx-1", "cursor-next");
        var page2 = TransactionsPageResponse("tx-2");
        var endpoint = CreateEndpoint(page1, page2);

        var transactions = new List<ExtensionTransaction>();
        await foreach (var tx in endpoint.GetAllExtensionTransactionsAsync("ext-id", TestContext.Current.CancellationToken))
            transactions.Add(tx);

        Assert.Equal(2, transactions.Count);
        Assert.Equal("tx-1", transactions[0].Id);
        Assert.Equal("tx-2", transactions[1].Id);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetLeaderboardAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetLeaderboardAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private BitsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new BitsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage LeaderboardResponse() =>
        JsonResponse("""
        {
          "data": [{
            "user_id": "158010205",
            "user_login": "twitchdev1",
            "user_name": "TwitchDev1",
            "rank": 1,
            "score": 12543
          }],
          "date_range": {
            "started_at": "2018-02-05T08:00:00Z",
            "ended_at": "2018-02-12T08:00:00Z"
          },
          "total": 1
        }
        """);

    private static HttpResponseMessage CheermotesResponse() =>
        JsonResponse("""
        {
          "data": [{
            "prefix": "Cheer",
            "tiers": [{"min_bits": 1, "id": "1", "color": "#979797", "can_cheer": true, "show_in_bits_card": true}],
            "type": "global_first_party",
            "order": 1,
            "last_updated": "2018-05-22T00:06:04Z",
            "is_charitable": false
          }]
        }
        """);

    private static HttpResponseMessage TransactionsPageResponse(string id = "tx-1", string cursor = "") =>
        JsonResponse($$"""
        {
          "data": [{{TransactionJson(id)}}],
          "pagination": {{(string.IsNullOrEmpty(cursor) ? "{}" : $"{{\"cursor\":\"{cursor}\"}}") }}
        }
        """);

    private static HttpResponseMessage TransactionsPageResponseWithCursor(string id, string cursor) =>
        TransactionsPageResponse(id, cursor);

    private static string TransactionJson(string id) => $$"""
        {
          "id": "{{id}}",
          "timestamp": "2019-01-28T04:15:53Z",
          "broadcaster_user_id": "439964613",
          "broadcaster_user_login": "broadcaster",
          "broadcaster_user_name": "Broadcaster",
          "user_id": "139712484",
          "user_login": "user1",
          "user_name": "User1",
          "product_type": "BITS_IN_EXTENSION",
          "product_data": {
            "domain": "twitch.ext.93138sf",
            "sku": "large",
            "cost": {"amount": 256, "type": "bits"},
            "inDevelopment": false,
            "displayName": "Large",
            "expiration": "",
            "broadcast": false
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
