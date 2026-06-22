namespace Twitch.Rx.Auth.Models;

public sealed record TokenValidation(
    string ClientId,
    string? Login,
    string[] Scopes,
    string? UserId,
    int ExpiresIn);
