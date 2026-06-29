using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.GuestStar;

// ── Public Interface ──────────────────────────────────────

public interface IGuestStarEndpoint
{
    Task<GuestStarSettings> GetSettingsAsync(string broadcasterId, CancellationToken ct = default);
    Task UpdateSettingsAsync(string broadcasterId, UpdateGuestStarSettingsRequest request, CancellationToken ct = default);
    Task<GuestStarSession?> GetSessionAsync(string broadcasterId, string moderatorId, CancellationToken ct = default);
    Task<GuestStarSession> CreateSessionAsync(string broadcasterId, CancellationToken ct = default);
    Task EndSessionAsync(string broadcasterId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<GuestStarInvite>> GetInvitesAsync(string broadcasterId, string moderatorId, string sessionId, CancellationToken ct = default);
    Task SendInviteAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, CancellationToken ct = default);
    Task DeleteInviteAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, CancellationToken ct = default);
    Task AssignSlotAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, string slotId, CancellationToken ct = default);
    Task UpdateSlotAsync(string broadcasterId, string moderatorId, string sessionId, string sourceSlotId, string? destinationSlotId = null, CancellationToken ct = default);
    Task DeleteSlotAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, string slotId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record GuestStarSettings(
    string BroadcasterId,
    bool IsModeratorSendLiveEnabled,
    int SlotCount,
    bool IsBrowserSourceAudioEnabled,
    string GroupLayout,
    string BrowserSourceToken);

public sealed record GuestStarSession(string Id, IReadOnlyList<GuestStarGuest> Guests);

public sealed record GuestStarGuest(
    string SlotId,
    bool IsLive,
    string UserId,
    string UserDisplayName,
    string UserLogin,
    int Volume,
    string AssignedAt,
    GuestStarAudioSettings AudioSettings,
    GuestStarVideoSettings VideoSettings);

public sealed record GuestStarAudioSettings(bool IsHostAudioEnabled, bool IsGuestAudioEnabled);

public sealed record GuestStarVideoSettings(bool IsHostVideoEnabled, bool IsGuestVideoEnabled);

public sealed record GuestStarInvite(
    string UserId,
    string InvitedAt,
    string Status,
    bool IsVideoEnabled,
    bool IsAudioEnabled,
    bool IsVideoAvailable,
    bool IsAudioAvailable);

public sealed record UpdateGuestStarSettingsRequest(
    bool? IsModeratorSendLiveEnabled = null,
    int? SlotCount = null,
    bool? IsBrowserSourceAudioEnabled = null,
    string? GroupLayout = null,
    bool? RegenerateToken = null);

// ── Implementation ────────────────────────────────────────

internal sealed class GuestStarEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IGuestStarEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<GuestStarSettings> GetSettingsAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/guest_star/channel_settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseGuestStarSettingsDto, ct);
        return dto?.ToModel() ?? throw new InvalidOperationException("Guest Star settings not found.");
    }

    public async Task UpdateSettingsAsync(string broadcasterId, UpdateGuestStarSettingsRequest request, CancellationToken ct = default)
        => await PutAsync(
            $"/helix/guest_star/channel_settings?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            request.ToDto(),
            Ctx.UpdateGuestStarSettingsDto,
            ct);

    public async Task<GuestStarSession?> GetSessionAsync(string broadcasterId, string moderatorId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/guest_star/session?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}",
            Ctx.HelixResponseGuestStarSessionDto, ct);
        return dto?.ToModel();
    }

    public async Task<GuestStarSession> CreateSessionAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/guest_star/session?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseGuestStarSessionDto, ct);
        return dto.ToModel();
    }

    public async Task EndSessionAsync(string broadcasterId, string sessionId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/guest_star/session?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&session_id={Uri.EscapeDataString(sessionId)}",
            ct);

    public async Task<IReadOnlyList<GuestStarInvite>> GetInvitesAsync(string broadcasterId, string moderatorId, string sessionId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync(
            $"/helix/guest_star/invites?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}",
            Ctx.HelixResponseGuestStarInviteDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task SendInviteAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/guest_star/invites?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}&guest_id={Uri.EscapeDataString(guestId)}",
            ct);

    public async Task DeleteInviteAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/guest_star/invites?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}&guest_id={Uri.EscapeDataString(guestId)}",
            ct);

    public async Task AssignSlotAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, string slotId, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/guest_star/slot?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}&guest_id={Uri.EscapeDataString(guestId)}&slot_id={Uri.EscapeDataString(slotId)}",
            ct);

    public async Task UpdateSlotAsync(string broadcasterId, string moderatorId, string sessionId, string sourceSlotId, string? destinationSlotId = null, CancellationToken ct = default)
    {
        var url = $"/helix/guest_star/slot?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}&source_slot_id={Uri.EscapeDataString(sourceSlotId)}";
        if (destinationSlotId is not null)
            url += $"&destination_slot_id={Uri.EscapeDataString(destinationSlotId)}";
        await PatchAsync(url, ct);
    }

    public async Task DeleteSlotAsync(string broadcasterId, string moderatorId, string sessionId, string guestId, string slotId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/guest_star/slot?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(moderatorId)}&session_id={Uri.EscapeDataString(sessionId)}&guest_id={Uri.EscapeDataString(guestId)}&slot_id={Uri.EscapeDataString(slotId)}",
            ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record GuestStarSettingsDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("is_moderator_send_live_enabled")] bool IsModeratorSendLiveEnabled,
    [property: JsonPropertyName("slot_count")] int SlotCount,
    [property: JsonPropertyName("is_browser_source_audio_enabled")] bool IsBrowserSourceAudioEnabled,
    [property: JsonPropertyName("group_layout")] string GroupLayout,
    [property: JsonPropertyName("browser_source_token")] string BrowserSourceToken);

