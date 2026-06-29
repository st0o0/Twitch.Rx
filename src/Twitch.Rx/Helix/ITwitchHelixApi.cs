using R3;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Games;
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

    Observable<HelixError> Errors { get; }
}
