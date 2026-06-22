using Twitch.Rx.Auth.Models;

namespace Twitch.Rx.Auth;

public interface ITokenStore
{
    ValueTask<AccessToken?> GetAsync(CancellationToken ct = default);
    ValueTask SetAsync(AccessToken token, CancellationToken ct = default);
    ValueTask ClearAsync(CancellationToken ct = default);
}
