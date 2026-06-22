using System.Text.Json;

namespace Twitch.Rx.EventSub.Events;

public sealed record RawEventSubNotification(
    string SubscriptionType,
    string SubscriptionVersion,
    JsonElement Event);
