using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.HypeTrain;

// ── Public Interface ──────────────────────────────────────

public interface IHypeTrainEndpoint
{
    Task<Page<HypeTrainEvent>> GetEventsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<HypeTrainEvent> GetAllEventsAsync(string broadcasterId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record HypeTrainEvent(
    string Id,
    string BroadcasterId,
    int Level,
    long Total,
    string StartedAt,
    string ExpiresAt);

// ── Implementation ────────────────────────────────────────

internal sealed class HypeTrainEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IHypeTrainEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<HypeTrainEvent>> GetEventsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/hypetrain/events?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseHypeTrainEventDto, ct, HypeTrainMappings.ToModel);

    public IAsyncEnumerable<HypeTrainEvent> GetAllEventsAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/hypetrain/events?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseHypeTrainEventDto, ct, HypeTrainMappings.ToModel);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record HypeTrainEventDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("total")] long Total,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("expires_at")] string ExpiresAt);

// ── Mappings (file-scoped) ────────────────────────────────

static file class HypeTrainMappings
{
    public static HypeTrainEvent ToModel(this HypeTrainEventDto dto) => new(
        dto.Id, dto.BroadcasterId, dto.Level, dto.Total, dto.StartedAt, dto.ExpiresAt);
}
