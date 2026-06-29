namespace Twitch.Rx.Helix;

public sealed class HelixException(int statusCode, string error, string message, HttpMethod method, string url)
    : Exception($"Twitch API {statusCode} {error}: {message}")
{
    public int StatusCode { get; } = statusCode;
    public string Error { get; } = error;
    public HttpMethod Method { get; } = method;
    public string Url { get; } = url;
}
