using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Events;

public sealed record ChannelPointsRedemptionEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("user_input")] string UserInput,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("redeemed_at")] string RedeemedAt);
