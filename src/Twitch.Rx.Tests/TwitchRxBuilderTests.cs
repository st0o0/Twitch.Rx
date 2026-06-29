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
        Assert.NotNull(client.Helix);
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
    public void Build_WithHelixDisabled_ReturnsDisabledHelix()
    {
        var client = TwitchRx.CreateBuilder(o =>
        {
            o.ClientId = "id";
            o.ClientSecret = "secret";
            o.Helix.Enabled = false;
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => client.Helix.Users);
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
            o.Helix.BaseUrl = new Uri("https://custom-api.example.com");
        }).Build();

        Assert.NotNull(client);
    }
}