internal sealed record UpdateGuestStarSettingsDto(
    [property: JsonPropertyName("is_moderator_send_live_enabled")] bool? IsModeratorSendLiveEnabled,
    [property: JsonPropertyName("slot_count")] int? SlotCount,
    [property: JsonPropertyName("is_browser_source_audio_enabled")] bool? IsBrowserSourceAudioEnabled,
    [property: JsonPropertyName("group_layout")] string? GroupLayout,
    [property: JsonPropertyName("regenerate_token")] bool? RegenerateToken);

internal sealed record GuestStarSessionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("guests")] GuestStarGuestDto[]? Guests);

internal sealed record GuestStarGuestDto(
    [property: JsonPropertyName("slot_id")] string SlotId,
    [property: JsonPropertyName("is_live")] bool IsLive,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_display_name")] string UserDisplayName,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("volume")] int Volume,
    [property: JsonPropertyName("assigned_at")] string AssignedAt,
    [property: JsonPropertyName("audio_settings")] GuestStarAudioSettingsDto AudioSettings,
    [property: JsonPropertyName("video_settings")] GuestStarVideoSettingsDto VideoSettings);

internal sealed record GuestStarAudioSettingsDto(
    [property: JsonPropertyName("is_host_audio_enabled")] bool IsHostAudioEnabled,
    [property: JsonPropertyName("is_guest_audio_enabled")] bool IsGuestAudioEnabled);

internal sealed record GuestStarVideoSettingsDto(
    [property: JsonPropertyName("is_host_video_enabled")] bool IsHostVideoEnabled,
    [property: JsonPropertyName("is_guest_video_enabled")] bool IsGuestVideoEnabled);

internal sealed record GuestStarInviteDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("invited_at")] string InvitedAt,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("is_video_enabled")] bool IsVideoEnabled,
    [property: JsonPropertyName("is_audio_enabled")] bool IsAudioEnabled,
    [property: JsonPropertyName("is_video_available")] bool IsVideoAvailable,
    [property: JsonPropertyName("is_audio_available")] bool IsAudioAvailable);

// ── Mappings (file-scoped) ────────────────────────────────

static file class GuestStarMappings
{
    public static GuestStarSettings ToModel(this GuestStarSettingsDto dto) => new(
        dto.BroadcasterId, dto.IsModeratorSendLiveEnabled, dto.SlotCount,
        dto.IsBrowserSourceAudioEnabled, dto.GroupLayout, dto.BrowserSourceToken);

    public static UpdateGuestStarSettingsDto ToDto(this UpdateGuestStarSettingsRequest req) => new(
        req.IsModeratorSendLiveEnabled, req.SlotCount, req.IsBrowserSourceAudioEnabled,
        req.GroupLayout, req.RegenerateToken);

    public static GuestStarSession ToModel(this GuestStarSessionDto dto) => new(
        dto.Id,
        (dto.Guests ?? []).Select(g => g.ToModel()).ToArray());

    private static GuestStarGuest ToModel(this GuestStarGuestDto dto) => new(
        dto.SlotId, dto.IsLive, dto.UserId, dto.UserDisplayName, dto.UserLogin,
        dto.Volume, dto.AssignedAt,
        dto.AudioSettings.ToModel(), dto.VideoSettings.ToModel());

    private static GuestStarAudioSettings ToModel(this GuestStarAudioSettingsDto dto) => new(
        dto.IsHostAudioEnabled, dto.IsGuestAudioEnabled);

    private static GuestStarVideoSettings ToModel(this GuestStarVideoSettingsDto dto) => new(
        dto.IsHostVideoEnabled, dto.IsGuestVideoEnabled);

    public static GuestStarInvite ToModel(this GuestStarInviteDto dto) => new(
        dto.UserId, dto.InvitedAt, dto.Status,
        dto.IsVideoEnabled, dto.IsAudioEnabled, dto.IsVideoAvailable, dto.IsAudioAvailable);
}
