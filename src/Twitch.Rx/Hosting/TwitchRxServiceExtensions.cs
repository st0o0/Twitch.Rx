using Microsoft.Extensions.DependencyInjection;
using Twitch.Rx.Helix;

namespace Twitch.Rx.Hosting;

public static class TwitchRxServiceExtensions
{
    public static IServiceCollection AddTwitchRx(
        this IServiceCollection services,
        Action<TwitchRxOptions> configure)
    {
        services.AddSingleton<ITwitchRxClient>(sp =>
        {
            var builder = TwitchRx.CreateBuilder(configure);
            return builder.Build();
        });
        services.AddSingleton(sp => sp.GetRequiredService<ITwitchRxClient>().Auth);
        services.AddSingleton(sp => sp.GetRequiredService<ITwitchRxClient>().Helix);
        services.AddSingleton(sp => sp.GetRequiredService<ITwitchRxClient>().EventSub);

        return services;
    }
}
