using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Extensions;

// ── Public Interface ──────────────────────────────────────

public interface IExtensionsEndpoint
{
    Task<IReadOnlyList<Extension>> GetExtensionsAsync(string extensionId, CancellationToken ct = default);
    Task<ActiveExtensions> GetActiveExtensionsAsync(string? userId = null, CancellationToken ct = default);
    Task<ActiveExtensions> UpdateActiveExtensionsAsync(ActiveExtensions extensions, CancellationToken ct = default);
    Task<IReadOnlyList<ExtensionBitsProduct>> GetBitsProductsAsync(CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Extension(
    string Id,
    string Version,
    string Type,
    bool CanActivate,
    IReadOnlyList<string> SupportedFeatures);

public sealed record ActiveExtension(bool Active, string? Id, string? Version, string? Name, int? X, int? Y);

public sealed record ActiveExtensions(
    IReadOnlyDictionary<string, ActiveExtension> Panel,
    IReadOnlyDictionary<string, ActiveExtension> Overlay,
    IReadOnlyDictionary<string, ActiveExtension> Component);

public sealed record ExtensionBitsProduct(
    string Sku,
    ExtensionCost Cost,
    bool InDevelopment,
    string DisplayName,
    string Expiration,
    bool IsBroadcast);

public sealed record ExtensionCost(int Amount, string Type);

// ── Implementation ────────────────────────────────────────

internal sealed class ExtensionsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IExtensionsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<IReadOnlyList<Extension>> GetExtensionsAsync(string extensionId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync(
            $"/helix/extensions?extension_id={Uri.EscapeDataString(extensionId)}",
            Ctx.HelixResponseExtensionDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<ActiveExtensions> GetActiveExtensionsAsync(string? userId = null, CancellationToken ct = default)
    {
        var url = userId is not null
            ? $"/helix/users/extensions?user_id={Uri.EscapeDataString(userId)}"
            : "/helix/users/extensions";
        var response = await GetResponseAsync(url, Ctx.ActiveExtensionsResponseDto, ct);
        return response.Data.ToModel();
    }

    public async Task<ActiveExtensions> UpdateActiveExtensionsAsync(ActiveExtensions extensions, CancellationToken ct = default)
    {
        var requestDto = new ActiveExtensionsResponseDto(extensions.ToDto());
        var response = await PutResponseAsync("/helix/users/extensions",
            requestDto, Ctx.ActiveExtensionsResponseDto, Ctx.ActiveExtensionsResponseDto, ct);
        return response.Data.ToModel();
    }

    public async Task<IReadOnlyList<ExtensionBitsProduct>> GetBitsProductsAsync(CancellationToken ct = default)
    {
        var dtos = await GetListAsync("/helix/bits/extensions", Ctx.HelixResponseExtensionBitsProductDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ExtensionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("can_activate")] bool CanActivate,
    [property: JsonPropertyName("supported_features")] string[] SupportedFeatures);

internal sealed record ActiveExtensionItemDto(
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("x")] int? X,
    [property: JsonPropertyName("y")] int? Y);

internal sealed record ActiveExtensionsDataDto(
    [property: JsonPropertyName("panel")] Dictionary<string, ActiveExtensionItemDto>? Panel,
    [property: JsonPropertyName("overlay")] Dictionary<string, ActiveExtensionItemDto>? Overlay,
    [property: JsonPropertyName("component")] Dictionary<string, ActiveExtensionItemDto>? Component);

internal sealed record ActiveExtensionsResponseDto(
    [property: JsonPropertyName("data")] ActiveExtensionsDataDto Data);

internal sealed record ExtensionBitsProductDto(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("cost")] ExtensionCostDto Cost,
    [property: JsonPropertyName("in_development")] bool InDevelopment,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("expiration")] string Expiration,
    [property: JsonPropertyName("is_broadcast")] bool IsBroadcast);

internal sealed record ExtensionCostDto(
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("type")] string Type);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ExtensionMappings
{
    public static Extension ToModel(this ExtensionDto dto) => new(
        dto.Id, dto.Version, dto.Type, dto.CanActivate, dto.SupportedFeatures);

    public static ActiveExtensions ToModel(this ActiveExtensionsDataDto dto) => new(
        (dto.Panel ?? []).ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
        (dto.Overlay ?? []).ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
        (dto.Component ?? []).ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()));

    private static ActiveExtension ToModel(this ActiveExtensionItemDto dto) => new(
        dto.Active, dto.Id, dto.Version, dto.Name, dto.X, dto.Y);

    public static ActiveExtensionsDataDto ToDto(this ActiveExtensions ext) => new(
        ext.Panel.ToDictionary(kv => kv.Key, kv => kv.Value.ToDto()),
        ext.Overlay.ToDictionary(kv => kv.Key, kv => kv.Value.ToDto()),
        ext.Component.ToDictionary(kv => kv.Key, kv => kv.Value.ToDto()));

    private static ActiveExtensionItemDto ToDto(this ActiveExtension ext) => new(
        ext.Active, ext.Id, ext.Version, ext.Name, ext.X, ext.Y);

    public static ExtensionBitsProduct ToModel(this ExtensionBitsProductDto dto) => new(
        dto.Sku, dto.Cost.ToModel(), dto.InDevelopment, dto.DisplayName, dto.Expiration, dto.IsBroadcast);

    private static ExtensionCost ToModel(this ExtensionCostDto dto) => new(dto.Amount, dto.Type);
}
