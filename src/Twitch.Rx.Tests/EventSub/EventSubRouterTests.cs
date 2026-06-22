using System.Text.Json;
using R3;
using Twitch.Rx.EventSub;
using Twitch.Rx.EventSub.Events;
using Twitch.Rx.EventSub.Json;
using Xunit;

namespace Twitch.Rx.Tests.EventSub;

public sealed class EventSubRouterTests : IDisposable
{
    private readonly Subject<EventSubEnvelope> _messages = new();
    private readonly EventSubRouter _router;
    private readonly List<IDisposable> _subs = [];

    public EventSubRouterTests()
    {
        _router = new EventSubRouter(_messages);
    }

    [Fact]
    public void ChannelFollow_RoutesCorrectly()
    {
        ChannelFollowEvent? received = null;
        _subs.Add(_router.ChannelFollow.Subscribe(e => received = e));

        EmitNotification("channel.follow", """{"user_id":"1","user_login":"follower","user_name":"Follower","broadcaster_user_id":"2","broadcaster_user_login":"streamer","broadcaster_user_name":"Streamer","followed_at":"2023-01-01T00:00:00Z"}""");

        Assert.NotNull(received);
        Assert.Equal("follower", received!.UserLogin);
    }

    [Fact]
    public void ChatMessage_RoutesCorrectly()
    {
        ChatMessageEvent? received = null;
        _subs.Add(_router.ChatMessage.Subscribe(e => received = e));

        EmitNotification("channel.chat.message", """{"broadcaster_user_id":"1","broadcaster_user_login":"s","broadcaster_user_name":"S","chatter_user_id":"2","chatter_user_login":"chatter","chatter_user_name":"Chatter","message_id":"m1","message":{"text":"Hello!"}}""");

        Assert.NotNull(received);
        Assert.Equal("Hello!", received!.Message.Text);
    }

    [Fact]
    public void UnknownType_GoesToRawNotifications()
    {
        RawEventSubNotification? received = null;
        _subs.Add(_router.RawNotifications.Subscribe(e => received = e));

        EmitNotification("channel.some_new_event", """{"some_field":"value"}""");

        Assert.NotNull(received);
        Assert.Equal("channel.some_new_event", received!.SubscriptionType);
    }

    [Fact]
    public void Keepalive_DoesNotRouteToEvents()
    {
        ChannelFollowEvent? received = null;
        _subs.Add(_router.ChannelFollow.Subscribe(e => received = e));

        _messages.OnNext(new EventSubEnvelope(
            new EventSubMetadata("1", "session_keepalive", "2023-01-01T00:00:00Z"),
            new EventSubPayload()));

        Assert.Null(received);
    }

    [Fact]
    public void Shared_MultipleSubscribers_GetSameEvents()
    {
        var results1 = new List<ChannelFollowEvent>();
        var results2 = new List<ChannelFollowEvent>();
        _subs.Add(_router.ChannelFollow.Subscribe(e => results1.Add(e)));
        _subs.Add(_router.ChannelFollow.Subscribe(e => results2.Add(e)));

        EmitNotification("channel.follow", """{"user_id":"1","user_login":"f","user_name":"F","broadcaster_user_id":"2","broadcaster_user_login":"s","broadcaster_user_name":"S","followed_at":"2023-01-01T00:00:00Z"}""");

        Assert.Single(results1);
        Assert.Single(results2);
    }

    private void EmitNotification(string type, string eventJson)
    {
        _messages.OnNext(new EventSubEnvelope(
            new EventSubMetadata("1", "notification", "2023-01-01T00:00:00Z", type, "1"),
            new EventSubPayload(
                Subscription: new EventSubSubscription("sub-1", type, "1", "enabled", 1),
                Event: JsonSerializer.Deserialize<JsonElement>(eventJson))));
    }

    public void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _router.Dispose();
        _messages.Dispose();
    }
}
