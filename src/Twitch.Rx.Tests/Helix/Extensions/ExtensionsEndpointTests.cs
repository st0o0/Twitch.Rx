using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Extensions;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Extensions;

public sealed class ExtensionsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetExtensionsAsync_ReturnsExtensions()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"ext-1","version":"1.0","type":"panel","can_activate":true,"supported_features":["bits_enabled"]}]}"""));

        var result = await endpoint.GetExtensionsAsync("ext-1", TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("ext-1", result[0].Id);
        Assert.Equal("panel", result[0].Type);
        Assert.True(result[0].CanActivate);
    }

    [Fact]
    public async Task GetActiveExtensionsAsync_ReturnsActiveExtensions()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":{"panel":{"1":{"active":true,"id":"ext-1","version":"1.0","name":"Ext One","x":null,"y":null}},"overlay":{},"component":{}}}"""));

        var result = await endpoint.GetActiveExtensionsAsync(ct: TestContext.Current.CancellationToken);

        Assert.True(result.Panel.ContainsKey("1"));
        Assert.True(result.Panel["1"].Active);
        Assert.Equal("ext-1", result.Panel["1"].Id);
    }

    [Fact]
    public async Task UpdateActiveExtensionsAsync_ReturnsUpdatedExtensions()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":{"panel":{"1":{"active":false,"id":null,"version":null,"name":null,"x":null,"y":null}},"overlay":{},"component":{}}}"""));

        var input = new ActiveExtensions(
            Panel: new Dictionary<string, ActiveExtension> { ["1"] = new ActiveExtension(false, null, null, null, null, null) },
            Overlay: new Dictionary<string, ActiveExtension>(),
            Component: new Dictionary<string, ActiveExtension>());

        var result = await endpoint.UpdateActiveExtensionsAsync(input, TestContext.Current.CancellationToken);

        Assert.False(result.Panel["1"].Active);
    }

    [Fact]
    public async Task GetBitsProductsAsync_ReturnsBitsProducts()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"sku":"product-1","cost":{"amount":100,"type":"bits"},"in_development":false,"display_name":"Product One","expiration":"2025-01-01T00:00:00Z","is_broadcast":false}]}"""));

        var result = await endpoint.GetBitsProductsAsync(TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("product-1", result[0].Sku);
        Assert.Equal(100, result[0].Cost.Amount);
        Assert.Equal("bits", result[0].Cost.Type);
    }

    [Fact]
    public async Task GetExtensionsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetExtensionsAsync("ext-1", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private ExtensionsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ExtensionsEndpoint(httpClient, _errors);
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
