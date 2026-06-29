using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Charity;

// ── Public Interface ──────────────────────────────────────

public interface ICharityEndpoint
{
    Task<CharityCampaign?> GetCampaignAsync(string broadcasterId, CancellationToken ct = default);
    Task<Page<CharityDonation>> GetDonationsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<CharityDonation> GetAllDonationsAsync(string broadcasterId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record CharityAmount(int Value, int DecimalPlaces, string Currency);

public sealed record CharityCampaign(
    string Id,
    string BroadcasterId,
    string BroadcasterName,
    string BroadcasterLogin,
    string CharityName,
    string CharityDescription,
    string CharityLogo,
    string CharityWebsite,
    CharityAmount CurrentAmount,
    CharityAmount TargetAmount);

public sealed record CharityDonation(
    string Id,
    string CampaignId,
    string UserId,
    string UserLogin,
    string UserName,
    CharityAmount Amount);

// ── Implementation ────────────────────────────────────────

internal sealed class CharityEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), ICharityEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<CharityCampaign?> GetCampaignAsync(string broadcasterId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/charity/campaigns?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseCharityCampaignDto, ct))?.ToModel();

    public Task<Page<CharityDonation>> GetDonationsAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/charity/donations?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponseCharityDonationDto, ct, CharityMappings.ToModel);

    public IAsyncEnumerable<CharityDonation> GetAllDonationsAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/charity/donations?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponseCharityDonationDto, ct, CharityMappings.ToModel);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record CharityAmountDto(
    [property: JsonPropertyName("value")] int Value,
    [property: JsonPropertyName("decimal_places")] int DecimalPlaces,
    [property: JsonPropertyName("currency")] string Currency);

internal sealed record CharityCampaignDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("charity_name")] string CharityName,
    [property: JsonPropertyName("charity_description")] string CharityDescription,
    [property: JsonPropertyName("charity_logo")] string CharityLogo,
    [property: JsonPropertyName("charity_website")] string CharityWebsite,
    [property: JsonPropertyName("current_amount")] CharityAmountDto CurrentAmount,
    [property: JsonPropertyName("target_amount")] CharityAmountDto TargetAmount);

internal sealed record CharityDonationDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("campaign_id")] string CampaignId,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("amount")] CharityAmountDto Amount);

// ── Mappings (file-scoped) ────────────────────────────────

static file class CharityMappings
{
    private static CharityAmount ToModel(this CharityAmountDto dto) => new(dto.Value, dto.DecimalPlaces, dto.Currency);

    public static CharityCampaign ToModel(this CharityCampaignDto dto) => new(
        dto.Id, dto.BroadcasterId, dto.BroadcasterName, dto.BroadcasterLogin,
        dto.CharityName, dto.CharityDescription, dto.CharityLogo, dto.CharityWebsite,
        dto.CurrentAmount.ToModel(), dto.TargetAmount.ToModel());

    public static CharityDonation ToModel(this CharityDonationDto dto) => new(
        dto.Id, dto.CampaignId, dto.UserId, dto.UserLogin, dto.UserName,
        dto.Amount.ToModel());
}
