using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Twitch.Rx.Api.Json;
using Twitch.Rx.Api.Models;

namespace Twitch.Rx.Api.Endpoints;

internal sealed class PollsEndpoint(HttpClient httpClient) : IPollsEndpoint
{
    public async Task<Poll> CreateAsync(CreatePollRequest request, CancellationToken ct = default)
    {
        var body = new CreatePollDto(
            request.BroadcasterUserId,
            request.Title,
            request.Choices.Select(c => new PollChoiceDto(c)).ToArray(),
            request.DurationSeconds);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/helix/polls")
        {
            Content = JsonContent.Create(body, TwitchApiJsonContext.Default.CreatePollDto)
        };

        var response = await httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync(
            TwitchApiJsonContext.Default.TwitchDataResponsePollDto, ct)
            ?? throw new InvalidOperationException("Failed to deserialize poll response.");

        var dto = data.Data[0];
        return new Poll(dto.Id, dto.BroadcasterUserId, dto.Title,
            dto.Choices.Select(c => new PollOptionResult(c.Id, c.Title, c.Votes)).ToArray(),
            dto.Status, dto.DurationSeconds);
    }

    public async Task EndAsync(string broadcasterId, string pollId, string status = "TERMINATED", CancellationToken ct = default)
    {
        var body = new EndPollDto(broadcasterId, pollId, status);
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, "/helix/polls")
        {
            Content = JsonContent.Create(body, TwitchApiJsonContext.Default.EndPollDto)
        };

        var response = await httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();
    }
}

internal sealed record CreatePollDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] PollChoiceDto[] Choices,
    [property: JsonPropertyName("duration")] int Duration);

internal sealed record PollChoiceDto(
    [property: JsonPropertyName("title")] string Title)
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("votes")]
    public int Votes { get; init; }
}

internal sealed record EndPollDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status);

internal sealed record PollDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterUserId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] PollChoiceDto[] Choices,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("duration")] int DurationSeconds);
