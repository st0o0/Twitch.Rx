using Microsoft.Extensions.DependencyInjection;
using Twitch.Rx.Api;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;

namespace Twitch.Rx.Hosting;

public static class TwitchRxServiceExtensions
{
    public static IServiceCollection AddTwitchRx(
        this IServiceCollection services,
        Action<TwitchRxOptions> configure)
    {
        var builder = TwitchRx.CreateBuilder(configure);
        var client = builder.Build();

        services.AddSingleton(client);
        services.AddSingleton(client.Auth);
        services.AddSingleton(client.Api);
        services.AddSingleton(client.EventSub);

        return services;
    }
}
