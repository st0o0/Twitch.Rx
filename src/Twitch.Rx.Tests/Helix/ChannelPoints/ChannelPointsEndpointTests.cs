using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.ChannelPoints;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.ChannelPoints;

public sealed class ChannelPointsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetCustomRewardsAsync ──────────────────────────────

    [Fact]
    public async Task GetCustomRewardsAsync_ReturnsList()
    {
        var endpoint = CreateEndpoint(RewardsListResponse("reward-1", "Test Reward"));

        var rewards = await endpoint.GetCustomRewardsAsync("123", TestContext.Current.CancellationToken);

        Assert.Single(rewards);
        Assert.Equal("reward-1", rewards[0].Id);
        Assert.Equal("Test Reward", rewards[0].Title);
        Assert.Equal(100, rewards[0].Cost);
    }

    // ── GetCustomRewardAsync ───────────────────────────────

    [Fact]
    public async Task GetCustomRewardAsync_ReturnsSingleReward()
    {
        var endpoint = CreateEndpoint(RewardsListResponse("reward-1", "Test Reward"));

        var reward = await endpoint.GetCustomRewardAsync("123", "reward-1", TestContext.Current.CancellationToken);

        Assert.NotNull(reward);
        Assert.Equal("reward-1", reward!.Id);
        Assert.Equal("Test Reward", reward.Title);
    }

    [Fact]
    public async Task GetCustomRewardAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var reward = await endpoint.GetCustomRewardAsync("123", "nonexistent", TestContext.Current.CancellationToken);

        Assert.Null(reward);
    }

    // ── CreateCustomRewardAsync ────────────────────────────

    [Fact]
    public async Task CreateCustomRewardAsync_ReturnsCreatedReward()
    {
        var endpoint = CreateEndpoint(RewardsListResponse("reward-new", "New Reward"));

        var reward = await endpoint.CreateCustomRewardAsync(
            new CreateCustomRewardRequest("123", "New Reward", 100),
            TestContext.Current.CancellationToken);

        Assert.Equal("reward-new", reward.Id);
        Assert.Equal("New Reward", reward.Title);
    }

    // ── UpdateCustomRewardAsync ────────────────────────────

    [Fact]
    public async Task UpdateCustomRewardAsync_ReturnsUpdatedReward()
    {
        var endpoint = CreateEndpoint(RewardsListResponse("reward-1", "Updated Reward"));

        var reward = await endpoint.UpdateCustomRewardAsync(
            "123", "reward-1",
            new UpdateCustomRewardRequest(Title: "Updated Reward"),
            TestContext.Current.CancellationToken);

        Assert.Equal("reward-1", reward.Id);
        Assert.Equal("Updated Reward", reward.Title);
    }

    // ── DeleteCustomRewardAsync ────────────────────────────

    [Fact]
    public async Task DeleteCustomRewardAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.DeleteCustomRewardAsync("123", "reward-1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── GetRedemptionsAsync ────────────────────────────────

    [Fact]
    public async Task GetRedemptionsAsync_ReturnsPaginatedRedemptions()
    {
        var endpoint = CreateEndpoint(RedemptionsPageResponse("redemption-1", null));

        var page = await endpoint.GetRedemptionsAsync(
            "123", "reward-1", "UNFULFILLED",
            ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("redemption-1", page.Items[0].Id);
        Assert.Equal("UNFULFILLED", page.Items[0].Status);
        Assert.Null(page.Cursor);
    }

    // ── GetAllRedemptionsAsync ─────────────────────────────

    [Fact]
    public async Task GetAllRedemptionsAsync_IteratesAllPages()
    {
        var page1 = RedemptionsPageResponse("redemption-1", "cursor-next");
        var page2 = RedemptionsPageResponse("redemption-2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var redemptions = new List<Redemption>();
        await foreach (var r in endpoint.GetAllRedemptionsAsync("123", "reward-1", "UNFULFILLED", TestContext.Current.CancellationToken))
            redemptions.Add(r);

        Assert.Equal(2, redemptions.Count);
        Assert.Equal("redemption-1", redemptions[0].Id);
        Assert.Equal("redemption-2", redemptions[1].Id);
    }

    // ── UpdateRedemptionStatusAsync ────────────────────────

    [Fact]
    public async Task UpdateRedemptionStatusAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.UpdateRedemptionStatusAsync(
            "123", "reward-1",
            ["redemption-1", "redemption-2"],
            "FULFILLED",
            TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetCustomRewardsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetCustomRewardsAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    [Fact]
    public async Task CreateCustomRewardAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(403, "Forbidden", "Not authorized"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.CreateCustomRewardAsync(
                new CreateCustomRewardRequest("123", "Reward", 100),
                TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(403, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private ChannelPointsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ChannelPointsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage RewardsListResponse(string id, string title) =>
        JsonResponse($$"""{"data":[{{RewardJson(id, title)}}]}""");

    private static string RewardJson(string id, string title) => $$"""
        {
          "broadcaster_id": "123",
          "broadcaster_login": "streamer",
          "broadcaster_name": "Streamer",
          "id": "{{id}}",
          "title": "{{title}}",
          "prompt": "Test Prompt",
          "cost": 100,
          "background_color": "#FF0000",
          "is_enabled": true,
          "is_user_input_required": false,
          "max_per_stream_setting": {"is_enabled": false, "max_per_stream": 0},
          "max_per_user_per_stream_setting": {"is_enabled": false, "max_per_user_per_stream": 0},
          "global_cooldown_setting": {"is_enabled": false, "global_cooldown_seconds": 0},
          "is_paused": false,
          "is_in_stock": true,
          "should_redemptions_skip_request_queue": false,
          "redemptions_redeemed_current_stream": null,
          "cooldown_expires_at": null
        }
        """;

    private static HttpResponseMessage RedemptionsPageResponse(string id, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse($$"""{"data":[{{RedemptionJson(id)}}],"pagination":{{paginationJson}}}""");
    }

    private static string RedemptionJson(string id) => $$"""
        {
          "broadcaster_id": "123",
          "broadcaster_login": "streamer",
          "broadcaster_name": "Streamer",
          "id": "{{id}}",
          "user_id": "user1",
          "user_login": "testuser",
          "user_name": "TestUser",
          "user_input": "",
          "status": "UNFULFILLED",
          "redeemed_at": "2024-01-01T00:00:00Z",
          "reward": {"id": "reward-1", "title": "Test Reward", "prompt": "Test Prompt", "cost": 100}
        }
        """;

    private static HttpResponseMessage NoContentResponse() =>
        new(HttpStatusCode.NoContent);

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
