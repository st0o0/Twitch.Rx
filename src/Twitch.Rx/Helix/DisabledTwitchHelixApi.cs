using R3;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

internal sealed class DisabledTwitchHelixApi : ITwitchHelixApi
{
    private static T Throw<T>() =>
        throw new InvalidOperationException(
            "Helix API is not enabled. Call WithHelix() on the builder or set HelixOptions.Enabled = true.");

    public IUsersEndpoint Users => Throw<IUsersEndpoint>();

    public Observable<HelixError> Errors => Throw<Observable<HelixError>>();
}
