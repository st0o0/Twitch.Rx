using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.ChannelPoints;

// ── Public Interface ──────────────────────────────────────

public interface IChannelPointsEndpoint
{
    Task<IReadOnlyList<CustomReward>> GetCustomRewardsAsync(string broadcasterId, CancellationToken ct = default);
    Task<CustomReward?> GetCustomRewardAsync(string broadcasterId, string rewardId, CancellationToken ct = default);
    Task<CustomReward> CreateCustomRewardAsync(CreateCustomRewardRequest request, CancellationToken ct = default);
    Task<CustomReward> UpdateCustomRewardAsync(string broadcasterId, string rewardId, UpdateCustomRewardRequest request, CancellationToken ct = default);
    Task DeleteCustomRewardAsync(string broadcasterId, string rewardId, CancellationToken ct = default);
    Task<Page<Redemption>> GetRedemptionsAsync(string broadcasterId, string rewardId, string status, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Redemption> GetAllRedemptionsAsync(string broadcasterId, string rewardId, string status, CancellationToken ct = default);
    Task UpdateRedemptionStatusAsync(string broadcasterId, string rewardId, IEnumerable<string> redemptionIds, string status, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record CustomRewardMaxPerStreamSetting(bool IsEnabled, int MaxPerStream);
public sealed record CustomRewardMaxPerUserPerStreamSetting(bool IsEnabled, int MaxPerUserPerStream);
public sealed record CustomRewardGlobalCooldownSetting(bool IsEnabled, int GlobalCooldownSeconds);

public sealed record CustomReward(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string Id,
    string Title,
    string Prompt,
    int Cost,
    string BackgroundColor,
    bool IsEnabled,
    bool IsUserInputRequired,
    CustomRewardMaxPerStreamSetting MaxPerStreamSetting,
    CustomRewardMaxPerUserPerStreamSetting MaxPerUserPerStreamSetting,
    CustomRewardGlobalCooldownSetting GlobalCooldownSetting,
    bool IsPaused,
    bool IsInStock,
    bool ShouldRedemptionsSkipRequestQueue,
    int? RedemptionsRedeemedCurrentStream,
    string? CooldownExpiresAt);

public sealed record CreateCustomRewardRequest(
    string BroadcasterId,
    string Title,
    int Cost,
    string? Prompt = null,
    bool? IsEnabled = null,
    string? BackgroundColor = null,
    bool? IsUserInputRequired = null,
    bool? IsMaxPerStreamEnabled = null,
    int? MaxPerStream = null,
    bool? IsMaxPerUserPerStreamEnabled = null,
    int? MaxPerUserPerStream = null,
    bool? IsGlobalCooldownEnabled = null,
    int? GlobalCooldownSeconds = null,
    bool? ShouldRedemptionsSkipRequestQueue = null);

public sealed record UpdateCustomRewardRequest(
    string? Title = null,
    string? Prompt = null,
    int? Cost = null,
    string? BackgroundColor = null,
    bool? IsEnabled = null,
    bool? IsUserInputRequired = null,
    bool? IsMaxPerStreamEnabled = null,
    int? MaxPerStream = null,
    bool? IsMaxPerUserPerStreamEnabled = null,
    int? MaxPerUserPerStream = null,
    bool? IsGlobalCooldownEnabled = null,
    int? GlobalCooldownSeconds = null,
    bool? IsPaused = null,
    bool? ShouldRedemptionsSkipRequestQueue = null);

public sealed record RedemptionReward(string Id, string Title, string Prompt, int Cost);

public sealed record Redemption(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string Id,
    string UserId,
    string UserLogin,
    string UserName,
    string UserInput,
    string Status,
    string RedeemedAt,
    RedemptionReward Reward);

// ── Implementation ────────────────────────────────────────

internal sealed class ChannelPointsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IChannelPointsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<IReadOnlyList<CustomReward>> GetCustomRewardsAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync(
            $"/helix/channel_points/custom_rewards?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseCustomRewardDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<CustomReward?> GetCustomRewardAsync(string broadcasterId, string rewardId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/channel_points/custom_rewards?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&id={Uri.EscapeDataString(rewardId)}",
            Ctx.HelixResponseCustomRewardDto, ct);
        return dto?.ToModel();
    }

    public async Task<CustomReward> CreateCustomRewardAsync(CreateCustomRewardRequest request, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/channel_points/custom_rewards?broadcaster_id={Uri.EscapeDataString(request.BroadcasterId)}",
            request.ToDto(),
            Ctx.CreateCustomRewardDto,
            Ctx.HelixResponseCustomRewardDto,
            ct);
        return dto.ToModel();
    }

