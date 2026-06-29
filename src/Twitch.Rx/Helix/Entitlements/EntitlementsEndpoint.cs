using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Entitlements;

// ── Public Interface ──────────────────────────────────────

public interface IEntitlementsEndpoint
{
    Task<Page<DropsEntitlement>> GetDropsEntitlementsAsync(string? userId = null, string? gameId = null, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<DropsEntitlement> GetAllDropsEntitlementsAsync(string? userId = null, string? gameId = null, CancellationToken ct = default);
    Task<IReadOnlyList<UpdatedEntitlement>> UpdateDropsEntitlementsAsync(IEnumerable<string> entitlementIds, string fulfillmentStatus, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record DropsEntitlement(
    string Id,
    string BenefitId,
    string Timestamp,
    string UserId,
    string GameId,
    string FulfillmentStatus,
    string UpdatedAt);

public sealed record UpdatedEntitlement(string UserId, string Status, IReadOnlyList<string> Ids);

// ── Implementation ────────────────────────────────────────

internal sealed class EntitlementsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IEntitlementsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<DropsEntitlement>> GetDropsEntitlementsAsync(string? userId = null, string? gameId = null, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync(BuildDropsUrl(userId, gameId), cursor,
            Ctx.HelixPaginatedResponseDropsEntitlementDto, ct, EntitlementMappings.ToModel);

    public IAsyncEnumerable<DropsEntitlement> GetAllDropsEntitlementsAsync(string? userId = null, string? gameId = null, CancellationToken ct = default)
        => GetAllPagesAsync(BuildDropsUrl(userId, gameId),
            Ctx.HelixPaginatedResponseDropsEntitlementDto, ct, EntitlementMappings.ToModel);

    public async Task<IReadOnlyList<UpdatedEntitlement>> UpdateDropsEntitlementsAsync(IEnumerable<string> entitlementIds, string fulfillmentStatus, CancellationToken ct = default)
    {
        var dto = new UpdateDropsEntitlementsDto(entitlementIds.ToArray(), fulfillmentStatus);
        var dtos = await PatchListAsync("/helix/entitlements/drops",
            dto, Ctx.UpdateDropsEntitlementsDto, Ctx.HelixResponseUpdatedEntitlementDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    private static string BuildDropsUrl(string? userId, string? gameId)
    {
        var parts = new List<string>();
        if (userId is not null) parts.Add($"user_id={Uri.EscapeDataString(userId)}");
        if (gameId is not null) parts.Add($"game_id={Uri.EscapeDataString(gameId)}");
        return parts.Count > 0
            ? $"/helix/entitlements/drops?{string.Join("&", parts)}"
            : "/helix/entitlements/drops";
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record DropsEntitlementDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("benefit_id")] string BenefitId,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("game_id")] string GameId,
    [property: JsonPropertyName("fulfillment_status")] string FulfillmentStatus,
    [property: JsonPropertyName("updated_at")] string UpdatedAt);

internal sealed record UpdateDropsEntitlementsDto(
    [property: JsonPropertyName("entitlement_ids")] string[] EntitlementIds,
    [property: JsonPropertyName("fulfillment_status")] string FulfillmentStatus);

internal sealed record UpdatedEntitlementDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("ids")] string[] Ids);

// ── Mappings (file-scoped) ────────────────────────────────

static file class EntitlementMappings
{
    public static DropsEntitlement ToModel(this DropsEntitlementDto dto) => new(
        dto.Id, dto.BenefitId, dto.Timestamp, dto.UserId, dto.GameId, dto.FulfillmentStatus, dto.UpdatedAt);

    public static UpdatedEntitlement ToModel(this UpdatedEntitlementDto dto) => new(
        dto.UserId, dto.Status, dto.Ids);
}
