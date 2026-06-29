using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;
using Twitch.Rx.Helix;

namespace Twitch.Rx;

public sealed class TwitchRxOptions
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public TwitchAuthOptions Auth { get; set; } = new();
    public HelixOptions Helix { get; set; } = new();
    public TwitchEventSubOptions EventSub { get; set; } = new();
}
