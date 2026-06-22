using System.Text.Json;
using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Json;

internal sealed record EventSubEnvelope(
    [property: JsonPropertyName("metadata")] EventSubMetadata Metadata,
    [property: JsonPropertyName("payload")] EventSubPayload Payload);

internal sealed record EventSubMetadata(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("message_type")] string MessageType,
    [property: JsonPropertyName("message_timestamp")] string MessageTimestamp,
    [property: JsonPropertyName("subscription_type")] string? SubscriptionType = null,
    [property: JsonPropertyName("subscription_version")] string? SubscriptionVersion = null);

internal sealed record EventSubPayload(
    [property: JsonPropertyName("session")] EventSubSession? Session = null,
    [property: JsonPropertyName("subscription")] EventSubSubscription? Subscription = null,
    [property: JsonPropertyName("event")] JsonElement? Event = null);

internal sealed record EventSubSession(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("keepalive_timeout_seconds")] int? KeepaliveTimeoutSeconds = null,
    [property: JsonPropertyName("reconnect_url")] string? ReconnectUrl = null,
    [property: JsonPropertyName("connected_at")] string? ConnectedAt = null);

internal sealed record EventSubSubscription(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("cost")] int Cost);
