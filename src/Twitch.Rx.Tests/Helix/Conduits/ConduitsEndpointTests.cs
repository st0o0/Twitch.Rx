using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Conduits;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Conduits;

public sealed class ConduitsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetAsync_ReturnsConduits()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"conduit-1","shard_count":5}]}"""));

        var conduits = await endpoint.GetAsync(TestContext.Current.CancellationToken);

        Assert.Single(conduits);
        Assert.Equal("conduit-1", conduits[0].Id);
        Assert.Equal(5, conduits[0].ShardCount);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConduit()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"conduit-new","shard_count":3}]}"""));

        var conduit = await endpoint.CreateAsync(3, TestContext.Current.CancellationToken);

        Assert.Equal("conduit-new", conduit.Id);
        Assert.Equal(3, conduit.ShardCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedConduit()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"conduit-1","shard_count":10}]}"""));

        var conduit = await endpoint.UpdateAsync("conduit-1", 10, TestContext.Current.CancellationToken);

        Assert.Equal("conduit-1", conduit.Id);
        Assert.Equal(10, conduit.ShardCount);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.DeleteAsync("conduit-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetShardsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"shard-0","status":"enabled","transport":{"method":"webhook","callback":"https://example.com","session_id":null}}],"pagination":{}}"""));

        var page = await endpoint.GetShardsAsync("conduit-1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("shard-0", page.Items[0].Id);
        Assert.Equal("webhook", page.Items[0].Transport.Method);
    }

    [Fact]
    public async Task UpdateShardsAsync_ReturnsUpdatedShards()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"shard-0","status":"webhook_callback_verification_pending","transport":{"method":"webhook","callback":"https://example.com/new","session_id":null}}]}"""));

        var shards = await endpoint.UpdateShardsAsync("conduit-1",
            [new UpdateShardRequest("shard-0", "webhook", Callback: "https://example.com/new")],
            TestContext.Current.CancellationToken);

        Assert.Single(shards);
        Assert.Equal("shard-0", shards[0].Id);
    }

    [Fact]
    public async Task GetAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private ConduitsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ConduitsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage NoContentResponse() => new(HttpStatusCode.NoContent);

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
