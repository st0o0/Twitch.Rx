using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record StreamOnlineEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("started_at")] string StartedAt);
