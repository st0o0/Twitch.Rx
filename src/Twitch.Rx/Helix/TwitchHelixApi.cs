using R3;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Games;
using Twitch.Rx.Helix.Streams;
using Twitch.Rx.Helix.Subscriptions;
using Twitch.Rx.Helix.Users;
using Twitch.Rx.Helix.Videos;

namespace Twitch.Rx.Helix;

internal sealed class TwitchHelixApi : ITwitchHelixApi
{
    private readonly Subject<HelixError> _errors = new();

    public TwitchHelixApi(HttpClient httpClient)
    {
        Users = new UsersEndpoint(httpClient, _errors);
        Channels = new ChannelsEndpoint(httpClient, _errors);
        Chat = new ChatEndpoint(httpClient, _errors);
        Streams = new StreamsEndpoint(httpClient, _errors);
        Subscriptions = new SubscriptionsEndpoint(httpClient, _errors);
        Games = new GamesEndpoint(httpClient, _errors);
        Videos = new VideosEndpoint(httpClient, _errors);
    }

    public IUsersEndpoint Users { get; }
    public IChannelsEndpoint Channels { get; }
    public IChatEndpoint Chat { get; }
    public IStreamsEndpoint Streams { get; }
    public ISubscriptionsEndpoint Subscriptions { get; }
    public IGamesEndpoint Games { get; }
    public IVideosEndpoint Videos { get; }

    public Observable<HelixError> Errors => _errors;
}
