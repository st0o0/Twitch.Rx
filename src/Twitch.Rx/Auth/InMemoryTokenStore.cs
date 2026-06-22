using Twitch.Rx.Auth.Models;

namespace Twitch.Rx.Auth;

public sealed class InMemoryTokenStore : ITokenStore
{
    private volatile AccessToken? _token;

    public ValueTask<AccessToken?> GetAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_token);

    public ValueTask SetAsync(AccessToken token, CancellationToken ct = default)
    {
        _token = token;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken ct = default)
    {
        _token = null;
        return ValueTask.CompletedTask;
    }
}
