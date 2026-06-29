using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.GuestStar;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.GuestStar;

public sealed class GuestStarEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    private const string SettingsJson =
        """{"broadcaster_id":"123","is_moderator_send_live_enabled":true,"slot_count":5,"is_browser_source_audio_enabled":false,"group_layout":"TILED_LAYOUT","browser_source_token":"tok123"}""";

    private const string SessionJson =
        """{"id":"sess-1","guests":[]}""";

    private const string InviteJson =
        """{"user_id":"user-1","invited_at":"2024-01-01T00:00:00Z","status":"INVITED","is_video_enabled":true,"is_audio_enabled":true,"is_video_available":true,"is_audio_available":true}""";

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings()
    {
        var endpoint = CreateEndpoint(JsonResponse($$"""{"data":[{{SettingsJson}}]}"""));

        var result = await endpoint.GetSettingsAsync("123", TestContext.Current.CancellationToken);

        Assert.Equal("123", result.BroadcasterId);
        Assert.True(result.IsModeratorSendLiveEnabled);
        Assert.Equal(5, result.SlotCount);
        Assert.Equal("TILED_LAYOUT", result.GroupLayout);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.UpdateSettingsAsync("123",
            new UpdateGuestStarSettingsRequest(SlotCount: 3),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsSession()
    {
        var endpoint = CreateEndpoint(JsonResponse($$"""{"data":[{{SessionJson}}]}"""));

        var result = await endpoint.GetSessionAsync("123", "mod-1", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("sess-1", result!.Id);
        Assert.Empty(result.Guests);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenEmpty()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var result = await endpoint.GetSessionAsync("123", "mod-1", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsSession()
    {
        var endpoint = CreateEndpoint(JsonResponse($$"""{"data":[{{SessionJson}}]}"""));

        var result = await endpoint.CreateSessionAsync("123", TestContext.Current.CancellationToken);

        Assert.Equal("sess-1", result.Id);
    }

    [Fact]
    public async Task EndSessionAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.EndSessionAsync("123", "sess-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetInvitesAsync_ReturnsInvites()
    {
        var endpoint = CreateEndpoint(JsonResponse($$"""{"data":[{{InviteJson}}]}"""));

        var result = await endpoint.GetInvitesAsync("123", "mod-1", "sess-1", TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("user-1", result[0].UserId);
        Assert.Equal("INVITED", result[0].Status);
    }

    [Fact]
    public async Task SendInviteAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.SendInviteAsync("123", "mod-1", "sess-1", "guest-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteInviteAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.DeleteInviteAsync("123", "mod-1", "sess-1", "guest-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AssignSlotAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.AssignSlotAsync("123", "mod-1", "sess-1", "guest-1", "slot-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateSlotAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.UpdateSlotAsync("123", "mod-1", "sess-1", "slot-1", "slot-2", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteSlotAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.DeleteSlotAsync("123", "mod-1", "sess-1", "guest-1", "slot-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSettingsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetSettingsAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private GuestStarEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new GuestStarEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage NoContentResponse() => new(HttpStatusCode.NoContent);

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
