using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Moderation;

// ── Public Interface ──────────────────────────────────────

public interface IModerationEndpoint
{
    Task<BanResponse> BanUserAsync(string broadcasterId, string moderatorId, BanUserRequest request, CancellationToken ct = default);
    Task UnbanUserAsync(string broadcasterId, string moderatorId, string userId, CancellationToken ct = default);
    Task<Page<BannedUser>> GetBannedUsersAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<BannedUser> GetAllBannedUsersAsync(string broadcasterId, CancellationToken ct = default);
    Task<Page<BlockedTerm>> GetBlockedTermsAsync(string broadcasterId, string moderatorId, string? cursor = null, CancellationToken ct = default);
    Task<BlockedTerm> AddBlockedTermAsync(string broadcasterId, string moderatorId, string text, CancellationToken ct = default);
    Task RemoveBlockedTermAsync(string broadcasterId, string moderatorId, string termId, CancellationToken ct = default);
    Task<AutoModSettings> GetAutoModSettingsAsync(string broadcasterId, string moderatorId, CancellationToken ct = default);
    Task<AutoModSettings> UpdateAutoModSettingsAsync(string broadcasterId, string moderatorId, UpdateAutoModSettingsRequest request, CancellationToken ct = default);
    Task<Page<Moderator>> GetModeratorsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Moderator> GetAllModeratorsAsync(string broadcasterId, CancellationToken ct = default);
    Task AddModeratorAsync(string broadcasterId, string userId, CancellationToken ct = default);
    Task RemoveModeratorAsync(string broadcasterId, string userId, CancellationToken ct = default);
    Task<Page<Vip>> GetVIPsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    Task AddVIPAsync(string broadcasterId, string userId, CancellationToken ct = default);
    Task RemoveVIPAsync(string broadcasterId, string userId, CancellationToken ct = default);
    Task<ShieldModeStatus> GetShieldModeStatusAsync(string broadcasterId, string moderatorId, CancellationToken ct = default);
    Task<ShieldModeStatus> UpdateShieldModeStatusAsync(string broadcasterId, string moderatorId, bool isActive, CancellationToken ct = default);
    Task WarnUserAsync(string broadcasterId, string moderatorId, string userId, string reason, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record BanUserRequest(string UserId, int? Duration = null, string? Reason = null);

public sealed record BanResponse(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string ModeratorId,
    string ModeratorLogin,
    string ModeratorName,
    string UserId,
    string UserLogin,
    string UserName,
    string CreatedAt,
    string? EndTime);

public sealed record BannedUser(
    string UserId,
    string UserLogin,
    string UserName,
    string? ExpiresAt,
    string CreatedAt,
    string Reason,
    string ModeratorId,
    string ModeratorLogin,
    string ModeratorName);

public sealed record BlockedTerm(
    string BroadcasterId,
    string ModeratorId,
    string Id,
    string Text,
    string CreatedAt,
    string UpdatedAt,
    string? ExpiresAt);

public sealed record AutoModSettings(
    string BroadcasterId,
    string ModeratorId,
    int? OverallLevel,
    int Disability,
    int Aggression,
    int Sexuality,
    int Misogyny,
    int Bullying,
    int Swearing,
    int RaceEthnicityOrReligion,
    int SexBasedTerms);

public sealed record UpdateAutoModSettingsRequest(
    int? OverallLevel = null,
    int? Disability = null,
    int? Aggression = null,
    int? Sexuality = null,
    int? Misogyny = null,
    int? Bullying = null,
    int? Swearing = null,
    int? RaceEthnicityOrReligion = null,
    int? SexBasedTerms = null);

public sealed record Moderator(string UserId, string UserLogin, string UserName);

public sealed record Vip(string UserId, string UserLogin, string UserName);

public sealed record ShieldModeStatus(
    bool IsActive,
    string ModeratorId,
    string ModeratorLogin,
    string ModeratorName,
    string LastActivatedAt);

// ── Implementation ────────────────────────────────────────

internal sealed class ModerationEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IModerationEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<BanResponse> BanUserAsync(string broadcasterId, string moderatorId, BanUserRequest request, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/moderation/bans?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new BanUserRequestDto(new BanUserDataDto(request.UserId, request.Duration, request.Reason)),
            Ctx.BanUserRequestDto,
            Ctx.HelixResponseBanResponseDto,
            ct);
        return dto.ToModel();
    }

    public async Task UnbanUserAsync(string broadcasterId, string moderatorId, string userId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/moderation/bans?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&user_id={Uri.EscapeDataString(userId)}",
            ct);

