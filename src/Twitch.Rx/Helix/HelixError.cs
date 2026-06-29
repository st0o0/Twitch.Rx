namespace Twitch.Rx.Helix;

public sealed record HelixError(int StatusCode, string Error, string Message, HttpMethod Method, string Url);
