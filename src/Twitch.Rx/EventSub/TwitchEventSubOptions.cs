namespace Twitch.Rx.EventSub;

public sealed class TwitchEventSubOptions
{
    public Uri WebSocketUrl { get; set; } = new("wss://eventsub.wss.twitch.tv/ws");
    public bool Enabled { get; set; }
    public bool AutoReconnect { get; set; } = true;
    public TimeSpan? KeepaliveTimeout { get; set; }
    public List<EventSubSubscriptionConfig> Subscriptions { get; } = [];
}
