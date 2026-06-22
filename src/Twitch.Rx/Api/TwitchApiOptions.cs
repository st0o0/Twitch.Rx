namespace Twitch.Rx.Api;

public sealed class TwitchApiOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.twitch.tv");
    public bool Enabled { get; set; } = true;
}
