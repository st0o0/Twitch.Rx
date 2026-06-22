using System.Text.Json;
using Twitch.Rx.EventSub.Json;
using Xunit;

namespace Twitch.Rx.Tests.EventSub;

public sealed class EventSubJsonTests
{
    [Fact]
    public void Parses_SessionWelcome()
    {
        const string json = """
            {
                "metadata":{"message_id":"1","message_type":"session_welcome","message_timestamp":"2023-01-01T00:00:00Z"},
                "payload":{"session":{"id":"session-123","status":"connected","keepalive_timeout_seconds":10,"reconnect_url":null,"connected_at":"2023-01-01T00:00:00Z"}}
            }
            """;

        var envelope = JsonSerializer.Deserialize(json, EventSubJsonContext.Default.EventSubEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("session_welcome", envelope!.Metadata.MessageType);
        Assert.Equal("session-123", envelope.Payload.Session!.Id);
        Assert.Equal(10, envelope.Payload.Session.KeepaliveTimeoutSeconds);
    }

    [Fact]
    public void Parses_Notification()
    {
        const string json = """
            {
                "metadata":{"message_id":"1","message_type":"notification","message_timestamp":"2023-01-01T00:00:00Z","subscription_type":"channel.follow","subscription_version":"2"},
                "payload":{"subscription":{"id":"sub-1","type":"channel.follow","version":"2","status":"enabled","cost":1},"event":{"user_id":"111","user_login":"follower","user_name":"Follower","broadcaster_user_id":"222","broadcaster_user_login":"streamer","broadcaster_user_name":"Streamer","followed_at":"2023-01-01T00:00:00Z"}}
            }
            """;

        var envelope = JsonSerializer.Deserialize(json, EventSubJsonContext.Default.EventSubEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("notification", envelope!.Metadata.MessageType);
        Assert.Equal("channel.follow", envelope.Metadata.SubscriptionType);
        Assert.NotNull(envelope.Payload.Event);
    }

    [Fact]
    public void Parses_SessionReconnect()
    {
        const string json = """
            {
                "metadata":{"message_id":"1","message_type":"session_reconnect","message_timestamp":"2023-01-01T00:00:00Z"},
                "payload":{"session":{"id":"new-session","status":"reconnecting","keepalive_timeout_seconds":null,"reconnect_url":"wss://eventsub.wss.twitch.tv/ws?token=abc","connected_at":"2023-01-01T00:00:00Z"}}
            }
            """;

        var envelope = JsonSerializer.Deserialize(json, EventSubJsonContext.Default.EventSubEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("session_reconnect", envelope!.Metadata.MessageType);
        Assert.Equal("wss://eventsub.wss.twitch.tv/ws?token=abc", envelope.Payload.Session!.ReconnectUrl);
    }

    [Fact]
    public void Parses_Keepalive()
    {
        const string json = """
            {"metadata":{"message_id":"1","message_type":"session_keepalive","message_timestamp":"2023-01-01T00:00:00Z"},"payload":{}}
            """;

        var envelope = JsonSerializer.Deserialize(json, EventSubJsonContext.Default.EventSubEnvelope);

        Assert.NotNull(envelope);
        Assert.Equal("session_keepalive", envelope!.Metadata.MessageType);
    }
}
