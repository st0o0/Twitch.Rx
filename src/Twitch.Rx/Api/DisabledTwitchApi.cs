using Twitch.Rx.Api.Endpoints;

namespace Twitch.Rx.Api;

internal sealed class DisabledTwitchApi : ITwitchApi
{
    public IUsersEndpoint Users => throw new InvalidOperationException("API is not enabled. Set TwitchRxOptions.Api.Enabled = true.");
    public IPollsEndpoint Polls => throw new InvalidOperationException("API is not enabled. Set TwitchRxOptions.Api.Enabled = true.");
    public IChatEndpoint Chat => throw new InvalidOperationException("API is not enabled. Set TwitchRxOptions.Api.Enabled = true.");
}
