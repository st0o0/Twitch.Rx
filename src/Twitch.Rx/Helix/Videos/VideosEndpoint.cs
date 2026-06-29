using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Videos;

// ── Public Interface ──────────────────────────────────────

public interface IVideosEndpoint
{
    Task<Video?> GetByIdAsync(string videoId, CancellationToken ct = default);
    Task<IReadOnlyList<Video>> GetByIdsAsync(IEnumerable<string> videoIds, CancellationToken ct = default);
    Task<Page<Video>> GetByUserAsync(string userId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Video> GetAllByUserAsync(string userId, CancellationToken ct = default);
    Task<Page<Video>> GetByGameAsync(string gameId, string? cursor = null, CancellationToken ct = default);
    Task DeleteAsync(IEnumerable<string> videoIds, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Video(
    string Id,
    string StreamId,
    string UserId,
    string UserLogin,
    string UserName,
    string Title,
    string Description,
    string CreatedAt,
    string PublishedAt,
    string Url,
    string ThumbnailUrl,
    string Viewable,
    int ViewCount,
    string Language,
    string Type,
    string Duration);

// ── Implementation ────────────────────────────────────────

internal sealed class VideosEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IVideosEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<Video?> GetByIdAsync(string videoId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/videos?id={Uri.EscapeDataString(videoId)}",
            Ctx.HelixResponseVideoDto, ct))?.ToModel();

    public async Task<IReadOnlyList<Video>> GetByIdsAsync(IEnumerable<string> videoIds, CancellationToken ct = default)
    {
        var query = string.Join("&", videoIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
        var dtos = await GetListAsync($"/helix/videos?{query}", Ctx.HelixResponseVideoDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public Task<Page<Video>> GetByUserAsync(string userId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/videos?user_id={Uri.EscapeDataString(userId)}",
            cursor, Ctx.HelixPaginatedResponseVideoDto, ct, VideoMappings.ToModel);

    public IAsyncEnumerable<Video> GetAllByUserAsync(string userId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/videos?user_id={Uri.EscapeDataString(userId)}",
            Ctx.HelixPaginatedResponseVideoDto, ct, VideoMappings.ToModel);

    public Task<Page<Video>> GetByGameAsync(string gameId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/videos?game_id={Uri.EscapeDataString(gameId)}",
            cursor, Ctx.HelixPaginatedResponseVideoDto, ct, VideoMappings.ToModel);

    public async Task DeleteAsync(IEnumerable<string> videoIds, CancellationToken ct = default)
    {
        var query = string.Join("&", videoIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
        await DeleteAsync($"/helix/videos?{query}", ct);
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record VideoDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("stream_id")] string StreamId,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("published_at")] string PublishedAt,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("viewable")] string Viewable,
    [property: JsonPropertyName("view_count")] int ViewCount,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("duration")] string Duration);

// ── Mappings (file-scoped) ────────────────────────────────

static file class VideoMappings
{
    public static Video ToModel(this VideoDto dto) => new(
        dto.Id, dto.StreamId, dto.UserId, dto.UserLogin, dto.UserName,
        dto.Title, dto.Description, dto.CreatedAt, dto.PublishedAt,
        dto.Url, dto.ThumbnailUrl, dto.Viewable, dto.ViewCount,
        dto.Language, dto.Type, dto.Duration);
}
