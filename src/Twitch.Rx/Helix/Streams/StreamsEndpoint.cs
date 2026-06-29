using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Streams;

// ── Public Interface ──────────────────────────────────────

public interface IStreamsEndpoint
{
    Task<Page<TwitchStream>> GetStreamsAsync(GetStreamsRequest? request = null, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<TwitchStream> GetAllStreamsAsync(GetStreamsRequest? request = null, CancellationToken ct = default);
    Task<Page<TwitchStream>> GetFollowedStreamsAsync(string userId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<TwitchStream> GetAllFollowedStreamsAsync(string userId, CancellationToken ct = default);
    Task<StreamMarker> CreateMarkerAsync(string userId, string? description = null, CancellationToken ct = default);
    Task<Page<StreamMarkerGroup>> GetMarkersAsync(string userId, string? videoId = null, string? cursor = null, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record TwitchStream(
    string Id,
    string UserId,
    string UserLogin,
    string UserName,
    string GameId,
    string GameName,
    string Type,
    string Title,
    int ViewerCount,
    string StartedAt,
    string Language,
    string ThumbnailUrl,
    bool IsMature);

public sealed record GetStreamsRequest(
    IEnumerable<string>? GameIds = null,
    IEnumerable<string>? UserIds = null,
    IEnumerable<string>? UserLogins = null,
    string? Language = null);

public sealed record StreamMarker(
    string Id,
    string CreatedAt,
    string Description,
    int PositionSeconds);

public sealed record StreamMarkerGroup(
    string UserId,
    string UserName,
    string UserLogin,
    IReadOnlyList<StreamMarkerVideo> Videos);

public sealed record StreamMarkerVideo(
    string VideoId,
    IReadOnlyList<StreamMarkerItem> Markers);

public sealed record StreamMarkerItem(
    string Id,
    string CreatedAt,
    string Description,
    int PositionSeconds);

// ── Implementation ────────────────────────────────────────

internal sealed class StreamsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IStreamsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<TwitchStream>> GetStreamsAsync(GetStreamsRequest? request = null, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(BuildStreamsUrl(request), cursor, Ctx.HelixPaginatedResponseStreamDto, ct, StreamMappings.ToModel);

    public IAsyncEnumerable<TwitchStream> GetAllStreamsAsync(GetStreamsRequest? request = null, CancellationToken ct = default)
        => GetAllPagesAsync(BuildStreamsUrl(request), Ctx.HelixPaginatedResponseStreamDto, ct, StreamMappings.ToModel);

    public Task<Page<TwitchStream>> GetFollowedStreamsAsync(string userId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/streams/followed?user_id={Uri.EscapeDataString(userId)}",
            cursor, Ctx.HelixPaginatedResponseStreamDto, ct, StreamMappings.ToModel);

    public IAsyncEnumerable<TwitchStream> GetAllFollowedStreamsAsync(string userId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/streams/followed?user_id={Uri.EscapeDataString(userId)}",
            Ctx.HelixPaginatedResponseStreamDto, ct, StreamMappings.ToModel);

    public async Task<StreamMarker> CreateMarkerAsync(string userId, string? description = null, CancellationToken ct = default)
    {
        var dto = await PostAsync("/helix/streams/markers",
            new CreateMarkerDto(userId, description),
            Ctx.CreateMarkerDto,
            Ctx.HelixResponseStreamMarkerDto,
            ct);
        return dto.ToModel();
    }

    public Task<Page<StreamMarkerGroup>> GetMarkersAsync(string userId, string? videoId = null, string? cursor = null, CancellationToken ct = default)
    {
        var url = videoId is null
            ? $"/helix/streams/markers?user_id={Uri.EscapeDataString(userId)}"
            : $"/helix/streams/markers?user_id={Uri.EscapeDataString(userId)}&video_id={Uri.EscapeDataString(videoId)}";
        return GetPageAsync(url, cursor, Ctx.HelixPaginatedResponseStreamMarkerGroupDto, ct, StreamMappings.ToModel);
    }

    private static string BuildStreamsUrl(GetStreamsRequest? request)
    {
        if (request is null) return "/helix/streams";

        var parts = new List<string>();
        if (request.GameIds is not null)
            parts.AddRange(request.GameIds.Select(id => $"game_id={Uri.EscapeDataString(id)}"));
        if (request.UserIds is not null)
            parts.AddRange(request.UserIds.Select(id => $"user_id={Uri.EscapeDataString(id)}"));
        if (request.UserLogins is not null)
            parts.AddRange(request.UserLogins.Select(l => $"user_login={Uri.EscapeDataString(l)}"));
        if (request.Language is not null)
            parts.Add($"language={Uri.EscapeDataString(request.Language)}");

        return parts.Count > 0 ? $"/helix/streams?{string.Join("&", parts)}" : "/helix/streams";
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record StreamDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("game_name")] string GameName,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("viewer_count")] int ViewerCount,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("is_mature")] bool IsMature);

internal sealed record CreateMarkerDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("description")] string? Description);

internal sealed record StreamMarkerDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("position_seconds")] int PositionSeconds);

internal sealed record StreamMarkerGroupDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("videos")] StreamMarkerVideoDto[] Videos);

internal sealed record StreamMarkerVideoDto(
    [property: JsonPropertyName("video_id")] string VideoId,
    [property: JsonPropertyName("markers")] StreamMarkerItemDto[] Markers);

internal sealed record StreamMarkerItemDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("position_seconds")] int PositionSeconds);

// ── Mappings (file-scoped) ────────────────────────────────

static file class StreamMappings
{
    public static TwitchStream ToModel(StreamDto dto) => new(
        dto.Id, dto.UserId, dto.UserLogin, dto.UserName,
        dto.GameId, dto.GameName, dto.Type, dto.Title,
        dto.ViewerCount, dto.StartedAt, dto.Language,
        dto.ThumbnailUrl, dto.IsMature);

    public static StreamMarker ToModel(this StreamMarkerDto dto) => new(
        dto.Id, dto.CreatedAt, dto.Description, dto.PositionSeconds);

    public static StreamMarkerGroup ToModel(StreamMarkerGroupDto dto) => new(
        dto.UserId, dto.UserName, dto.UserLogin,
        dto.Videos.Select(v => v.ToModel()).ToArray());

    public static StreamMarkerVideo ToModel(this StreamMarkerVideoDto dto) => new(
        dto.VideoId, dto.Markers.Select(m => m.ToModel()).ToArray());

    public static StreamMarkerItem ToModel(this StreamMarkerItemDto dto) => new(
        dto.Id, dto.CreatedAt, dto.Description, dto.PositionSeconds);
}
