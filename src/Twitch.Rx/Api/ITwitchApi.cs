using Twitch.Rx.Api.Endpoints;

namespace Twitch.Rx.Api;

public interface ITwitchApi
{
    IUsersEndpoint Users { get; }
}
