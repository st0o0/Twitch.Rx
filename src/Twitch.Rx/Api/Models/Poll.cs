namespace Twitch.Rx.Api.Models;

public sealed record Poll(
    string Id,
    string BroadcasterUserId,
    string Title,
    IReadOnlyList<PollOptionResult> Choices,
    string Status,
    int DurationSeconds);

public sealed record PollOptionResult(string Id, string Title, int Votes);

public sealed record CreatePollRequest(
    string BroadcasterUserId,
    string Title,
    IReadOnlyList<string> Choices,
    int DurationSeconds);
