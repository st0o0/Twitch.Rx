using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record ChannelRaidEvent(
    [property: JsonPropertyName("from_broadcaster_user_id")] string FromBroadcasterUserId,
    [property: JsonPropertyName("from_broadcaster_user_login")] string FromBroadcasterUserLogin,
    [property: JsonPropertyName("from_broadcaster_user_name")] string FromBroadcasterUserName,
    [property: JsonPropertyName("to_broadcaster_user_id")] string ToBroadcasterUserId,
    [property: JsonPropertyName("to_broadcaster_user_login")] string ToBroadcasterUserLogin,
    [property: JsonPropertyName("to_broadcaster_user_name")] string ToBroadcasterUserName,
    [property: JsonPropertyName("viewers")] int Viewers);
