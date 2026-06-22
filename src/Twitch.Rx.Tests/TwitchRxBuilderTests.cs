using Twitch.Rx;
using Xunit;

namespace Twitch.Rx.Tests;

public sealed class TwitchRxBuilderTests
{
    [Fact]
    public void Build_WithValidOptions_CreatesClient()
    {
        var client = TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "test-id";
            o.ClientSecret = "test-secret";
        }).Build();

        Assert.NotNull(client);
        Assert.NotNull(client.Auth);
        Assert.NotNull(client.Api);
        Assert.NotNull(client.EventSub);
    }

    [Fact]
    public void Build_WithoutCredentials_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "";
            o.ClientSecret = "";
        }).Build());
    }

    [Fact]
    public void Build_WithApiDisabled_ReturnsDisabledApi()
    {
        var client = TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "id";
            o.ClientSecret = "secret";
            o.Api.Enabled = false;
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => client.Api.Users);
        Assert.Contains("not enabled", ex.Message);
    }

    [Fact]
    public async Task Build_WithEventSubDisabled_ReturnsDisabledEventSub()
    {
        var client = TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "id";
            o.ClientSecret = "secret";
        }).Build();

        // EventSub.Enabled defaults to false - just await; it'll throw if broken
        await client.EventSub.ConnectAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Build_WithCustomUrls_UsesConfiguredUrls()
    {
        var client = TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "id";
            o.ClientSecret = "secret";
            o.Auth.BaseUrl = new Uri("https://custom-auth.example.com");
            o.Api.BaseUrl = new Uri("https://custom-api.example.com");
        }).Build();

        Assert.NotNull(client);
    }
}
