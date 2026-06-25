using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record PollEndEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] IReadOnlyList<PollChoice> Choices,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("ended_at")] string EndedAt);
