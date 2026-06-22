namespace Twitch.Rx.Auth.Models;

public sealed record AccessToken(
    string Token,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string[] Scopes,
    DateTimeOffset ObtainedAt)
{
    public DateTimeOffset ExpiresAt => ObtainedAt.AddSeconds(ExpiresIn);
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
