using R3;
using Twitch.Rx.Helix.Channels;
using Twitch.Rx.Helix.Chat;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

public interface ITwitchHelixApi
{
    IUsersEndpoint Users { get; }
    IChannelsEndpoint Channels { get; }
    IChatEndpoint Chat { get; }

    Observable<HelixError> Errors { get; }
}
