using System.Text.Json.Serialization;
using Twitch.Rx.Helix.Bits;
using Twitch.Rx.Helix.ChannelPoints;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Clips;
using Twitch.Rx.Helix.Games;
using Twitch.Rx.Helix.Moderation;
using Twitch.Rx.Helix.Polls;
using Twitch.Rx.Helix.Predictions;
using Twitch.Rx.Helix.Streams;
using Twitch.Rx.Helix.Subscriptions;
using Twitch.Rx.Helix.Users;
using Twitch.Rx.Helix.Videos;

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
// Streams
[JsonSerializable(typeof(HelixPaginatedResponse<StreamDto>))]
[JsonSerializable(typeof(CreateMarkerDto))]
[JsonSerializable(typeof(HelixResponse<StreamMarkerDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<StreamMarkerGroupDto>))]
// Subscriptions
[JsonSerializable(typeof(HelixPaginatedResponse<SubscriptionDto>))]
[JsonSerializable(typeof(HelixResponse<UserSubscriptionDto>))]
// Games
[JsonSerializable(typeof(HelixPaginatedResponse<GameDto>))]
[JsonSerializable(typeof(HelixResponse<GameDto>))]
// Videos
[JsonSerializable(typeof(HelixPaginatedResponse<VideoDto>))]
[JsonSerializable(typeof(HelixResponse<VideoDto>))]
// Polls
[JsonSerializable(typeof(HelixPaginatedResponse<PollDto>))]
[JsonSerializable(typeof(HelixResponse<PollDto>))]
[JsonSerializable(typeof(CreatePollDto))]
[JsonSerializable(typeof(EndPollDto))]
// Predictions
[JsonSerializable(typeof(HelixPaginatedResponse<PredictionDto>))]
[JsonSerializable(typeof(HelixResponse<PredictionDto>))]
[JsonSerializable(typeof(CreatePredictionDto))]
[JsonSerializable(typeof(EndPredictionDto))]
// Bits
[JsonSerializable(typeof(BitsLeaderboardResponseDto))]
[JsonSerializable(typeof(HelixResponse<CheermoteDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<ExtensionTransactionDto>))]
// Clips
[JsonSerializable(typeof(HelixResponse<CreatedClipDto>))]
[JsonSerializable(typeof(HelixResponse<ClipDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<ClipDto>))]
// ChannelPoints
[JsonSerializable(typeof(HelixResponse<CustomRewardDto>))]
[JsonSerializable(typeof(CreateCustomRewardDto))]
[JsonSerializable(typeof(UpdateCustomRewardDto))]
[JsonSerializable(typeof(HelixPaginatedResponse<RedemptionDto>))]
[JsonSerializable(typeof(UpdateRedemptionStatusDto))]
// Moderation
[JsonSerializable(typeof(BanUserRequestDto))]
[JsonSerializable(typeof(HelixResponse<BanResponseDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<BannedUserDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<BlockedTermDto>))]
[JsonSerializable(typeof(AddBlockedTermDto))]
[JsonSerializable(typeof(HelixResponse<BlockedTermDto>))]
[JsonSerializable(typeof(HelixResponse<AutoModSettingsDto>))]
[JsonSerializable(typeof(UpdateAutoModSettingsDto))]
[JsonSerializable(typeof(HelixPaginatedResponse<ModeratorDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<VipDto>))]
[JsonSerializable(typeof(EmptyModRequestDto))]
[JsonSerializable(typeof(HelixResponse<ShieldModeStatusDto>))]
[JsonSerializable(typeof(UpdateShieldModeDto))]
[JsonSerializable(typeof(WarnUserRequestDto))]
internal partial class HelixJsonContext : JsonSerializerContext;
