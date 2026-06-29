using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Charity;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Charity;

public sealed class CharityEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetCampaignAsync ───────────────────────────────────

    [Fact]
    public async Task GetCampaignAsync_ReturnsCampaign()
    {
        var endpoint = CreateEndpoint(CampaignListResponse("camp-1", "123"));

        var campaign = await endpoint.GetCampaignAsync("123", TestContext.Current.CancellationToken);

        Assert.NotNull(campaign);
        Assert.Equal("camp-1", campaign!.Id);
        Assert.Equal("123", campaign.BroadcasterId);
    }

    // ── GetDonationsAsync ──────────────────────────────────

    [Fact]
    public async Task GetDonationsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(DonationsPageResponse("don-1", "camp-1", null));

        var page = await endpoint.GetDonationsAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("don-1", page.Items[0].Id);
        Assert.Equal("camp-1", page.Items[0].CampaignId);
        Assert.Null(page.Cursor);
    }

    // ── GetAllDonationsAsync ───────────────────────────────

    [Fact]
    public async Task GetAllDonationsAsync_IteratesAllPages()
    {
        var page1 = DonationsPageResponse("don-1", "camp-1", "cursor-next");
        var page2 = DonationsPageResponse("don-2", "camp-1", null);
        var endpoint = CreateEndpoint(page1, page2);

        var donations = new List<CharityDonation>();
        await foreach (var d in endpoint.GetAllDonationsAsync("123", TestContext.Current.CancellationToken))
            donations.Add(d);

        Assert.Equal(2, donations.Count);
        Assert.Equal("don-1", donations[0].Id);
        Assert.Equal("don-2", donations[1].Id);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetCampaignAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetCampaignAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private CharityEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new CharityEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage CampaignListResponse(string id, string broadcasterId) =>
        JsonResponse($$"""{"data":[{{CampaignJson(id, broadcasterId)}}]}""");

    private static string CampaignJson(string id, string broadcasterId) => $$"""
        {
          "id": "{{id}}",
          "broadcaster_id": "{{broadcasterId}}",
          "broadcaster_name": "Broadcaster",
          "broadcaster_login": "broadcaster",
          "charity_name": "Some Charity",
          "charity_description": "Helping the world",
          "charity_logo": "https://example.com/logo.jpg",
          "charity_website": "https://example.com",
          "current_amount": {"value": 500, "decimal_places": 2, "currency": "USD"},
          "target_amount": {"value": 10000, "decimal_places": 2, "currency": "USD"}
        }
        """;

    private static HttpResponseMessage DonationsPageResponse(string id, string campaignId, string? cursor)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return JsonResponse($$"""{"data":[{{DonationJson(id, campaignId)}}],"pagination":{{paginationJson}}}""");
    }

    private static string DonationJson(string id, string campaignId) => $$"""
        {
          "id": "{{id}}",
          "campaign_id": "{{campaignId}}",
          "user_id": "user-1",
          "user_login": "donoruser",
          "user_name": "DonorUser",
          "amount": {"value": 100, "decimal_places": 2, "currency": "USD"}
        }
        """;

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
