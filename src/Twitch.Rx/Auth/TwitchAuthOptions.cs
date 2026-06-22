namespace Twitch.Rx.Auth;

public sealed class TwitchAuthOptions
{
    public Uri BaseUrl { get; set; } = new("https://id.twitch.tv");
}
