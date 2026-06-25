using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Twitch.Rx.Api.Json;

namespace Twitch.Rx.Api.Endpoints;

internal sealed class ChatEndpoint(HttpClient httpClient) : IChatEndpoint
{
    public async Task SendMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default)
    {
        var body = new SendChatMessageDto(broadcasterId, senderId, message);
        var request = new HttpRequestMessage(HttpMethod.Post, "/helix/chat/messages")
        {
            Content = JsonContent.Create(body, TwitchApiJsonContext.Default.SendChatMessageDto)
        };
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}

internal sealed record SendChatMessageDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("sender_id")] string SenderId,
    [property: JsonPropertyName("message")] string Message);
