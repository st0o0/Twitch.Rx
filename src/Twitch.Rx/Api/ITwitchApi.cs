using Twitch.Rx.Api.Endpoints;

namespace Twitch.Rx.Api;

public interface ITwitchApi
{
    IUsersEndpoint Users { get; }
    IPollsEndpoint Polls { get; }
    IChatEndpoint Chat { get; }
}
