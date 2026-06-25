using Twitch.Rx.Api.Endpoints;

namespace Twitch.Rx.Api;

internal sealed class TwitchApi(HttpClient httpClient) : ITwitchApi
{
    public IUsersEndpoint Users { get; } = new UsersEndpoint(httpClient);
    public IPollsEndpoint Polls { get; } = new PollsEndpoint(httpClient);
}
