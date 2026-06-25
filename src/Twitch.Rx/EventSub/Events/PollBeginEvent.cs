using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record PollBeginEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] IReadOnlyList<PollChoice> Choices,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("ends_at")] string EndsAt);

public sealed record PollChoice(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("votes")] int Votes,
    [property: JsonPropertyName("channel_points_votes")] int ChannelPointsVotes,
    [property: JsonPropertyName("bits_votes")] int BitsVotes);
