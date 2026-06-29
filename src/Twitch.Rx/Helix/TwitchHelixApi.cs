using R3;
using Twitch.Rx.Helix.Ads;
using Twitch.Rx.Helix.Analytics;
using Twitch.Rx.Helix.Bits;
using Twitch.Rx.Helix.ChannelPoints;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Charity;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Clips;
using Twitch.Rx.Helix.Conduits;
using Twitch.Rx.Helix.ContentClassification;
using Twitch.Rx.Helix.Entitlements;
using Twitch.Rx.Helix.Extensions;
using Twitch.Rx.Helix.Games;
using Twitch.Rx.Helix.Goals;
using Twitch.Rx.Helix.GuestStar;
using Twitch.Rx.Helix.HypeTrain;
using Twitch.Rx.Helix.Moderation;
using Twitch.Rx.Helix.Polls;
using Twitch.Rx.Helix.Predictions;
using Twitch.Rx.Helix.Raids;
using Twitch.Rx.Helix.Schedule;
using Twitch.Rx.Helix.Search;
using Twitch.Rx.Helix.Streams;
using Twitch.Rx.Helix.Subscriptions;
using Twitch.Rx.Helix.Teams;
using Twitch.Rx.Helix.Users;
using Twitch.Rx.Helix.Videos;
using Twitch.Rx.Helix.Whispers;

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
        Search = new SearchEndpoint(httpClient, _errors);
        Teams = new TeamsEndpoint(httpClient, _errors);
        HypeTrain = new HypeTrainEndpoint(httpClient, _errors);
        Analytics = new AnalyticsEndpoint(httpClient, _errors);
        Charity = new CharityEndpoint(httpClient, _errors);
        Ads = new AdsEndpoint(httpClient, _errors);
        Conduits = new ConduitsEndpoint(httpClient, _errors);
        ContentClassification = new ContentClassificationEndpoint(httpClient, _errors);
        Entitlements = new EntitlementsEndpoint(httpClient, _errors);
        Extensions = new ExtensionsEndpoint(httpClient, _errors);
        Goals = new GoalsEndpoint(httpClient, _errors);
        GuestStar = new GuestStarEndpoint(httpClient, _errors);
        Raids = new RaidsEndpoint(httpClient, _errors);
        Schedule = new ScheduleEndpoint(httpClient, _errors);
        Whispers = new WhispersEndpoint(httpClient, _errors);
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
    public ISearchEndpoint Search { get; }
    public ITeamsEndpoint Teams { get; }
    public IHypeTrainEndpoint HypeTrain { get; }
    public IAnalyticsEndpoint Analytics { get; }
    public ICharityEndpoint Charity { get; }
    public IAdsEndpoint Ads { get; }
    public IConduitsEndpoint Conduits { get; }
    public IContentClassificationEndpoint ContentClassification { get; }
    public IEntitlementsEndpoint Entitlements { get; }
    public IExtensionsEndpoint Extensions { get; }
    public IGoalsEndpoint Goals { get; }
    public IGuestStarEndpoint GuestStar { get; }
    public IRaidsEndpoint Raids { get; }
    public IScheduleEndpoint Schedule { get; }
    public IWhispersEndpoint Whispers { get; }

    public Observable<HelixError> Errors => _errors;
}