    public Task<Page<BannedUser>> GetBannedUsersAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/moderation/banned?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseBannedUserDto, ct, ModerationMappings.ToModel);

    public IAsyncEnumerable<BannedUser> GetAllBannedUsersAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync(
            $"/helix/moderation/banned?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseBannedUserDto, ct, ModerationMappings.ToModel);

    public Task<Page<BlockedTerm>> GetBlockedTermsAsync(string broadcasterId, string moderatorId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/moderation/blocked_terms?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            cursor, Ctx.HelixPaginatedResponseBlockedTermDto, ct, ModerationMappings.ToModel);

    public async Task<BlockedTerm> AddBlockedTermAsync(string broadcasterId, string moderatorId, string text, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/moderation/blocked_terms?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new AddBlockedTermDto(text),
            Ctx.AddBlockedTermDto,
            Ctx.HelixResponseBlockedTermDto,
            ct);
        return dto.ToModel();
    }

    public async Task RemoveBlockedTermAsync(string broadcasterId, string moderatorId, string termId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/moderation/blocked_terms?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&id={Uri.EscapeDataString(termId)}",
            ct);

    public async Task<AutoModSettings> GetAutoModSettingsAsync(string broadcasterId, string moderatorId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/moderation/automod/settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            Ctx.HelixResponseAutoModSettingsDto, ct);
        return dto?.ToModel() ?? throw new InvalidOperationException("AutoMod settings not found.");
    }

    public async Task<AutoModSettings> UpdateAutoModSettingsAsync(string broadcasterId, string moderatorId, UpdateAutoModSettingsRequest request, CancellationToken ct = default)
    {
        var dto = await PutAsync(
            $"/helix/moderation/automod/settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            request.ToDto(),
            Ctx.UpdateAutoModSettingsDto,
            Ctx.HelixResponseAutoModSettingsDto,
            ct);
        return dto.ToModel();
    }

    public Task<Page<Moderator>> GetModeratorsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/moderation/moderators?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseModeratorDto, ct, ModerationMappings.ToModel);

    public IAsyncEnumerable<Moderator> GetAllModeratorsAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync(
            $"/helix/moderation/moderators?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseModeratorDto, ct, ModerationMappings.ToModel);

    public async Task AddModeratorAsync(string broadcasterId, string userId, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/moderation/moderators?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(userId)}",
            new EmptyModRequestDto(),
            Ctx.EmptyModRequestDto,
            ct);

    public async Task RemoveModeratorAsync(string broadcasterId, string userId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/moderation/moderators?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(userId)}",
            ct);

    public Task<Page<Vip>> GetVIPsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/channels/vips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseVipDto, ct, ModerationMappings.ToModel);

    public async Task AddVIPAsync(string broadcasterId, string userId, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/channels/vips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(userId)}",
            new EmptyModRequestDto(),
            Ctx.EmptyModRequestDto,
            ct);

    public async Task RemoveVIPAsync(string broadcasterId, string userId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/channels/vips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(userId)}",
            ct);

    public async Task<ShieldModeStatus> GetShieldModeStatusAsync(string broadcasterId, string moderatorId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/moderation/shield_mode?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            Ctx.HelixResponseShieldModeStatusDto, ct);
        return dto?.ToModel() ?? throw new InvalidOperationException("Shield mode status not found.");
    }

    public async Task<ShieldModeStatus> UpdateShieldModeStatusAsync(string broadcasterId, string moderatorId, bool isActive, CancellationToken ct = default)
    {
        var dto = await PutAsync(
            $"/helix/moderation/shield_mode?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new UpdateShieldModeDto(isActive),
            Ctx.UpdateShieldModeDto,
            Ctx.HelixResponseShieldModeStatusDto,
            ct);
        return dto.ToModel();
    }

    public async Task WarnUserAsync(string broadcasterId, string moderatorId, string userId, string reason, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/moderation/warnings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            new WarnUserRequestDto(new WarnUserDataDto(userId, reason)),
            Ctx.WarnUserRequestDto,
            ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record BanUserDataDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("duration")] int? Duration,
    [property: JsonPropertyName("reason")] string? Reason);

internal sealed record BanUserRequestDto(
    [property: JsonPropertyName("data")] BanUserDataDto Data);

internal sealed record BanResponseDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("moderator_id")] string ModeratorId,
    [property: JsonPropertyName("moderator_login")] string ModeratorLogin,
    [property: JsonPropertyName("moderator_name")] string ModeratorName,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("end_time")] string? EndTime);

internal sealed record BannedUserDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("expires_at")] string? ExpiresAt,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("moderator_id")] string ModeratorId,
    [property: JsonPropertyName("moderator_login")] string ModeratorLogin,
    [property: JsonPropertyName("moderator_name")] string ModeratorName);

