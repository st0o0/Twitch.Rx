using Twitch.Rx.Api;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;

namespace Twitch.Rx;

public sealed class TwitchRxOptions
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public TwitchAuthOptions Auth { get; set; } = new();
    public TwitchApiOptions Api { get; set; } = new();
    public TwitchEventSubOptions EventSub { get; set; } = new();
}
