using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Raids;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Raids;

public sealed class RaidsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task StartAsync_ReturnsRaidInfo()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"created_at":"2024-01-01T00:00:00Z","is_mature":false}]}"""));

        var raid = await endpoint.StartAsync("111", "222", TestContext.Current.CancellationToken);

        Assert.Equal("2024-01-01T00:00:00Z", raid.CreatedAt);
        Assert.False(raid.IsMature);
    }

    [Fact]
    public async Task CancelAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.CancelAsync("123", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.StartAsync("111", "222", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private RaidsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new RaidsEndpoint(httpClient, _errors);
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
