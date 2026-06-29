using Microsoft.Extensions.DependencyInjection;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;
using Twitch.Rx.Helix;
using Twitch.Rx.Hosting;
using Xunit;

namespace Twitch.Rx.Tests.Hosting;

public sealed class TwitchRxServiceExtensionsTests
{
    [Fact]
    public void AddTwitchRx_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddTwitchRx(o =>
        {
            o.ClientId = "test-id";
            o.ClientSecret = "test-secret";
        });

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ITwitchRxClient>());
        Assert.NotNull(provider.GetRequiredService<ITwitchAuth>());
        Assert.NotNull(provider.GetRequiredService<ITwitchHelixApi>());
        Assert.NotNull(provider.GetRequiredService<ITwitchEventSub>());
    }

    [Fact]
    public void AddTwitchRx_ClientIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddTwitchRx(o =>
        {
            o.ClientId = "test-id";
            o.ClientSecret = "test-secret";
        });

        var provider = services.BuildServiceProvider();
        var c1 = provider.GetRequiredService<ITwitchRxClient>();
        var c2 = provider.GetRequiredService<ITwitchRxClient>();

        Assert.Same(c1, c2);
    }
}
