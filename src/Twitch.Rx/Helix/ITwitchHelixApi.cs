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

    Observable<HelixError> Errors { get; }
}
