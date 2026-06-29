using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Search;

// ── Public Interface ──────────────────────────────────────

public interface ISearchEndpoint
{
    Task<Page<Category>> SearchCategoriesAsync(string query, string? cursor = null, CancellationToken ct = default);
    Task<Page<SearchChannel>> SearchChannelsAsync(string query, bool? liveOnly = null, string? cursor = null, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Category(string Id, string Name, string BoxArtUrl);

public sealed record SearchChannel(
    string Id,
    string BroadcasterLogin,
    string DisplayName,
    string GameId,
    string GameName,
    bool IsLive,
    string ThumbnailUrl,
    string Title,
    string StartedAt,
    string BroadcasterLanguage);

// ── Implementation ────────────────────────────────────────

internal sealed class SearchEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), ISearchEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Category>> SearchCategoriesAsync(string query, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/search/categories?query={Uri.EscapeDataString(query)}",
            cursor, Ctx.HelixPaginatedResponseCategoryDto, ct, SearchMappings.ToModel);

    public Task<Page<SearchChannel>> SearchChannelsAsync(string query, bool? liveOnly = null, string? cursor = null, CancellationToken ct = default)
    {
        var url = $"/helix/search/channels?query={Uri.EscapeDataString(query)}";
        if (liveOnly.HasValue) url += $"&live_only={(liveOnly.Value ? "true" : "false")}";
        return GetPageAsync(url, cursor, Ctx.HelixPaginatedResponseSearchChannelDto, ct, SearchMappings.ToModel);
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record CategoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("box_art_url")] string BoxArtUrl);

internal sealed record SearchChannelDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("game_name")] string GameName,
    [property: JsonPropertyName("is_live")] bool IsLive,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("broadcaster_language")] string BroadcasterLanguage);

// ── Mappings (file-scoped) ────────────────────────────────

static file class SearchMappings
{
    public static Category ToModel(this CategoryDto dto) => new(dto.Id, dto.Name, dto.BoxArtUrl);

    public static SearchChannel ToModel(this SearchChannelDto dto) => new(
        dto.Id, dto.BroadcasterLogin, dto.DisplayName,
        dto.GameId, dto.GameName, dto.IsLive,
        dto.ThumbnailUrl, dto.Title, dto.StartedAt, dto.BroadcasterLanguage);
}