internal sealed record BlockedTermDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("moderator_id")] string ModeratorId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("updated_at")] string UpdatedAt,
    [property: JsonPropertyName("expires_at")] string? ExpiresAt);

internal sealed record AddBlockedTermDto(
    [property: JsonPropertyName("text")] string Text);

internal sealed record AutoModSettingsDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("moderator_id")] string ModeratorId,
    [property: JsonPropertyName("overall_level")] int? OverallLevel,
    [property: JsonPropertyName("disability")] int Disability,
    [property: JsonPropertyName("aggression")] int Aggression,
    [property: JsonPropertyName("sexuality")] int Sexuality,
    [property: JsonPropertyName("misogyny")] int Misogyny,
    [property: JsonPropertyName("bullying")] int Bullying,
    [property: JsonPropertyName("swearing")] int Swearing,
    [property: JsonPropertyName("race_ethnicity_or_religion")] int RaceEthnicityOrReligion,
    [property: JsonPropertyName("sex_based_terms")] int SexBasedTerms);

internal sealed record UpdateAutoModSettingsDto(
    [property: JsonPropertyName("overall_level")] int? OverallLevel,
    [property: JsonPropertyName("disability")] int? Disability,
    [property: JsonPropertyName("aggression")] int? Aggression,
    [property: JsonPropertyName("sexuality")] int? Sexuality,
    [property: JsonPropertyName("misogyny")] int? Misogyny,
    [property: JsonPropertyName("bullying")] int? Bullying,
    [property: JsonPropertyName("swearing")] int? Swearing,
    [property: JsonPropertyName("race_ethnicity_or_religion")] int? RaceEthnicityOrReligion,
    [property: JsonPropertyName("sex_based_terms")] int? SexBasedTerms);

internal sealed record ModeratorDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName);

internal sealed record VipDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName);

internal sealed record ShieldModeStatusDto(
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("moderator_id")] string ModeratorId,
    [property: JsonPropertyName("moderator_login")] string ModeratorLogin,
    [property: JsonPropertyName("moderator_name")] string ModeratorName,
    [property: JsonPropertyName("last_activated_at")] string LastActivatedAt);

internal sealed record UpdateShieldModeDto(
    [property: JsonPropertyName("is_active")] bool IsActive);

internal sealed record WarnUserDataDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record WarnUserRequestDto(
    [property: JsonPropertyName("data")] WarnUserDataDto Data);

internal sealed record EmptyModRequestDto();

// ── Mappings (file-scoped) ────────────────────────────────

static file class ModerationMappings
{
    public static BanResponse ToModel(this BanResponseDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.ModeratorId, dto.ModeratorLogin, dto.ModeratorName,
        dto.UserId, dto.UserLogin, dto.UserName,
        dto.CreatedAt, dto.EndTime);

    public static BannedUser ToModel(this BannedUserDto dto) => new(
        dto.UserId, dto.UserLogin, dto.UserName,
        dto.ExpiresAt, dto.CreatedAt, dto.Reason,
        dto.ModeratorId, dto.ModeratorLogin, dto.ModeratorName);

    public static BlockedTerm ToModel(this BlockedTermDto dto) => new(
        dto.BroadcasterId, dto.ModeratorId, dto.Id,
        dto.Text, dto.CreatedAt, dto.UpdatedAt, dto.ExpiresAt);

    public static AutoModSettings ToModel(this AutoModSettingsDto dto) => new(
        dto.BroadcasterId, dto.ModeratorId, dto.OverallLevel,
        dto.Disability, dto.Aggression, dto.Sexuality,
        dto.Misogyny, dto.Bullying, dto.Swearing,
        dto.RaceEthnicityOrReligion, dto.SexBasedTerms);

    public static UpdateAutoModSettingsDto ToDto(this UpdateAutoModSettingsRequest req) => new(
        req.OverallLevel, req.Disability, req.Aggression, req.Sexuality,
        req.Misogyny, req.Bullying, req.Swearing,
        req.RaceEthnicityOrReligion, req.SexBasedTerms);

    public static Moderator ToModel(this ModeratorDto dto) => new(dto.UserId, dto.UserLogin, dto.UserName);

    public static Vip ToModel(this VipDto dto) => new(dto.UserId, dto.UserLogin, dto.UserName);

    public static ShieldModeStatus ToModel(this ShieldModeStatusDto dto) => new(
        dto.IsActive, dto.ModeratorId, dto.ModeratorLogin, dto.ModeratorName, dto.LastActivatedAt);
}
