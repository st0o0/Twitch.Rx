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

internal sealed class DisabledTwitchHelixApi : ITwitchHelixApi
{
    private static T Throw<T>() =>
        throw new InvalidOperationException(
            "Helix API is not enabled. Call WithHelix() on the builder or set HelixOptions.Enabled = true.");

    public IUsersEndpoint Users => Throw<IUsersEndpoint>();
    public IChannelsEndpoint Channels => Throw<IChannelsEndpoint>();
    public IChatEndpoint Chat => Throw<IChatEndpoint>();
    public IStreamsEndpoint Streams => Throw<IStreamsEndpoint>();
    public ISubscriptionsEndpoint Subscriptions => Throw<ISubscriptionsEndpoint>();
    public IGamesEndpoint Games => Throw<IGamesEndpoint>();
    public IVideosEndpoint Videos => Throw<IVideosEndpoint>();
    public IPollsEndpoint Polls => Throw<IPollsEndpoint>();
    public IPredictionsEndpoint Predictions => Throw<IPredictionsEndpoint>();
    public IBitsEndpoint Bits => Throw<IBitsEndpoint>();
    public IClipsEndpoint Clips => Throw<IClipsEndpoint>();
    public IChannelPointsEndpoint ChannelPoints => Throw<IChannelPointsEndpoint>();
    public IModerationEndpoint Moderation => Throw<IModerationEndpoint>();
    public ISearchEndpoint Search => Throw<ISearchEndpoint>();
    public ITeamsEndpoint Teams => Throw<ITeamsEndpoint>();
    public IHypeTrainEndpoint HypeTrain => Throw<IHypeTrainEndpoint>();
    public IAnalyticsEndpoint Analytics => Throw<IAnalyticsEndpoint>();
    public ICharityEndpoint Charity => Throw<ICharityEndpoint>();
    public IAdsEndpoint Ads => Throw<IAdsEndpoint>();
    public IConduitsEndpoint Conduits => Throw<IConduitsEndpoint>();
    public IContentClassificationEndpoint ContentClassification => Throw<IContentClassificationEndpoint>();
    public IEntitlementsEndpoint Entitlements => Throw<IEntitlementsEndpoint>();
    public IExtensionsEndpoint Extensions => Throw<IExtensionsEndpoint>();
    public IGoalsEndpoint Goals => Throw<IGoalsEndpoint>();
    public IGuestStarEndpoint GuestStar => Throw<IGuestStarEndpoint>();
    public IRaidsEndpoint Raids => Throw<IRaidsEndpoint>();
    public IScheduleEndpoint Schedule => Throw<IScheduleEndpoint>();
    public IWhispersEndpoint Whispers => Throw<IWhispersEndpoint>();

    public Observable<HelixError> Errors => Throw<Observable<HelixError>>();
}
