using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using Twitch.Rx.Auth;
using Twitch.Rx.Auth.Models;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Auth;

public sealed class TwitchAuthHandlerTests
{
    private readonly ITwitchAuth _auth = Substitute.For<ITwitchAuth>();
    private const string ClientId = "test-client";

    public TwitchAuthHandlerTests()
    {
        _auth.GetTokenAsync(Arg.Any<CancellationToken>())
            .Returns(new AccessToken("token123", "bearer", 3600, null, [], DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task SendAsync_AddsAuthAndClientIdHeaders()
    {
        var inner = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(inner);

        await client.GetAsync("https://api.twitch.tv/helix/users", TestContext.Current.CancellationToken);

        var clientIdValues = inner.LastRequest!.Headers.GetValues("Client-Id").ToList();
        Assert.Single(clientIdValues);
        Assert.Equal(ClientId, clientIdValues[0]);
        Assert.NotNull(inner.LastRequest.Headers.Authorization);
        Assert.Equal("Bearer", inner.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("token123", inner.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_RefreshesAndRetries_On401()
    {
        var refreshed = new AccessToken("new-token", "bearer", 3600, null, [], DateTimeOffset.UtcNow);
        _auth.RefreshTokenAsync(Arg.Any<CancellationToken>()).Returns(refreshed);

        var inner = new FakeHttpHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(inner);

        var response = await client.GetAsync("https://api.twitch.tv/helix/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _auth.Received(1).RefreshTokenAsync(Arg.Any<CancellationToken>());
        Assert.Equal(2, inner.RequestCount);
    }

    [Fact]
    public async Task SendAsync_WaitsAndRetries_On429()
    {
        var rateLimited = new HttpResponseMessage((HttpStatusCode)429);
        rateLimited.Headers.TryAddWithoutValidation(
            "Ratelimit-Reset",
            DateTimeOffset.UtcNow.AddMilliseconds(100).ToUnixTimeSeconds().ToString());

        var inner = new FakeHttpHandler(rateLimited, new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(inner);

        var response = await client.GetAsync("https://api.twitch.tv/helix/users", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.RequestCount);
    }

    private HttpClient CreateClient(HttpMessageHandler inner)
    {
        var handler = new TwitchAuthHandler(_auth, ClientId) { InnerHandler = inner };
        return new HttpClient(handler);
    }
}
