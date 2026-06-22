using Twitch.Rx.Auth;
using Twitch.Rx.Auth.Models;
using Xunit;

namespace Twitch.Rx.Tests.Auth;

public sealed class InMemoryTokenStoreTests
{
    private readonly InMemoryTokenStore _store = new();

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenEmpty()
    {
        var result = await _store.GetAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredToken()
    {
        var token = new AccessToken("abc", "bearer", 3600, null, [], DateTimeOffset.UtcNow);
        await _store.SetAsync(token);

        var result = await _store.GetAsync();
        Assert.Equal(token, result);
    }

    [Fact]
    public async Task ClearAsync_RemovesToken()
    {
        var token = new AccessToken("abc", "bearer", 3600, null, [], DateTimeOffset.UtcNow);
        await _store.SetAsync(token);
        await _store.ClearAsync();

        var result = await _store.GetAsync();
        Assert.Null(result);
    }

    [Fact]
    public void AccessToken_ExpiresAt_IsCalculated()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new AccessToken("abc", "bearer", 3600, null, [], now);

        Assert.Equal(now.AddSeconds(3600), token.ExpiresAt);
    }

    [Fact]
    public void AccessToken_IsExpired_WhenPastExpiry()
    {
        var token = new AccessToken("abc", "bearer", 0, null, [], DateTimeOffset.UtcNow.AddSeconds(-10));

        Assert.True(token.IsExpired);
    }
}
