using R3;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

internal sealed class TwitchHelixApi : ITwitchHelixApi
{
    private readonly Subject<HelixError> _errors = new();

    public TwitchHelixApi(HttpClient httpClient)
    {
        Users = new UsersEndpoint(httpClient, _errors);
        Channels = new ChannelsEndpoint(httpClient, _errors);
        Chat = new ChatEndpoint(httpClient, _errors);
    }

    public IUsersEndpoint Users { get; }
    public IChannelsEndpoint Channels { get; }
    public IChatEndpoint Chat { get; }

    public Observable<HelixError> Errors => _errors;
}
