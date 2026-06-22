using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record ChatMessageEvent(
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("chatter_user_id")] string ChatterUserId,
    [property: JsonPropertyName("chatter_user_login")] string ChatterUserLogin,
    [property: JsonPropertyName("chatter_user_name")] string ChatterUserName,
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("message")] ChatMessageContent Message);

public sealed record ChatMessageContent(
    [property: JsonPropertyName("text")] string Text);
