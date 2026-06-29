using System.Text.Json.Serialization;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

internal sealed record HelixErrorDto(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string Message);

[JsonSerializable(typeof(HelixErrorDto))]
// Users
[JsonSerializable(typeof(HelixResponse<UserDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<UserDto>))]
[JsonSerializable(typeof(UpdateUserDto))]
// Channels
[JsonSerializable(typeof(HelixResponse<ChannelInfoDto>))]
[JsonSerializable(typeof(ModifyChannelDto))]
[JsonSerializable(typeof(HelixResponse<ChannelEditorDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<FollowerDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<FollowedChannelDto>))]
// Chat
[JsonSerializable(typeof(HelixPaginatedResponse<ChatterDto>))]
[JsonSerializable(typeof(HelixResponse<EmoteDto>))]
[JsonSerializable(typeof(HelixResponse<BadgeDto>))]
[JsonSerializable(typeof(HelixResponse<ChatSettingsDto>))]
[JsonSerializable(typeof(UpdateChatSettingsDto))]
[JsonSerializable(typeof(SendAnnouncementDto))]
[JsonSerializable(typeof(EmptyRequestDto))]
[JsonSerializable(typeof(SendChatMessageDto))]
[JsonSerializable(typeof(HelixResponse<UserChatColorDto>))]
internal partial class HelixJsonContext : JsonSerializerContext;
