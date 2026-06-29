namespace Twitch.Rx.Helix;

public sealed class HelixOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.twitch.tv");
    public bool Enabled { get; set; } = true;
}
