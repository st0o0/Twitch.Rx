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

public interface ITwitchHelixApi
{
    IUsersEndpoint Users { get; }
    IChannelsEndpoint Channels { get; }
    IChatEndpoint Chat { get; }
    IStreamsEndpoint Streams { get; }
    ISubscriptionsEndpoint Subscriptions { get; }
    IGamesEndpoint Games { get; }
    IVideosEndpoint Videos { get; }
    IPollsEndpoint Polls { get; }
    IPredictionsEndpoint Predictions { get; }
    IBitsEndpoint Bits { get; }
    IClipsEndpoint Clips { get; }
    IChannelPointsEndpoint ChannelPoints { get; }
    IModerationEndpoint Moderation { get; }
    ISearchEndpoint Search { get; }
    ITeamsEndpoint Teams { get; }
    IHypeTrainEndpoint HypeTrain { get; }
    IAnalyticsEndpoint Analytics { get; }
    ICharityEndpoint Charity { get; }
    IAdsEndpoint Ads { get; }
    IConduitsEndpoint Conduits { get; }
    IContentClassificationEndpoint ContentClassification { get; }
    IEntitlementsEndpoint Entitlements { get; }
    IExtensionsEndpoint Extensions { get; }
    IGoalsEndpoint Goals { get; }
    IGuestStarEndpoint GuestStar { get; }
    IRaidsEndpoint Raids { get; }
    IScheduleEndpoint Schedule { get; }
    IWhispersEndpoint Whispers { get; }

    Observable<HelixError> Errors { get; }
}
