using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Raids;

// ── Public Interface ──────────────────────────────────────

public interface IRaidsEndpoint
{
    Task<RaidInfo> StartAsync(string fromBroadcasterId, string toBroadcasterId, CancellationToken ct = default);
    Task CancelAsync(string broadcasterId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record RaidInfo(string CreatedAt, bool IsMature);

// ── Implementation ────────────────────────────────────────

internal sealed class RaidsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IRaidsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<RaidInfo> StartAsync(string fromBroadcasterId, string toBroadcasterId, CancellationToken ct = default)
    {
        var dto = await PostAsync(
            $"/helix/raids?from_broadcaster_id={Uri.EscapeDataString(fromBroadcasterId)}&to_broadcaster_id={Uri.EscapeDataString(toBroadcasterId)}",
            Ctx.HelixResponseRaidInfoDto,
            ct);
        return dto.ToModel();
    }

    public async Task CancelAsync(string broadcasterId, CancellationToken ct = default)
        => await DeleteAsync($"/helix/raids?broadcaster_id={Uri.EscapeDataString(broadcasterId)}", ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record RaidInfoDto(
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("is_mature")] bool IsMature);

// ── Mappings (file-scoped) ────────────────────────────────

static file class RaidsMappings
{
    public static RaidInfo ToModel(this RaidInfoDto dto) => new(dto.CreatedAt, dto.IsMature);
}
