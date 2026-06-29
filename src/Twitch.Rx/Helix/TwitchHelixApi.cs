using R3;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

internal sealed class TwitchHelixApi : ITwitchHelixApi
{
    private readonly Subject<HelixError> _errors = new();

    public TwitchHelixApi(HttpClient httpClient)
    {
        Users = new UsersEndpoint(httpClient, _errors);
    }

    public IUsersEndpoint Users { get; }

    public Observable<HelixError> Errors => _errors;
}
