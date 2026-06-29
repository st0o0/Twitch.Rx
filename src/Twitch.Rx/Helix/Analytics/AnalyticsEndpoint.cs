using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Analytics;

// ── Public Interface ──────────────────────────────────────

public interface IAnalyticsEndpoint
{
    Task<Page<ExtensionAnalytics>> GetExtensionAnalyticsAsync(string? extensionId = null, string? cursor = null, CancellationToken ct = default);
    Task<Page<GameAnalytics>> GetGameAnalyticsAsync(string? gameId = null, string? cursor = null, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record AnalyticsDateRange(string StartedAt, string EndedAt);

public sealed record ExtensionAnalytics(string ExtensionId, string Url, string Type, AnalyticsDateRange DateRange);

public sealed record GameAnalytics(string GameId, string Url, string Type, AnalyticsDateRange DateRange);

// ── Implementation ────────────────────────────────────────

internal sealed class AnalyticsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IAnalyticsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<ExtensionAnalytics>> GetExtensionAnalyticsAsync(string? extensionId = null, string? cursor = null, CancellationToken ct = default)
    {
        var url = extensionId is null
            ? "/helix/analytics/extensions"
            : $"/helix/analytics/extensions?extension_id={Uri.EscapeDataString(extensionId)}";
        return GetPageAsync(url, cursor, Ctx.HelixPaginatedResponseExtensionAnalyticsDto, ct, AnalyticsMappings.ToModel);
    }

    public Task<Page<GameAnalytics>> GetGameAnalyticsAsync(string? gameId = null, string? cursor = null, CancellationToken ct = default)
    {
        var url = gameId is null
            ? "/helix/analytics/games"
            : $"/helix/analytics/games?game_id={Uri.EscapeDataString(gameId)}";
        return GetPageAsync(url, cursor, Ctx.HelixPaginatedResponseGameAnalyticsDto, ct, AnalyticsMappings.ToModel);
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record AnalyticsDateRangeDto(
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("ended_at")] string EndedAt);

internal sealed record ExtensionAnalyticsDto(
    [property: JsonPropertyName("extension_id")] string ExtensionId,
    [property: JsonPropertyName("URL")] string Url,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("date_range")] AnalyticsDateRangeDto DateRange);

internal sealed record GameAnalyticsDto(
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("URL")] string Url,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("date_range")] AnalyticsDateRangeDto DateRange);

// ── Mappings (file-scoped) ────────────────────────────────

static file class AnalyticsMappings
{
    public static ExtensionAnalytics ToModel(this ExtensionAnalyticsDto dto) => new(
        dto.ExtensionId, dto.Url, dto.Type,
        new AnalyticsDateRange(dto.DateRange.StartedAt, dto.DateRange.EndedAt));

    public static GameAnalytics ToModel(this GameAnalyticsDto dto) => new(
        dto.GameId, dto.Url, dto.Type,
        new AnalyticsDateRange(dto.DateRange.StartedAt, dto.DateRange.EndedAt));
}
