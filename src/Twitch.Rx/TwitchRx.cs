namespace Twitch.Rx;

public static class TwitchRx
{
    public static TwitchRxBuilder CreateBuilder(Action<TwitchRxOptions> configure)
        => new(configure);
}
