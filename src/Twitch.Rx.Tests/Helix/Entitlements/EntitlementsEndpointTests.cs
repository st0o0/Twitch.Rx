using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Entitlements;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Entitlements;

public sealed class EntitlementsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    private static readonly string EntitlementJson =
        """{"id":"ent-1","benefit_id":"benefit-1","timestamp":"2024-01-01T00:00:00Z","user_id":"user-1","game_id":"game-1","fulfillment_status":"CLAIMED","updated_at":"2024-01-01T00:00:00Z"}""";

    private static string PagedResponse(string dataJson, string? cursor = null)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return $$"""{"data":[{{dataJson}}],"pagination":{{paginationJson}}}""";
    }

    [Fact]
    public async Task GetDropsEntitlementsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(JsonResponse(PagedResponse(EntitlementJson)));

        var page = await endpoint.GetDropsEntitlementsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("ent-1", page.Items[0].Id);
        Assert.Equal("CLAIMED", page.Items[0].FulfillmentStatus);
    }

    [Fact]
    public async Task GetAllDropsEntitlementsAsync_IteratesAllPages()
    {
        var page1 = JsonResponse(PagedResponse(EntitlementJson, cursor: "next"));
        var page2 = JsonResponse(PagedResponse(EntitlementJson.Replace("ent-1", "ent-2")));
        var endpoint = CreateEndpoint(page1, page2);

        var items = new List<DropsEntitlement>();
        await foreach (var e in endpoint.GetAllDropsEntitlementsAsync(ct: TestContext.Current.CancellationToken))
            items.Add(e);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task UpdateDropsEntitlementsAsync_ReturnsUpdatedEntitlements()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"user_id":"user-1","status":"SUCCESS","ids":["ent-1"]}]}"""));

        var updated = await endpoint.UpdateDropsEntitlementsAsync(["ent-1"], "FULFILLED",
            TestContext.Current.CancellationToken);

        Assert.Single(updated);
        Assert.Equal("user-1", updated[0].UserId);
        Assert.Equal("SUCCESS", updated[0].Status);
    }

    [Fact]
    public async Task GetDropsEntitlementsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetDropsEntitlementsAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private EntitlementsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new EntitlementsEndpoint(httpClient, _errors);
    }

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
