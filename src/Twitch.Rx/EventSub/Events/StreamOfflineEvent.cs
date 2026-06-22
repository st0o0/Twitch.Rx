using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record StreamOfflineEvent(
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName);
