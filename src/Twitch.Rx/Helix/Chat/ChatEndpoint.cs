using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Chat;

// ── Public Interface ──────────────────────────────────────

public interface IChatEndpoint
{
    Task<Page<Chatter>> GetChattersAsync(string broadcasterId, string moderatorId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Chatter> GetAllChattersAsync(string broadcasterId, string moderatorId, CancellationToken ct = default);
    Task<IReadOnlyList<Emote>> GetChannelEmotesAsync(string broadcasterId, CancellationToken ct = default);
    Task<IReadOnlyList<Emote>> GetGlobalEmotesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Emote>> GetEmoteSetsAsync(IEnumerable<string> emoteSetIds, CancellationToken ct = default);
    Task<IReadOnlyList<Badge>> GetChannelBadgesAsync(string broadcasterId, CancellationToken ct = default);
    Task<IReadOnlyList<Badge>> GetGlobalBadgesAsync(CancellationToken ct = default);
    Task<ChatSettings> GetSettingsAsync(string broadcasterId, CancellationToken ct = default);
    Task<ChatSettings> UpdateSettingsAsync(string broadcasterId, string moderatorId, UpdateChatSettingsRequest request, CancellationToken ct = default);
    Task SendAnnouncementAsync(string broadcasterId, string moderatorId, string message, string? color = null, CancellationToken ct = default);
    Task SendShoutoutAsync(string fromBroadcasterId, string toBroadcasterId, string moderatorId, CancellationToken ct = default);
    Task SendMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default);
    Task<IReadOnlyList<UserChatColor>> GetUserColorAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    Task UpdateUserColorAsync(string userId, string color, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Chatter(string UserId, string UserLogin, string UserName);

public sealed record Emote(
    string Id,
    string Name,
    EmoteImages Images,
    string? Tier,
    string EmoteType,
    string EmoteSetId,
    IReadOnlyList<string> Format,
    IReadOnlyList<string> Scale,
    IReadOnlyList<string> ThemeMode);

public sealed record EmoteImages(string Url1X, string Url2X, string Url4X);

public sealed record Badge(string SetId, IReadOnlyList<BadgeVersion> Versions);

public sealed record BadgeVersion(
    string Id,
    string ImageUrl1X,
    string ImageUrl2X,
    string ImageUrl4X,
    string Title,
    string Description,
    string ClickAction,
    string ClickUrl);

public sealed record ChatSettings(
    string BroadcasterId,
    bool EmoteMode,
    bool FollowerMode,
    int? FollowerModeDuration,
    bool SlowMode,
    int? SlowModeWaitTime,
    bool SubscriberMode,
    bool UniqueChatMode,
    string? NonModeratorChatDelay,
    bool? NonModeratorChatDelayDuration);

public sealed record UpdateChatSettingsRequest(
    bool? EmoteMode = null,
    bool? FollowerMode = null,
    int? FollowerModeDuration = null,
    bool? SlowMode = null,
    int? SlowModeWaitTime = null,
    bool? SubscriberMode = null,
    bool? UniqueChatMode = null,
    bool? NonModeratorChatDelay = null,
    int? NonModeratorChatDelayDuration = null);

public sealed record UserChatColor(string UserId, string UserLogin, string UserName, string Color);

// ── Implementation ────────────────────────────────────────

internal sealed class ChatEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IChatEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Chatter>> GetChattersAsync(string broadcasterId, string moderatorId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/chat/chatters?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            cursor, Ctx.HelixPaginatedResponseChatterDto, ct, ChatMappings.ToModel);

    public IAsyncEnumerable<Chatter> GetAllChattersAsync(string broadcasterId, string moderatorId, CancellationToken ct = default)
        => GetAllPagesAsync(
            $"/helix/chat/chatters?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            Ctx.HelixPaginatedResponseChatterDto, ct, ChatMappings.ToModel);

    public async Task<IReadOnlyList<Emote>> GetChannelEmotesAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync($"/helix/chat/emotes?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseEmoteDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<Emote>> GetGlobalEmotesAsync(CancellationToken ct = default)
    {
        var dtos = await GetListAsync("/helix/chat/emotes/global", Ctx.HelixResponseEmoteDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<Emote>> GetEmoteSetsAsync(IEnumerable<string> emoteSetIds, CancellationToken ct = default)
    {
        var query = string.Join("&", emoteSetIds.Select(id => $"emote_set_id={Uri.EscapeDataString(id)}"));
        var dtos = await GetListAsync($"/helix/chat/emotes/set?{query}", Ctx.HelixResponseEmoteDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<Badge>> GetChannelBadgesAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync($"/helix/chat/badges?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseBadgeDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<Badge>> GetGlobalBadgesAsync(CancellationToken ct = default)
    {
        var dtos = await GetListAsync("/helix/chat/badges/global", Ctx.HelixResponseBadgeDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<ChatSettings> GetSettingsAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync($"/helix/chat/settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseChatSettingsDto, ct);
        return dto?.ToModel() ?? throw new InvalidOperationException("Chat settings not found.");
    }

    public async Task<ChatSettings> UpdateSettingsAsync(string broadcasterId, string moderatorId, UpdateChatSettingsRequest request, CancellationToken ct = default)
    {
        var dto = await PatchAsync(
            $"/helix/chat/settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            request.ToDto(),
            Ctx.UpdateChatSettingsDto,
            Ctx.HelixResponseChatSettingsDto,
            ct);
        return dto.ToModel();
    }

    public async Task SendAnnouncementAsync(string broadcasterId, string moderatorId, string message, string? color = null, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/chat/announcements?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new SendAnnouncementDto(message, color ?? "primary"),
            Ctx.SendAnnouncementDto,
            ct);

    public async Task SendShoutoutAsync(string fromBroadcasterId, string toBroadcasterId, string moderatorId, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/chat/shoutouts?from_broadcaster_id={Uri.EscapeDataString(fromBroadcasterId)}&to_broadcaster_id={Uri.EscapeDataString(toBroadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new EmptyRequestDto(),
            Ctx.EmptyRequestDto,
            ct);

    public async Task SendMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default)
        => await PostAsync(
            "/helix/chat/messages",
            new SendChatMessageDto(broadcasterId, senderId, message),
            Ctx.SendChatMessageDto,
            ct);

    public async Task<IReadOnlyList<UserChatColor>> GetUserColorAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var query = string.Join("&", userIds.Select(id => $"user_id={Uri.EscapeDataString(id)}"));
        var dtos = await GetListAsync($"/helix/chat/color?{query}", Ctx.HelixResponseUserChatColorDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task UpdateUserColorAsync(string userId, string color, CancellationToken ct = default)
        => await PutAsync($"/helix/chat/color?user_id={Uri.EscapeDataString(userId)}&color={Uri.EscapeDataString(color)}", ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ChatterDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName);

internal sealed record EmoteDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("images")] EmoteImagesDto Images,
    [property: JsonPropertyName("tier")] string? Tier,
    [property: JsonPropertyName("emote_type")] string EmoteType,
    [property: JsonPropertyName("emote_set_id")] string EmoteSetId,
    [property: JsonPropertyName("format")] string[] Format,
    [property: JsonPropertyName("scale")] string[] Scale,
    [property: JsonPropertyName("theme_mode")] string[] ThemeMode);

internal sealed record EmoteImagesDto(
    [property: JsonPropertyName("url_1x")] string Url1X,
    [property: JsonPropertyName("url_2x")] string Url2X,
    [property: JsonPropertyName("url_4x")] string Url4X);

internal sealed record BadgeDto(
    [property: JsonPropertyName("set_id")] string SetId,
    [property: JsonPropertyName("versions")] BadgeVersionDto[] Versions);

internal sealed record BadgeVersionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("image_url_1x")] string ImageUrl1X,
    [property: JsonPropertyName("image_url_2x")] string ImageUrl2X,
    [property: JsonPropertyName("image_url_4x")] string ImageUrl4X,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("click_action")] string ClickAction,
    [property: JsonPropertyName("click_url")] string ClickUrl);

internal sealed record ChatSettingsDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("emote_mode")] bool EmoteMode,
    [property: JsonPropertyName("follower_mode")] bool FollowerMode,
    [property: JsonPropertyName("follower_mode_duration")] int? FollowerModeDuration,
    [property: JsonPropertyName("slow_mode")] bool SlowMode,
    [property: JsonPropertyName("slow_mode_wait_time")] int? SlowModeWaitTime,
    [property: JsonPropertyName("subscriber_mode")] bool SubscriberMode,
    [property: JsonPropertyName("unique_chat_mode")] bool UniqueChatMode,
    [property: JsonPropertyName("non_moderator_chat_delay")] string? NonModeratorChatDelay,
    [property: JsonPropertyName("non_moderator_chat_delay_duration")] bool? NonModeratorChatDelayDuration);

internal sealed record UpdateChatSettingsDto(
    [property: JsonPropertyName("emote_mode")] bool? EmoteMode,
    [property: JsonPropertyName("follower_mode")] bool? FollowerMode,
    [property: JsonPropertyName("follower_mode_duration")] int? FollowerModeDuration,
    [property: JsonPropertyName("slow_mode")] bool? SlowMode,
    [property: JsonPropertyName("slow_mode_wait_time")] int? SlowModeWaitTime,
    [property: JsonPropertyName("subscriber_mode")] bool? SubscriberMode,
    [property: JsonPropertyName("unique_chat_mode")] bool? UniqueChatMode,
    [property: JsonPropertyName("non_moderator_chat_delay")] bool? NonModeratorChatDelay,
    [property: JsonPropertyName("non_moderator_chat_delay_duration")] int? NonModeratorChatDelayDuration);

internal sealed record SendAnnouncementDto(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("color")] string Color);

internal sealed record EmptyRequestDto();

internal sealed record SendChatMessageDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("sender_id")] string SenderId,
    [property: JsonPropertyName("message")] string Message);

internal sealed record UserChatColorDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("color")] string Color);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ChatMappings
{
    public static Chatter ToModel(ChatterDto dto) => new(dto.UserId, dto.UserLogin, dto.UserName);

