using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Channels;

// ── Public Interface ──────────────────────────────────────

public interface IChannelsEndpoint
{
    Task<ChannelInfo?> GetInfoAsync(string broadcasterId, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelInfo>> GetInfoAsync(IEnumerable<string> broadcasterIds, CancellationToken ct = default);
    Task ModifyAsync(ModifyChannelRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelEditor>> GetEditorsAsync(string broadcasterId, CancellationToken ct = default);
    Task<Page<Follower>> GetFollowersAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Follower> GetAllFollowersAsync(string broadcasterId, CancellationToken ct = default);
    Task<Page<FollowedChannel>> GetFollowedChannelsAsync(string userId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<FollowedChannel> GetAllFollowedChannelsAsync(string userId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record ChannelInfo(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string BroadcasterLanguage,
    string GameId,
    string GameName,
    string Title,
    int Delay,
    IReadOnlyList<string> Tags,
    bool IsBrandedContent);

public sealed record ModifyChannelRequest(
    string? GameId = null,
    string? BroadcasterLanguage = null,
    string? Title = null,
    int? Delay = null,
    IReadOnlyList<string>? Tags = null,
    bool? IsBrandedContent = null);

public sealed record ChannelEditor(
    string UserId,
    string UserName,
    string CreatedAt);

public sealed record Follower(
    string UserId,
    string UserLogin,
    string UserName,
    string FollowedAt);

public sealed record FollowedChannel(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string FollowedAt);

// ── Implementation ────────────────────────────────────────

internal sealed class ChannelsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IChannelsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<ChannelInfo?> GetInfoAsync(string broadcasterId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/channels?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseChannelInfoDto, ct))?.ToModel();

    public async Task<IReadOnlyList<ChannelInfo>> GetInfoAsync(IEnumerable<string> broadcasterIds, CancellationToken ct = default)
    {
        var query = string.Join("&", broadcasterIds.Select(id => $"broadcaster_id={Uri.EscapeDataString(id)}"));
        var dtos = await GetListAsync($"/helix/channels?{query}", Ctx.HelixResponseChannelInfoDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task ModifyAsync(ModifyChannelRequest request, CancellationToken ct = default)
        => await PatchAsync("/helix/channels", request.ToDto(), Ctx.ModifyChannelDto, ct);

    public async Task<IReadOnlyList<ChannelEditor>> GetEditorsAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync($"/helix/channels/editors?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseChannelEditorDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public Task<Page<Follower>> GetFollowersAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/channels/followers?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseFollowerDto, ct, ChannelMappings.ToModel);

    public IAsyncEnumerable<Follower> GetAllFollowersAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/channels/followers?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseFollowerDto, ct, ChannelMappings.ToModel);

    public Task<Page<FollowedChannel>> GetFollowedChannelsAsync(string userId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/channels/followed?user_id={Uri.EscapeDataString(userId)}",
            cursor, Ctx.HelixPaginatedResponseFollowedChannelDto, ct, ChannelMappings.ToModel);

    public IAsyncEnumerable<FollowedChannel> GetAllFollowedChannelsAsync(string userId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/channels/followed?user_id={Uri.EscapeDataString(userId)}",
            Ctx.HelixPaginatedResponseFollowedChannelDto, ct, ChannelMappings.ToModel);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ChannelInfoDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("broadcaster_language")] string BroadcasterLanguage,
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("game_name")] string GameName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("delay")] int Delay,
    [property: JsonPropertyName("tags")] string[] Tags,
    [property: JsonPropertyName("is_branded_content")] bool IsBrandedContent);

internal sealed record ModifyChannelDto(
    [property: JsonPropertyName("game_id")] string? GameId,
    [property: JsonPropertyName("broadcaster_language")] string? BroadcasterLanguage,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("delay")] int? Delay,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("is_branded_content")] bool? IsBrandedContent);

internal sealed record ChannelEditorDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("created_at")] string CreatedAt);

internal sealed record FollowerDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("followed_at")] string FollowedAt);

internal sealed record FollowedChannelDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("followed_at")] string FollowedAt);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ChannelMappings
{
    public static ChannelInfo ToModel(this ChannelInfoDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.BroadcasterLanguage, dto.GameId, dto.GameName,
        dto.Title, dto.Delay, dto.Tags, dto.IsBrandedContent);

    public static ModifyChannelDto ToDto(this ModifyChannelRequest req) => new(
        req.GameId, req.BroadcasterLanguage, req.Title,
        req.Delay, req.Tags, req.IsBrandedContent);

    public static ChannelEditor ToModel(this ChannelEditorDto dto) => new(
        dto.UserId, dto.UserName, dto.CreatedAt);

    public static Follower ToModel(FollowerDto dto) => new(
        dto.UserId, dto.UserLogin, dto.UserName, dto.FollowedAt);

    public static FollowedChannel ToModel(FollowedChannelDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName, dto.FollowedAt);
}
