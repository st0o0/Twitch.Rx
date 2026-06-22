using R3;
using Twitch.Rx.Auth.Models;

namespace Twitch.Rx.Auth;

public interface ITwitchAuth : IAsyncDisposable
{
    Observable<AccessToken> TokenChanged { get; }
    Observable<AuthError> Errors { get; }

    ValueTask<AccessToken> GetTokenAsync(CancellationToken ct = default);
    ValueTask<AccessToken> RefreshTokenAsync(CancellationToken ct = default);
    ValueTask<TokenValidation> ValidateAsync(CancellationToken ct = default);
}
