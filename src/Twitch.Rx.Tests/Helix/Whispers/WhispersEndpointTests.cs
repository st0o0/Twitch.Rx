using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Whispers;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Whispers;

public sealed class WhispersEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task SendAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.SendAsync("111", "222", "Hello!", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.SendAsync("111", "222", "Hello!", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private WhispersEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new WhispersEndpoint(httpClient, _errors);
    }

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
