using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Chat;

public sealed class ChatEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetChattersAsync (paginated) ──────────────────────

    [Fact]
    public async Task GetChattersAsync_ReturnsPage()
    {
        var json = """{"data":[{"user_id":"10","user_login":"viewer1","user_name":"Viewer1"},{"user_id":"11","user_login":"viewer2","user_name":"Viewer2"}],"pagination":{"cursor":"cur1"}}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var page = await endpoint.GetChattersAsync("123", "mod456", ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal("10", page.Items[0].UserId);
        Assert.Equal("viewer1", page.Items[0].UserLogin);
        Assert.Equal("cur1", page.Cursor);
        Assert.True(page.HasMore);
    }

    // ── GetAllChattersAsync ───────────────────────────────

    [Fact]
    public async Task GetAllChattersAsync_IteratesAllPages()
    {
        var page1 = """{"data":[{"user_id":"1","user_login":"a","user_name":"A"}],"pagination":{"cursor":"next"}}""";
        var page2 = """{"data":[{"user_id":"2","user_login":"b","user_name":"B"}],"pagination":{}}""";
        var endpoint = CreateEndpoint(JsonResponse(page1), JsonResponse(page2));

        var chatters = new List<Chatter>();
        await foreach (var c in endpoint.GetAllChattersAsync("123", "mod456", TestContext.Current.CancellationToken))
            chatters.Add(c);

        Assert.Equal(2, chatters.Count);
        Assert.Equal("1", chatters[0].UserId);
        Assert.Equal("2", chatters[1].UserId);
    }

    // ── GetChannelEmotesAsync ─────────────────────────────

    [Fact]
    public async Task GetChannelEmotesAsync_ReturnsEmotes()
    {
        var json = """{"data":[{"id":"e1","name":"MyEmote","images":{"url_1x":"https://ex.com/1x","url_2x":"https://ex.com/2x","url_4x":"https://ex.com/4x"},"tier":"1000","emote_type":"subscriptions","emote_set_id":"set1","format":["static"],"scale":["1.0"],"theme_mode":["light"]}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var emotes = await endpoint.GetChannelEmotesAsync("123", TestContext.Current.CancellationToken);

        Assert.Single(emotes);
        Assert.Equal("e1", emotes[0].Id);
        Assert.Equal("MyEmote", emotes[0].Name);
        Assert.Equal("https://ex.com/1x", emotes[0].Images.Url1X);
        Assert.Equal("1000", emotes[0].Tier);
    }

    // ── GetGlobalEmotesAsync ──────────────────────────────

    [Fact]
    public async Task GetGlobalEmotesAsync_ReturnsEmotes()
    {
        var json = """{"data":[{"id":"g1","name":"GlobalEmote","images":{"url_1x":"https://ex.com/1x","url_2x":"https://ex.com/2x","url_4x":"https://ex.com/4x"},"tier":null,"emote_type":"globals","emote_set_id":"gset","format":["static"],"scale":["1.0"],"theme_mode":["light"]}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var emotes = await endpoint.GetGlobalEmotesAsync(TestContext.Current.CancellationToken);

        Assert.Single(emotes);
        Assert.Equal("g1", emotes[0].Id);
        Assert.Equal("globals", emotes[0].EmoteType);
    }

    // ── GetEmoteSetsAsync ─────────────────────────────────

    [Fact]
    public async Task GetEmoteSetsAsync_ReturnsEmotes()
    {
        var json = """{"data":[{"id":"s1","name":"SetEmote","images":{"url_1x":"https://ex.com/1x","url_2x":"https://ex.com/2x","url_4x":"https://ex.com/4x"},"tier":null,"emote_type":"subscriptions","emote_set_id":"set99","format":["static"],"scale":["1.0"],"theme_mode":["light"]}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var emotes = await endpoint.GetEmoteSetsAsync(["set99"], TestContext.Current.CancellationToken);

        Assert.Single(emotes);
        Assert.Equal("s1", emotes[0].Id);
        Assert.Equal("set99", emotes[0].EmoteSetId);
    }

    // ── GetChannelBadgesAsync ─────────────────────────────

    [Fact]
    public async Task GetChannelBadgesAsync_ReturnsBadges()
    {
        var json = """{"data":[{"set_id":"subscriber","versions":[{"id":"0","image_url_1x":"https://ex.com/1x","image_url_2x":"https://ex.com/2x","image_url_4x":"https://ex.com/4x","title":"Subscriber","description":"Subscriber badge","click_action":"subscribe","click_url":"https://twitch.tv"}]}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var badges = await endpoint.GetChannelBadgesAsync("123", TestContext.Current.CancellationToken);

        Assert.Single(badges);
        Assert.Equal("subscriber", badges[0].SetId);
        Assert.Single(badges[0].Versions);
        Assert.Equal("0", badges[0].Versions[0].Id);
        Assert.Equal("Subscriber", badges[0].Versions[0].Title);
    }

    // ── GetGlobalBadgesAsync ──────────────────────────────

    [Fact]
    public async Task GetGlobalBadgesAsync_ReturnsBadges()
    {
        var json = """{"data":[{"set_id":"premium","versions":[{"id":"1","image_url_1x":"https://ex.com/1x","image_url_2x":"https://ex.com/2x","image_url_4x":"https://ex.com/4x","title":"Premium","description":"Premium badge","click_action":"","click_url":""}]}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var badges = await endpoint.GetGlobalBadgesAsync(TestContext.Current.CancellationToken);

        Assert.Single(badges);
        Assert.Equal("premium", badges[0].SetId);
    }

    // ── GetSettingsAsync ──────────────────────────────────

    [Fact]
    public async Task GetSettingsAsync_ReturnsChatSettings()
    {
        var json = """{"data":[{"broadcaster_id":"123","emote_mode":false,"follower_mode":true,"follower_mode_duration":10,"slow_mode":false,"slow_mode_wait_time":null,"subscriber_mode":false,"unique_chat_mode":false,"non_moderator_chat_delay":null,"non_moderator_chat_delay_duration":null}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var settings = await endpoint.GetSettingsAsync("123", TestContext.Current.CancellationToken);

        Assert.Equal("123", settings.BroadcasterId);
        Assert.False(settings.EmoteMode);
        Assert.True(settings.FollowerMode);
        Assert.Equal(10, settings.FollowerModeDuration);
    }

    // ── UpdateSettingsAsync ───────────────────────────────

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsUpdatedSettings()
    {
        var json = """{"data":[{"broadcaster_id":"123","emote_mode":true,"follower_mode":false,"follower_mode_duration":null,"slow_mode":false,"slow_mode_wait_time":null,"subscriber_mode":false,"unique_chat_mode":false,"non_moderator_chat_delay":null,"non_moderator_chat_delay_duration":null}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var settings = await endpoint.UpdateSettingsAsync(
            "123", "mod456",
            new UpdateChatSettingsRequest(EmoteMode: true),
            TestContext.Current.CancellationToken);

        Assert.True(settings.EmoteMode);
        Assert.False(settings.FollowerMode);
    }

    // ── SendAnnouncementAsync ─────────────────────────────

    [Fact]
    public async Task SendAnnouncementAsync_SendsPostRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var endpoint = CreateEndpoint(handler);

        await endpoint.SendAnnouncementAsync("123", "mod456", "Hello chat!", "blue", TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    // ── SendShoutoutAsync ─────────────────────────────────

    [Fact]
    public async Task SendShoutoutAsync_SendsPostRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var endpoint = CreateEndpoint(handler);

        await endpoint.SendShoutoutAsync("fromBroadcaster", "toBroadcaster", "mod456", TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Contains("from_broadcaster_id=fromBroadcaster", handler.LastRequest?.RequestUri?.Query);
    }

    // ── SendMessageAsync ──────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_SendsPostRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var endpoint = CreateEndpoint(handler);

        await endpoint.SendMessageAsync("123", "sender789", "Hello!", TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    // ── GetUserColorAsync ─────────────────────────────────

    [Fact]
    public async Task GetUserColorAsync_ReturnsColors()
    {
        var json = """{"data":[{"user_id":"42","user_login":"tester","user_name":"Tester","color":"#FF0000"}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var colors = await endpoint.GetUserColorAsync(["42"], TestContext.Current.CancellationToken);

        Assert.Single(colors);
        Assert.Equal("42", colors[0].UserId);
        Assert.Equal("#FF0000", colors[0].Color);
    }

    // ── UpdateUserColorAsync ──────────────────────────────

    [Fact]
    public async Task UpdateUserColorAsync_SendsPutRequest()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var endpoint = CreateEndpoint(handler);

        await endpoint.UpdateUserColorAsync("42", "blue", TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
    }

    // ── Error handling ────────────────────────────────────

    [Fact]
    public async Task GetChattersAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetChattersAsync("123", "mod", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    [Fact]
    public async Task SendAnnouncementAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(403, "Forbidden", "Not authorized"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.SendAnnouncementAsync("123", "mod", "msg", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(403, received!.StatusCode);
    }

    [Fact]
    public async Task GetSettingsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(404, "Not Found", "Channel not found"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetSettingsAsync("999", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(404, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private ChatEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        return CreateEndpoint(handler);
    }

    private ChatEndpoint CreateEndpoint(FakeHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ChatEndpoint(httpClient, _errors);
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
