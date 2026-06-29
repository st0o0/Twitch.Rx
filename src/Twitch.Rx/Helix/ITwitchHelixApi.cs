using R3;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

public interface ITwitchHelixApi
{
    IUsersEndpoint Users { get; }

    Observable<HelixError> Errors { get; }
}
