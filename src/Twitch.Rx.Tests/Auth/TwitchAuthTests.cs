using System.Net;
using R3;
using Twitch.Rx.Auth;
using Twitch.Rx.Auth.Models;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Auth;

public sealed class TwitchAuthTests : IDisposable
{
    private readonly TwitchRxOptions _options = new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret"
    };
    private readonly InMemoryTokenStore _tokenStore = new();
    private readonly List<IDisposable> _subscriptions = [];

    [Fact]
    public async Task GetTokenAsync_AcquiresViaClientCredentials()
    {
        var handler = new FakeHttpHandler(TokenResponse("test-token", 3600));
        await using var auth = CreateAuth(handler);

        var token = await auth.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test-token", token.Token);
        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal("/oauth2/token", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsCached_WhenNotExpired()
    {
        var handler = new FakeHttpHandler(TokenResponse("token", 3600));
        await using var auth = CreateAuth(handler);

        await auth.GetTokenAsync(TestContext.Current.CancellationToken);
        await auth.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetTokenAsync_SeedsFromOptions_WhenAccessTokenSet()
    {
        _options.AccessToken = "pre-seeded";
        _options.RefreshToken = "refresh";
        var handler = new FakeHttpHandler();
        await using var auth = CreateAuth(handler);

        var token = await auth.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("pre-seeded", token.Token);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GetTokenAsync_EmitsTokenChanged()
    {
        var handler = new FakeHttpHandler(TokenResponse("token", 3600));
        await using var auth = CreateAuth(handler);

        AccessToken? emitted = null;
        _subscriptions.Add(auth.TokenChanged.Subscribe(t => emitted = t));

        await auth.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(emitted);
        Assert.Equal("token", emitted!.Token);
    }

    [Fact]
    public async Task RefreshTokenAsync_UsesRefreshGrant()
    {
        _options.AccessToken = "old";
        _options.RefreshToken = "refresh-123";
        var handler = new FakeHttpHandler(TokenResponse("refreshed", 3600, "new-refresh"));
        await using var auth = CreateAuth(handler);

        var token = await auth.RefreshTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("refreshed", token.Token);
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains("refresh_token=refresh-123", body);
    }

    [Fact]
    public async Task ValidateAsync_CallsValidateEndpoint()
    {
        var handler = new FakeHttpHandler(
            TokenResponse("token", 3600),
            ValidationResponse());
        await using var auth = CreateAuth(handler);

        await auth.GetTokenAsync(TestContext.Current.CancellationToken);
        var validation = await auth.ValidateAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test-client-id", validation.ClientId);
        Assert.Equal("testuser", validation.Login);
    }

    private TwitchAuth CreateAuth(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = _options.Auth.BaseUrl };
        return new TwitchAuth(_options, httpClient, _tokenStore);
    }

    private static HttpResponseMessage TokenResponse(string token, int expiresIn, string? refresh = null) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""{"access_token":"{{token}}","expires_in":{{expiresIn}},"token_type":"bearer"{{(refresh is not null ? $",\"refresh_token\":\"{refresh}\",\"scope\":[]" : "")}}}""",
            System.Text.Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage ValidationResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            """{"client_id":"test-client-id","login":"testuser","scopes":[],"user_id":"12345","expires_in":3600}""",
            System.Text.Encoding.UTF8, "application/json")
    };

    public void Dispose()
    {
        foreach (var sub in _subscriptions) sub.Dispose();
    }
}
