using System.Net.Http.Json;
using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Bits;

// ── Public Interface ──────────────────────────────────────

public interface IBitsEndpoint
{
    Task<BitsLeaderboard> GetLeaderboardAsync(GetBitsLeaderboardRequest? request = null, CancellationToken ct = default);
    Task<IReadOnlyList<Cheermote>> GetCheermotesAsync(string? broadcasterId = null, CancellationToken ct = default);
    Task<Page<ExtensionTransaction>> GetExtensionTransactionsAsync(string extensionId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<ExtensionTransaction> GetAllExtensionTransactionsAsync(string extensionId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record BitsLeaderboard(
    int Total,
    DateRange DateRange,
    IReadOnlyList<BitsLeaderboardEntry> Entries);

public sealed record DateRange(string StartedAt, string EndedAt);

public sealed record BitsLeaderboardEntry(
    string UserId,
    string UserLogin,
    string UserName,
    int Rank,
    int Score);

public sealed record GetBitsLeaderboardRequest(
    int? Count = null,
    string? Period = null,
    string? StartedAt = null,
    string? UserId = null);

public sealed record Cheermote(
    string Prefix,
    IReadOnlyList<CheermoteTier> Tiers,
    string Type,
    int Order,
    string LastUpdated,
    bool IsCharitable);

public sealed record CheermoteTier(
    int MinBits,
    string Id,
    string Color,
    bool CanCheer,
    bool ShowInBitsCard);

public sealed record ExtensionTransaction(
    string Id,
    string Timestamp,
    string BroadcasterUserId,
    string BroadcasterUserLogin,
    string BroadcasterUserName,
    string UserId,
    string UserLogin,
    string UserName,
    string ProductType,
    ExtensionProductData ProductData);

public sealed record ExtensionProductData(
    string Domain,
    string Sku,
    ExtensionProductCost Cost,
    bool InDevelopment,
    string DisplayName);

public sealed record ExtensionProductCost(int Amount, string Type);

// ── Implementation ────────────────────────────────────────

internal sealed class BitsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IBitsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<BitsLeaderboard> GetLeaderboardAsync(GetBitsLeaderboardRequest? request = null, CancellationToken ct = default)
    {
        var dto = await GetResponseAsync(BuildLeaderboardUrl(request), Ctx.BitsLeaderboardResponseDto, ct);
        return dto.ToModel();
    }

    public async Task<IReadOnlyList<Cheermote>> GetCheermotesAsync(string? broadcasterId = null, CancellationToken ct = default)
    {
        var url = broadcasterId is null
            ? "/helix/bits/cheermotes"
            : $"/helix/bits/cheermotes?broadcaster_id={Uri.EscapeDataString(broadcasterId)}";
        var dtos = await GetListAsync(url, Ctx.HelixResponseCheermoteDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public Task<Page<ExtensionTransaction>> GetExtensionTransactionsAsync(string extensionId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/extensions/transactions?extension_id={Uri.EscapeDataString(extensionId)}",
            cursor, Ctx.HelixPaginatedResponseExtensionTransactionDto, ct, BitsMappings.ToModel);

    public IAsyncEnumerable<ExtensionTransaction> GetAllExtensionTransactionsAsync(string extensionId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/extensions/transactions?extension_id={Uri.EscapeDataString(extensionId)}",
            Ctx.HelixPaginatedResponseExtensionTransactionDto, ct, BitsMappings.ToModel);

    private static string BuildLeaderboardUrl(GetBitsLeaderboardRequest? request)
    {
        if (request is null) return "/helix/bits/leaderboard";

        var parts = new List<string>();
        if (request.Count is not null) parts.Add($"count={request.Count}");
        if (request.Period is not null) parts.Add($"period={Uri.EscapeDataString(request.Period)}");
        if (request.StartedAt is not null) parts.Add($"started_at={Uri.EscapeDataString(request.StartedAt)}");
        if (request.UserId is not null) parts.Add($"user_id={Uri.EscapeDataString(request.UserId)}");

        return parts.Count > 0
            ? $"/helix/bits/leaderboard?{string.Join("&", parts)}"
            : "/helix/bits/leaderboard";
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record BitsLeaderboardResponseDto(
    [property: JsonPropertyName("data")] BitsLeaderboardEntryDto[] Data,
    [property: JsonPropertyName("date_range")] DateRangeDto DateRange,
    [property: JsonPropertyName("total")] int Total);

internal sealed record DateRangeDto(
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("ended_at")] string EndedAt);

internal sealed record BitsLeaderboardEntryDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("rank")] int Rank,
    [property: JsonPropertyName("score")] int Score);

internal sealed record CheermoteDto(
    [property: JsonPropertyName("prefix")] string Prefix,
    [property: JsonPropertyName("tiers")] CheermoteTierDto[] Tiers,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("last_updated")] string LastUpdated,
    [property: JsonPropertyName("is_charitable")] bool IsCharitable);

internal sealed record CheermoteTierDto(
    [property: JsonPropertyName("min_bits")] int MinBits,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("can_cheer")] bool CanCheer,
    [property: JsonPropertyName("show_in_bits_card")] bool ShowInBitsCard);

internal sealed record ExtensionTransactionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("broadcaster_user_id")] string BroadcasterUserId,
    [property: JsonPropertyName("broadcaster_user_login")] string BroadcasterUserLogin,
    [property: JsonPropertyName("broadcaster_user_name")] string BroadcasterUserName,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("product_type")] string ProductType,
    [property: JsonPropertyName("product_data")] ExtensionProductDataDto ProductData);

internal sealed record ExtensionProductDataDto(
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("cost")] ExtensionProductCostDto Cost,
    [property: JsonPropertyName("inDevelopment")] bool InDevelopment,
    [property: JsonPropertyName("displayName")] string DisplayName);

internal sealed record ExtensionProductCostDto(
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("type")] string Type);

// ── Mappings (file-scoped) ────────────────────────────────

static file class BitsMappings
{
    public static BitsLeaderboard ToModel(this BitsLeaderboardResponseDto dto) => new(
        dto.Total,
        dto.DateRange.ToModel(),
        dto.Data.Select(e => e.ToModel()).ToArray());

    private static DateRange ToModel(this DateRangeDto dto) => new(dto.StartedAt, dto.EndedAt);

    private static BitsLeaderboardEntry ToModel(this BitsLeaderboardEntryDto dto) => new(
        dto.UserId, dto.UserLogin, dto.UserName, dto.Rank, dto.Score);

    public static Cheermote ToModel(this CheermoteDto dto) => new(
        dto.Prefix,
        dto.Tiers.Select(t => t.ToModel()).ToArray(),
        dto.Type, dto.Order, dto.LastUpdated, dto.IsCharitable);

    private static CheermoteTier ToModel(this CheermoteTierDto dto) => new(
        dto.MinBits, dto.Id, dto.Color, dto.CanCheer, dto.ShowInBitsCard);

    public static ExtensionTransaction ToModel(ExtensionTransactionDto dto) => new(
        dto.Id, dto.Timestamp,
        dto.BroadcasterUserId, dto.BroadcasterUserLogin, dto.BroadcasterUserName,
        dto.UserId, dto.UserLogin, dto.UserName,
        dto.ProductType, dto.ProductData.ToModel());

    private static ExtensionProductData ToModel(this ExtensionProductDataDto dto) => new(
        dto.Domain, dto.Sku, dto.Cost.ToModel(), dto.InDevelopment, dto.DisplayName);

    private static ExtensionProductCost ToModel(this ExtensionProductCostDto dto) => new(dto.Amount, dto.Type);
}