    public async Task<CustomReward> UpdateCustomRewardAsync(string broadcasterId, string rewardId, UpdateCustomRewardRequest request, CancellationToken ct = default)
    {
        var dto = await PatchAsync(
            $"/helix/channel_points/custom_rewards?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&id={Uri.EscapeDataString(rewardId)}",
            request.ToDto(),
            Ctx.UpdateCustomRewardDto,
            Ctx.HelixResponseCustomRewardDto,
            ct);
        return dto.ToModel();
    }

    public async Task DeleteCustomRewardAsync(string broadcasterId, string rewardId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/channel_points/custom_rewards?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&id={Uri.EscapeDataString(rewardId)}",
            ct);

    public Task<Page<Redemption>> GetRedemptionsAsync(string broadcasterId, string rewardId, string status, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(
            $"/helix/channel_points/custom_rewards/redemptions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&reward_id={Uri.EscapeDataString(rewardId)}&status={Uri.EscapeDataString(status)}",
            cursor, Ctx.HelixPaginatedResponseRedemptionDto, ct, ChannelPointsMappings.ToModel);

    public IAsyncEnumerable<Redemption> GetAllRedemptionsAsync(string broadcasterId, string rewardId, string status, CancellationToken ct = default)
        => GetAllPagesAsync(
            $"/helix/channel_points/custom_rewards/redemptions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&reward_id={Uri.EscapeDataString(rewardId)}&status={Uri.EscapeDataString(status)}",
            Ctx.HelixPaginatedResponseRedemptionDto, ct, ChannelPointsMappings.ToModel);

    public async Task UpdateRedemptionStatusAsync(string broadcasterId, string rewardId, IEnumerable<string> redemptionIds, string status, CancellationToken ct = default)
    {
        var idParams = string.Join("&", redemptionIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
        await PatchAsync(
            $"/helix/channel_points/custom_rewards/redemptions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&reward_id={Uri.EscapeDataString(rewardId)}&{idParams}",
            new UpdateRedemptionStatusDto(status),
            Ctx.UpdateRedemptionStatusDto,
            ct);
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record CustomRewardMaxPerStreamSettingDto(
    [property: JsonPropertyName("is_enabled")] bool IsEnabled,
    [property: JsonPropertyName("max_per_stream")] int MaxPerStream);

internal sealed record CustomRewardMaxPerUserPerStreamSettingDto(
    [property: JsonPropertyName("is_enabled")] bool IsEnabled,
    [property: JsonPropertyName("max_per_user_per_stream")] int MaxPerUserPerStream);

internal sealed record CustomRewardGlobalCooldownSettingDto(
    [property: JsonPropertyName("is_enabled")] bool IsEnabled,
    [property: JsonPropertyName("global_cooldown_seconds")] int GlobalCooldownSeconds);

internal sealed record CustomRewardDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("cost")] int Cost,
    [property: JsonPropertyName("background_color")] string BackgroundColor,
    [property: JsonPropertyName("is_enabled")] bool IsEnabled,
    [property: JsonPropertyName("is_user_input_required")] bool IsUserInputRequired,
    [property: JsonPropertyName("max_per_stream_setting")] CustomRewardMaxPerStreamSettingDto MaxPerStreamSetting,
    [property: JsonPropertyName("max_per_user_per_stream_setting")] CustomRewardMaxPerUserPerStreamSettingDto MaxPerUserPerStreamSetting,
    [property: JsonPropertyName("global_cooldown_setting")] CustomRewardGlobalCooldownSettingDto GlobalCooldownSetting,
    [property: JsonPropertyName("is_paused")] bool IsPaused,
    [property: JsonPropertyName("is_in_stock")] bool IsInStock,
    [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool ShouldRedemptionsSkipRequestQueue,
    [property: JsonPropertyName("redemptions_redeemed_current_stream")] int? RedemptionsRedeemedCurrentStream,
    [property: JsonPropertyName("cooldown_expires_at")] string? CooldownExpiresAt);

internal sealed record CreateCustomRewardDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("cost")] int Cost,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("is_enabled")] bool? IsEnabled,
    [property: JsonPropertyName("background_color")] string? BackgroundColor,
    [property: JsonPropertyName("is_user_input_required")] bool? IsUserInputRequired,
    [property: JsonPropertyName("is_max_per_stream_enabled")] bool? IsMaxPerStreamEnabled,
    [property: JsonPropertyName("max_per_stream")] int? MaxPerStream,
    [property: JsonPropertyName("is_max_per_user_per_stream_enabled")] bool? IsMaxPerUserPerStreamEnabled,
    [property: JsonPropertyName("max_per_user_per_stream")] int? MaxPerUserPerStream,
    [property: JsonPropertyName("is_global_cooldown_enabled")] bool? IsGlobalCooldownEnabled,
    [property: JsonPropertyName("global_cooldown_seconds")] int? GlobalCooldownSeconds,
    [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool? ShouldRedemptionsSkipRequestQueue);

internal sealed record UpdateCustomRewardDto(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("cost")] int? Cost,
    [property: JsonPropertyName("background_color")] string? BackgroundColor,
    [property: JsonPropertyName("is_enabled")] bool? IsEnabled,
    [property: JsonPropertyName("is_user_input_required")] bool? IsUserInputRequired,
    [property: JsonPropertyName("is_max_per_stream_enabled")] bool? IsMaxPerStreamEnabled,
    [property: JsonPropertyName("max_per_stream")] int? MaxPerStream,
    [property: JsonPropertyName("is_max_per_user_per_stream_enabled")] bool? IsMaxPerUserPerStreamEnabled,
    [property: JsonPropertyName("max_per_user_per_stream")] int? MaxPerUserPerStream,
    [property: JsonPropertyName("is_global_cooldown_enabled")] bool? IsGlobalCooldownEnabled,
    [property: JsonPropertyName("global_cooldown_seconds")] int? GlobalCooldownSeconds,
    [property: JsonPropertyName("is_paused")] bool? IsPaused,
    [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool? ShouldRedemptionsSkipRequestQueue);

internal sealed record RedemptionRewardDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("cost")] int Cost);