    public static Emote ToModel(this EmoteDto dto) => new(
        dto.Id, dto.Name, dto.Images.ToModel(),
        dto.Tier, dto.EmoteType, dto.EmoteSetId,
        dto.Format, dto.Scale, dto.ThemeMode);

    public static EmoteImages ToModel(this EmoteImagesDto dto) => new(dto.Url1X, dto.Url2X, dto.Url4X);

    public static Badge ToModel(this BadgeDto dto) => new(
        dto.SetId, dto.Versions.Select(v => v.ToModel()).ToArray());

    public static BadgeVersion ToModel(this BadgeVersionDto dto) => new(
        dto.Id, dto.ImageUrl1X, dto.ImageUrl2X, dto.ImageUrl4X,
        dto.Title, dto.Description, dto.ClickAction, dto.ClickUrl);

    public static ChatSettings ToModel(this ChatSettingsDto dto) => new(
        dto.BroadcasterId, dto.EmoteMode, dto.FollowerMode, dto.FollowerModeDuration,
        dto.SlowMode, dto.SlowModeWaitTime, dto.SubscriberMode, dto.UniqueChatMode,
        dto.NonModeratorChatDelay, dto.NonModeratorChatDelayDuration);

    public static UpdateChatSettingsDto ToDto(this UpdateChatSettingsRequest req) => new(
        req.EmoteMode, req.FollowerMode, req.FollowerModeDuration,
        req.SlowMode, req.SlowModeWaitTime, req.SubscriberMode, req.UniqueChatMode,
        req.NonModeratorChatDelay, req.NonModeratorChatDelayDuration);

    public static UserChatColor ToModel(this UserChatColorDto dto) => new(
        dto.UserId, dto.UserLogin, dto.UserName, dto.Color);
}
