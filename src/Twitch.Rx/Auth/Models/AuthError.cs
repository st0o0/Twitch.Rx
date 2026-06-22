namespace Twitch.Rx.Auth.Models;

public sealed record AuthError(string Message, Exception? Exception = null);
