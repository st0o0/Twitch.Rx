using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Ads;

// ── Public Interface ──────────────────────────────────────

public interface IAdsEndpoint
{
    Task<Commercial> StartCommercialAsync(string broadcasterId, int lengthSeconds, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Commercial(int Length, string Message, int RetryAfter);

// ── Implementation ────────────────────────────────────────

internal sealed class AdsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IAdsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<Commercial> StartCommercialAsync(string broadcasterId, int lengthSeconds, CancellationToken ct = default)
    {
        var dto = await PostAsync("/helix/channels/commercial",
            new StartCommercialDto(broadcasterId, lengthSeconds),
            Ctx.StartCommercialDto,
            Ctx.HelixResponseCommercialDto,
            ct);
        return dto.ToModel();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record StartCommercialDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("length")] int Length);

internal sealed record CommercialDto(
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("retry_after")] int RetryAfter);

// ── Mappings (file-scoped) ────────────────────────────────

static file class AdsMappings
{
    public static Commercial ToModel(this CommercialDto dto) => new(dto.Length, dto.Message, dto.RetryAfter);
}
