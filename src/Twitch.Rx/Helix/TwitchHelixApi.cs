using R3;
using Twitch.Rx.Helix.Bits;
using Twitch.Rx.Helix.ChannelPoints;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Clips;
using Twitch.Rx.Helix.Games;
using Twitch.Rx.Helix.Moderation;
using Twitch.Rx.Helix.Polls;
using Twitch.Rx.Helix.Predictions;
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
        Polls = new PollsEndpoint(httpClient, _errors);
        Predictions = new PredictionsEndpoint(httpClient, _errors);
        Bits = new BitsEndpoint(httpClient, _errors);
        Clips = new ClipsEndpoint(httpClient, _errors);
        ChannelPoints = new ChannelPointsEndpoint(httpClient, _errors);
        Moderation = new ModerationEndpoint(httpClient, _errors);
    }

    public IUsersEndpoint Users { get; }
    public IChannelsEndpoint Channels { get; }
    public IChatEndpoint Chat { get; }
    public IStreamsEndpoint Streams { get; }
    public ISubscriptionsEndpoint Subscriptions { get; }
    public IGamesEndpoint Games { get; }
    public IVideosEndpoint Videos { get; }
    public IPollsEndpoint Polls { get; }
    public IPredictionsEndpoint Predictions { get; }
    public IBitsEndpoint Bits { get; }
    public IClipsEndpoint Clips { get; }
    public IChannelPointsEndpoint ChannelPoints { get; }
    public IModerationEndpoint Moderation { get; }

    public Observable<HelixError> Errors => _errors;
}