internal sealed record RedemptionDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("user_input")] string UserInput,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("redeemed_at")] string RedeemedAt,
    [property: JsonPropertyName("reward")] RedemptionRewardDto Reward);

internal sealed record UpdateRedemptionStatusDto(
    [property: JsonPropertyName("status")] string Status);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ChannelPointsMappings
{
    public static CustomReward ToModel(this CustomRewardDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.Id, dto.Title, dto.Prompt, dto.Cost, dto.BackgroundColor,
        dto.IsEnabled, dto.IsUserInputRequired,
        new CustomRewardMaxPerStreamSetting(dto.MaxPerStreamSetting.IsEnabled, dto.MaxPerStreamSetting.MaxPerStream),
        new CustomRewardMaxPerUserPerStreamSetting(dto.MaxPerUserPerStreamSetting.IsEnabled, dto.MaxPerUserPerStreamSetting.MaxPerUserPerStream),
        new CustomRewardGlobalCooldownSetting(dto.GlobalCooldownSetting.IsEnabled, dto.GlobalCooldownSetting.GlobalCooldownSeconds),
        dto.IsPaused, dto.IsInStock, dto.ShouldRedemptionsSkipRequestQueue,
        dto.RedemptionsRedeemedCurrentStream, dto.CooldownExpiresAt);

    public static Redemption ToModel(this RedemptionDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.Id, dto.UserId, dto.UserLogin, dto.UserName,
        dto.UserInput, dto.Status, dto.RedeemedAt,
        new RedemptionReward(dto.Reward.Id, dto.Reward.Title, dto.Reward.Prompt, dto.Reward.Cost));

    public static CreateCustomRewardDto ToDto(this CreateCustomRewardRequest req) => new(
        req.Title, req.Cost, req.Prompt, req.IsEnabled, req.BackgroundColor,
        req.IsUserInputRequired, req.IsMaxPerStreamEnabled, req.MaxPerStream,
        req.IsMaxPerUserPerStreamEnabled, req.MaxPerUserPerStream,
        req.IsGlobalCooldownEnabled, req.GlobalCooldownSeconds,
        req.ShouldRedemptionsSkipRequestQueue);

    public static UpdateCustomRewardDto ToDto(this UpdateCustomRewardRequest req) => new(
        req.Title, req.Prompt, req.Cost, req.BackgroundColor,
        req.IsEnabled, req.IsUserInputRequired,
        req.IsMaxPerStreamEnabled, req.MaxPerStream,
        req.IsMaxPerUserPerStreamEnabled, req.MaxPerUserPerStream,
        req.IsGlobalCooldownEnabled, req.GlobalCooldownSeconds,
        req.IsPaused, req.ShouldRedemptionsSkipRequestQueue);
}
