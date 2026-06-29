using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Polls;

// ── Public Interface ──────────────────────────────────────

public interface IPollsEndpoint
{
    Task<Page<Poll>> GetAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Poll> GetAllAsync(string broadcasterId, CancellationToken ct = default);
    Task<Poll> CreateAsync(CreatePollRequest request, CancellationToken ct = default);
    Task<Poll> EndAsync(EndPollRequest request, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public enum PollStatus { Active, Completed, Terminated, Archived, Moderated, Invalid }
public enum PollEndStatus { Terminated, Archived }

public sealed record Poll(
    string Id,
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string Title,
    IReadOnlyList<PollChoice> Choices,
    bool BitsVotingEnabled,
    int BitsPerVote,
    bool ChannelPointsVotingEnabled,
    int ChannelPointsPerVote,
    PollStatus Status,
    int Duration,
    string StartedAt,
    string? EndedAt);

public sealed record PollChoice(
    string Id,
    string Title,
    int Votes,
    int ChannelPointsVotes,
    int BitsVotes);

public sealed record CreatePollRequest(
    string BroadcasterId,
    string Title,
    IReadOnlyList<string> Choices,
    int Duration,
    bool? ChannelPointsVotingEnabled = null,
    int? ChannelPointsPerVote = null,
    bool? BitsVotingEnabled = null,
    int? BitsPerVote = null);

public sealed record EndPollRequest(
    string BroadcasterId,
    string Id,
    PollEndStatus Status);

// ── Implementation ────────────────────────────────────────

internal sealed class PollsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IPollsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Poll>> GetAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/polls?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponsePollDto, ct, PollMappings.ToModel);

    public IAsyncEnumerable<Poll> GetAllAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/polls?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponsePollDto, ct, PollMappings.ToModel);

    public async Task<Poll> CreateAsync(CreatePollRequest request, CancellationToken ct = default)
    {
        var dto = await PostAsync("/helix/polls",
            request.ToDto(),
            Ctx.CreatePollDto,
            Ctx.HelixResponsePollDto,
            ct);
        return dto.ToModel();
    }

    public async Task<Poll> EndAsync(EndPollRequest request, CancellationToken ct = default)
    {
        var dto = await PatchAsync("/helix/polls",
            request.ToDto(),
            Ctx.EndPollDto,
            Ctx.HelixResponsePollDto,
            ct);
        return dto.ToModel();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record PollDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] PollChoiceDto[] Choices,
    [property: JsonPropertyName("bits_voting_enabled")] bool BitsVotingEnabled,
    [property: JsonPropertyName("bits_per_vote")] int BitsPerVote,
    [property: JsonPropertyName("channel_points_voting_enabled")] bool ChannelPointsVotingEnabled,
    [property: JsonPropertyName("channel_points_per_vote")] int ChannelPointsPerVote,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("started_at")] string StartedAt,
    [property: JsonPropertyName("ended_at")] string? EndedAt);

internal sealed record PollChoiceDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("votes")] int Votes,
    [property: JsonPropertyName("channel_points_votes")] int ChannelPointsVotes,
    [property: JsonPropertyName("bits_votes")] int BitsVotes);

internal sealed record CreatePollChoiceDto(
    [property: JsonPropertyName("title")] string Title);

internal sealed record CreatePollDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("choices")] CreatePollChoiceDto[] Choices,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("channel_points_voting_enabled")] bool? ChannelPointsVotingEnabled,
    [property: JsonPropertyName("channel_points_per_vote")] int? ChannelPointsPerVote,
    [property: JsonPropertyName("bits_voting_enabled")] bool? BitsVotingEnabled,
    [property: JsonPropertyName("bits_per_vote")] int? BitsPerVote);

internal sealed record EndPollDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status);

// ── Mappings (file-scoped) ────────────────────────────────

static file class PollMappings
{
    public static Poll ToModel(this PollDto dto) => new(
        dto.Id, dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.Title,
        dto.Choices.Select(c => c.ToModel()).ToArray(),
        dto.BitsVotingEnabled, dto.BitsPerVote,
        dto.ChannelPointsVotingEnabled, dto.ChannelPointsPerVote,
        dto.Status.ToPollStatus(), dto.Duration, dto.StartedAt, dto.EndedAt);

    public static PollChoice ToModel(this PollChoiceDto dto) => new(
        dto.Id, dto.Title, dto.Votes, dto.ChannelPointsVotes, dto.BitsVotes);

    private static PollStatus ToPollStatus(this string status) => status switch
    {
        "ACTIVE" => PollStatus.Active,
        "COMPLETED" => PollStatus.Completed,
        "TERMINATED" => PollStatus.Terminated,
        "ARCHIVED" => PollStatus.Archived,
        "MODERATED" => PollStatus.Moderated,
        _ => PollStatus.Invalid
    };

    public static CreatePollDto ToDto(this CreatePollRequest req) => new(
        req.BroadcasterId,
        req.Title,
        req.Choices.Select(c => new CreatePollChoiceDto(c)).ToArray(),
        req.Duration,
        req.ChannelPointsVotingEnabled,
        req.ChannelPointsPerVote,
        req.BitsVotingEnabled,
        req.BitsPerVote);

    public static EndPollDto ToDto(this EndPollRequest req) => new(
        req.BroadcasterId,
        req.Id,
        req.Status switch
        {
            PollEndStatus.Terminated => "TERMINATED",
            PollEndStatus.Archived => "ARCHIVED",
            _ => throw new ArgumentOutOfRangeException(nameof(req.Status))
        });
}
