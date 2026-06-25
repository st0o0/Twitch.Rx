namespace Twitch.Rx.Api.Endpoints;

public interface IChatEndpoint
{
    Task SendMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default);
}
