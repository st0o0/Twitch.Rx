using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Moderation;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Moderation;

public sealed class ModerationEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── BanUserAsync ───────────────────────────────────────

    [Fact]
    public async Task BanUserAsync_ReturnsBanResponse()
    {
        var endpoint = CreateEndpoint(BanResponseListResponse());

        var ban = await endpoint.BanUserAsync(
            "123", "mod1",
            new BanUserRequest("user1", Duration: 600, Reason: "spam"),
            TestContext.Current.CancellationToken);

        Assert.Equal("123", ban.BroadcasterId);
        Assert.Equal("mod1", ban.ModeratorId);
        Assert.Equal("user1", ban.UserId);
    }

    // ── UnbanUserAsync ─────────────────────────────────────

    [Fact]
    public async Task UnbanUserAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.UnbanUserAsync("123", "mod1", "user1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── GetBannedUsersAsync ────────────────────────────────

    [Fact]
    public async Task GetBannedUsersAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(BannedUsersPageResponse("user1", null));

        var page = await endpoint.GetBannedUsersAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("user1", page.Items[0].UserId);
        Assert.Equal("spam", page.Items[0].Reason);
        Assert.Null(page.Cursor);
    }

    // ── GetAllBannedUsersAsync ─────────────────────────────

    [Fact]
    public async Task GetAllBannedUsersAsync_IteratesAllPages()
    {
        var page1 = BannedUsersPageResponse("user1", "cursor-next");
        var page2 = BannedUsersPageResponse("user2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var bannedUsers = new List<BannedUser>();
        await foreach (var u in endpoint.GetAllBannedUsersAsync("123", TestContext.Current.CancellationToken))
            bannedUsers.Add(u);

        Assert.Equal(2, bannedUsers.Count);
        Assert.Equal("user1", bannedUsers[0].UserId);
        Assert.Equal("user2", bannedUsers[1].UserId);
    }

    // ── GetBlockedTermsAsync ───────────────────────────────

    [Fact]
    public async Task GetBlockedTermsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(BlockedTermsPageResponse("term-1", "badword", null));

        var page = await endpoint.GetBlockedTermsAsync("123", "mod1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("term-1", page.Items[0].Id);
        Assert.Equal("badword", page.Items[0].Text);
    }

    // ── AddBlockedTermAsync ────────────────────────────────

    [Fact]
    public async Task AddBlockedTermAsync_ReturnsBlockedTerm()
    {
        var endpoint = CreateEndpoint(BlockedTermListResponse("term-new", "newbadword"));

        var term = await endpoint.AddBlockedTermAsync("123", "mod1", "newbadword", TestContext.Current.CancellationToken);

        Assert.Equal("term-new", term.Id);
        Assert.Equal("newbadword", term.Text);
    }

    // ── RemoveBlockedTermAsync ─────────────────────────────

    [Fact]
    public async Task RemoveBlockedTermAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.RemoveBlockedTermAsync("123", "mod1", "term-1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── GetAutoModSettingsAsync ────────────────────────────

    [Fact]
    public async Task GetAutoModSettingsAsync_ReturnsSettings()
    {
        var endpoint = CreateEndpoint(AutoModSettingsListResponse());

        var settings = await endpoint.GetAutoModSettingsAsync("123", "mod1", TestContext.Current.CancellationToken);

        Assert.Equal("123", settings.BroadcasterId);
        Assert.Equal("mod1", settings.ModeratorId);
        Assert.Equal(2, settings.Aggression);
    }

    // ── UpdateAutoModSettingsAsync ─────────────────────────

    [Fact]
    public async Task UpdateAutoModSettingsAsync_ReturnsUpdatedSettings()
    {
        var endpoint = CreateEndpoint(AutoModSettingsListResponse());

        var settings = await endpoint.UpdateAutoModSettingsAsync(
            "123", "mod1",
            new UpdateAutoModSettingsRequest(Aggression: 2),
            TestContext.Current.CancellationToken);

        Assert.Equal("123", settings.BroadcasterId);
        Assert.Equal(2, settings.Aggression);
    }

    // ── GetModeratorsAsync ─────────────────────────────────

    [Fact]
    public async Task GetModeratorsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(ModeratorsPageResponse("mod1", null));

        var page = await endpoint.GetModeratorsAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("mod1", page.Items[0].UserId);
    }

    // ── GetAllModeratorsAsync ──────────────────────────────

    [Fact]
    public async Task GetAllModeratorsAsync_IteratesAllPages()
    {
        var page1 = ModeratorsPageResponse("mod1", "cursor-next");
        var page2 = ModeratorsPageResponse("mod2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var mods = new List<Moderator>();
        await foreach (var m in endpoint.GetAllModeratorsAsync("123", TestContext.Current.CancellationToken))
            mods.Add(m);

        Assert.Equal(2, mods.Count);
        Assert.Equal("mod1", mods[0].UserId);
        Assert.Equal("mod2", mods[1].UserId);
    }

    // ── AddModeratorAsync ──────────────────────────────────

    [Fact]
    public async Task AddModeratorAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.AddModeratorAsync("123", "user1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── RemoveModeratorAsync ───────────────────────────────

    [Fact]
    public async Task RemoveModeratorAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.RemoveModeratorAsync("123", "mod1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── GetVIPsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetVIPsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(VipsPageResponse("vip1", null));

        var page = await endpoint.GetVIPsAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("vip1", page.Items[0].UserId);
    }

    // ── AddVIPAsync ────────────────────────────────────────

    [Fact]
    public async Task AddVIPAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.AddVIPAsync("123", "user1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── RemoveVIPAsync ─────────────────────────────────────

    [Fact]
    public async Task RemoveVIPAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.RemoveVIPAsync("123", "vip1", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── GetShieldModeStatusAsync ───────────────────────────

    [Fact]
    public async Task GetShieldModeStatusAsync_ReturnsStatus()
    {
        var endpoint = CreateEndpoint(ShieldModeStatusListResponse(false));

        var status = await endpoint.GetShieldModeStatusAsync("123", "mod1", TestContext.Current.CancellationToken);

        Assert.False(status.IsActive);
        Assert.Equal("mod1", status.ModeratorId);
    }

    // ── UpdateShieldModeStatusAsync ────────────────────────

    [Fact]
    public async Task UpdateShieldModeStatusAsync_ReturnsUpdatedStatus()
    {
        var endpoint = CreateEndpoint(ShieldModeStatusListResponse(true));

        var status = await endpoint.UpdateShieldModeStatusAsync("123", "mod1", true, TestContext.Current.CancellationToken);

        Assert.True(status.IsActive);
        Assert.Equal("mod1", status.ModeratorId);
    }

    // ── WarnUserAsync ──────────────────────────────────────

    [Fact]
    public async Task WarnUserAsync_CompletesSuccessfully()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.WarnUserAsync("123", "mod1", "user1", "Please follow the rules.", TestContext.Current.CancellationToken);
        // No exception = success
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task BanUserAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.BanUserAsync("123", "mod1", new BanUserRequest("user1"), TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    [Fact]
    public async Task GetBannedUsersAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(403, "Forbidden", "Not authorized"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetBannedUsersAsync("123", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(403, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private ModerationEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ModerationEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage BanResponseListResponse() =>
        JsonResponse($$"""
        {
          "data": [{
            "broadcaster_id": "123",
            "broadcaster_login": "streamer",
            "broadcaster_name": "Streamer",
            "moderator_id": "mod1",
            "moderator_login": "moduser",
            "moderator_name": "ModUser",
            "user_id": "user1",
            "user_login": "baduser",
            "user_name": "BadUser",
            "created_at": "2024-01-01T00:00:00Z",
            "end_time": "2024-01-01T00:10:00Z"
          }]
        }
        """);

    private static HttpResponseMessage BannedUsersPageResponse(string userId, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse($$"""{"data":[{{BannedUserJson(userId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string BannedUserJson(string userId) => $$"""
        {
          "user_id": "{{userId}}",
          "user_login": "baduser",
          "user_name": "BadUser",
          "expires_at": null,
          "created_at": "2024-01-01T00:00:00Z",
          "reason": "spam",
          "moderator_id": "mod1",
          "moderator_login": "moduser",
          "moderator_name": "ModUser"
        }
        """;

    private static HttpResponseMessage BlockedTermsPageResponse(string id, string text, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse($$"""{"data":[{{BlockedTermJson(id, text)}}],"pagination":{{paginationJson}}}""");
    }

    private static HttpResponseMessage BlockedTermListResponse(string id, string text) =>
        JsonResponse($$"""{"data":[{{BlockedTermJson(id, text)}}]}""");

    private static string BlockedTermJson(string id, string text) => $$"""
        {
          "broadcaster_id": "123",
          "moderator_id": "mod1",
          "id": "{{id}}",
          "text": "{{text}}",
          "created_at": "2024-01-01T00:00:00Z",
          "updated_at": "2024-01-01T00:00:00Z",
          "expires_at": null
        }
        """;

    private static HttpResponseMessage AutoModSettingsListResponse() =>
        JsonResponse("""
        {
          "data": [{
            "broadcaster_id": "123",
            "moderator_id": "mod1",
            "overall_level": null,
            "disability": 0,
            "aggression": 2,
            "sexuality": 0,
            "misogyny": 0,
            "bullying": 0,
            "swearing": 0,
            "race_ethnicity_or_religion": 0,
            "sex_based_terms": 0
          }]
        }
        """);

    private static HttpResponseMessage ModeratorsPageResponse(string userId, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse($$"""{"data":[{{ModeratorJson(userId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string ModeratorJson(string userId) => $$"""
        {
          "user_id": "{{userId}}",
          "user_login": "moduser",
          "user_name": "ModUser"
        }
        """;

    private static HttpResponseMessage VipsPageResponse(string userId, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse($$"""{"data":[{{VipJson(userId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string VipJson(string userId) => $$"""
        {
          "user_id": "{{userId}}",
          "user_login": "vipuser",
          "user_name": "VipUser"
        }
        """;

    private static HttpResponseMessage ShieldModeStatusListResponse(bool isActive) =>
        JsonResponse($$"""
        {
          "data": [{
            "is_active": {{(isActive ? "true" : "false")}},
            "moderator_id": "mod1",
            "moderator_login": "moduser",
            "moderator_name": "ModUser",
            "last_activated_at": "2024-01-01T00:00:00Z"
          }]
        }
        """);

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
