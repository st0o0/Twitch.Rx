using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Json;

internal sealed record CreateSubscriptionRequest(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("condition")] Dictionary<string, string> Condition,
    [property: JsonPropertyName("transport")] SubscriptionTransport Transport);

internal sealed record SubscriptionTransport(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("session_id")] string SessionId);
