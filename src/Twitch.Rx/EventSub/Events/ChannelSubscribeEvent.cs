using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record ChannelSubscribeEvent(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("tier")] string Tier,
    [property: JsonPropertyName("is_gift")] bool IsGift);
