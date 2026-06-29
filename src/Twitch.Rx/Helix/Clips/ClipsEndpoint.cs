using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Clips;

// ── Public Interface ──────────────────────────────────────

public interface IClipsEndpoint
{
    Task<CreatedClip> CreateAsync(string broadcasterId, CancellationToken ct = default);
    Task<Clip?> GetByIdAsync(string clipId, CancellationToken ct = default);
    Task<Page<Clip>> GetByBroadcasterAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Clip> GetAllByBroadcasterAsync(string broadcasterId, CancellationToken ct = default);
    Task<Page<Clip>> GetByGameAsync(string gameId, string? cursor = null, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record CreatedClip(string Id, string EditUrl);

public sealed record Clip(
    string Id,
    string Url,
    string EmbedUrl,
    string BroadcasterId,
    string BroadcasterName,
    string CreatorId,
    string CreatorName,
    string VideoId,
    string GameId,
    string Language,
    string Title,
    int ViewCount,
    string CreatedAt,
    string ThumbnailUrl,
    float Duration,
    int? VodOffset);

// ── Implementation ────────────────────────────────────────

internal sealed class ClipsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IClipsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<CreatedClip> CreateAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/clips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseCreatedClipDto, ct);
        return dto.ToModel();
    }

    public async Task<Clip?> GetByIdAsync(string clipId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/clips?id={Uri.EscapeDataString(clipId)}",
            Ctx.HelixResponseClipDto, ct))?.ToModel();

    public Task<Page<Clip>> GetByBroadcasterAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/clips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseClipDto, ct, ClipMappings.ToModel);

    public IAsyncEnumerable<Clip> GetAllByBroadcasterAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/clips?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseClipDto, ct, ClipMappings.ToModel);

    public Task<Page<Clip>> GetByGameAsync(string gameId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/clips?game_id={Uri.EscapeDataString(gameId)}",
            cursor, Ctx.HelixPaginatedResponseClipDto, ct, ClipMappings.ToModel);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record CreatedClipDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("edit_url")] string EditUrl);

internal sealed record ClipDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("embed_url")] string EmbedUrl,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("creator_id")] string CreatorId,
    [property: JsonPropertyName("creator_name")] string CreatorName,
    [property: JsonPropertyName("video_id")] string VideoId,
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("view_count")] int ViewCount,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("duration")] float Duration,
    [property: JsonPropertyName("vod_offset")] int? VodOffset);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ClipMappings
{
    public static CreatedClip ToModel(this CreatedClipDto dto) => new(dto.Id, dto.EditUrl);

    public static Clip ToModel(this ClipDto dto) => new(
        dto.Id, dto.Url, dto.EmbedUrl,
        dto.BroadcasterId, dto.BroadcasterName,
        dto.CreatorId, dto.CreatorName,
        dto.VideoId, dto.GameId, dto.Language,
        dto.Title, dto.ViewCount, dto.CreatedAt,
        dto.ThumbnailUrl, dto.Duration, dto.VodOffset);
}
