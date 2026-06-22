namespace Twitch.Rx.EventSub;

public sealed record EventSubSubscriptionConfig(
    string Type,
    string Version,
    Dictionary<string, string> Condition);
