using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Subscriptions;

// ── Public Interface ──────────────────────────────────────

public interface ISubscriptionsEndpoint
{
    Task<Page<Subscription>> GetBroadcasterSubscriptionsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Subscription> GetAllBroadcasterSubscriptionsAsync(string broadcasterId, CancellationToken ct = default);
    Task<UserSubscription?> CheckUserSubscriptionAsync(string broadcasterId, string userId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Subscription(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string GifterId,
    string GifterLogin,
    string GifterName,
    bool IsGift,
    string PlanName,
    string Tier,
    string UserId,
    string UserLogin,
    string UserName);

public sealed record UserSubscription(
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    bool IsGift,
    string PlanName,
    string Tier,
    string UserId,
    string UserLogin,
    string UserName);

// ── Implementation ────────────────────────────────────────

internal sealed class SubscriptionsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), ISubscriptionsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Subscription>> GetBroadcasterSubscriptionsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/subscriptions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseSubscriptionDto, ct, SubscriptionMappings.ToModel);

    public IAsyncEnumerable<Subscription> GetAllBroadcasterSubscriptionsAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/subscriptions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseSubscriptionDto, ct, SubscriptionMappings.ToModel);

    public async Task<UserSubscription?> CheckUserSubscriptionAsync(string broadcasterId, string userId, CancellationToken ct = default)
    {
        var dto = await GetFirstAsync(
            $"/helix/subscriptions/user?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(userId)}",
            Ctx.HelixResponseUserSubscriptionDto, ct);
        return dto?.ToModel();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record SubscriptionDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("gifter_id")] string GifterId,
    [property: JsonPropertyName("gifter_login")] string GifterLogin,
    [property: JsonPropertyName("gifter_name")] string GifterName,
    [property: JsonPropertyName("is_gift")] bool IsGift,
    [property: JsonPropertyName("plan_name")] string PlanName,
    [property: JsonPropertyName("tier")] string Tier,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName);

internal sealed record UserSubscriptionDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("is_gift")] bool IsGift,
    [property: JsonPropertyName("plan_name")] string PlanName,
    [property: JsonPropertyName("tier")] string Tier,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName);

// ── Mappings (file-scoped) ────────────────────────────────

static file class SubscriptionMappings
{
    public static Subscription ToModel(SubscriptionDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.GifterId, dto.GifterLogin, dto.GifterName,
        dto.IsGift, dto.PlanName, dto.Tier,
        dto.UserId, dto.UserLogin, dto.UserName);

    public static UserSubscription ToModel(this UserSubscriptionDto dto) => new(
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.IsGift, dto.PlanName, dto.Tier,
        dto.UserId, dto.UserLogin, dto.UserName);
}
