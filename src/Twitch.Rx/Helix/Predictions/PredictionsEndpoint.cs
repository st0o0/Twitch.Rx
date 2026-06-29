using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Predictions;

// ── Public Interface ──────────────────────────────────────

public interface IPredictionsEndpoint
{
    Task<Page<Prediction>> GetAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Prediction> GetAllAsync(string broadcasterId, CancellationToken ct = default);
    Task<Prediction> CreateAsync(CreatePredictionRequest request, CancellationToken ct = default);
    Task<Prediction> EndAsync(EndPredictionRequest request, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public enum PredictionStatus { Active, Canceled, Locked, Resolved }
public enum PredictionEndStatus { Resolved, Canceled, Locked }

public sealed record Prediction(
    string Id,
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName,
    string Title,
    string? WinningOutcomeId,
    IReadOnlyList<PredictionOutcome> Outcomes,
    int PredictionWindow,
    PredictionStatus Status,
    string CreatedAt,
    string? EndedAt,
    string? LockedAt);

public sealed record PredictionOutcome(
    string Id,
    string Title,
    int Users,
    int ChannelPoints,
    IReadOnlyList<TopPredictor>? TopPredictors,
    string Color);

public sealed record TopPredictor(
    string UserId,
    string UserLogin,
    string UserName,
    int ChannelPointsUsed,
    int? ChannelPointsWon);

public sealed record CreatePredictionRequest(
    string BroadcasterId,
    string Title,
    IReadOnlyList<string> Outcomes,
    int PredictionWindow);

public sealed record EndPredictionRequest(
    string BroadcasterId,
    string Id,
    PredictionEndStatus Status,
    string? WinningOutcomeId = null);

// ── Implementation ────────────────────────────────────────

internal sealed class PredictionsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IPredictionsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Prediction>> GetAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/predictions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            cursor, Ctx.HelixPaginatedResponsePredictionDto, ct, PredictionMappings.ToModel);

    public IAsyncEnumerable<Prediction> GetAllAsync(string broadcasterId, CancellationToken ct = default)
        => GetAllPagesAsync($"/helix/predictions?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixPaginatedResponsePredictionDto, ct, PredictionMappings.ToModel);

    public async Task<Prediction> CreateAsync(CreatePredictionRequest request, CancellationToken ct = default)
    {
        var dto = await PostAsync("/helix/predictions",
            request.ToDto(),
            Ctx.CreatePredictionDto,
            Ctx.HelixResponsePredictionDto,
            ct);
        return dto.ToModel();
    }

    public async Task<Prediction> EndAsync(EndPredictionRequest request, CancellationToken ct = default)
    {
        var dto = await PatchAsync("/helix/predictions",
            request.ToDto(),
            Ctx.EndPredictionDto,
            Ctx.HelixResponsePredictionDto,
            ct);
        return dto.ToModel();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record PredictionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("winning_outcome_id")] string? WinningOutcomeId,
    [property: JsonPropertyName("outcomes")] PredictionOutcomeDto[] Outcomes,
    [property: JsonPropertyName("prediction_window")] int PredictionWindow,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("ended_at")] string? EndedAt,
    [property: JsonPropertyName("locked_at")] string? LockedAt);

internal sealed record PredictionOutcomeDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("users")] int Users,
    [property: JsonPropertyName("channel_points")] int ChannelPoints,
    [property: JsonPropertyName("top_predictors")] TopPredictorDto[]? TopPredictors,
    [property: JsonPropertyName("color")] string Color);

internal sealed record TopPredictorDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("user_login")] string UserLogin,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("channel_points_used")] int ChannelPointsUsed,
    [property: JsonPropertyName("channel_points_won")] int? ChannelPointsWon);

internal sealed record CreatePredictionOutcomeDto(
    [property: JsonPropertyName("title")] string Title);

internal sealed record CreatePredictionDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("outcomes")] CreatePredictionOutcomeDto[] Outcomes,
    [property: JsonPropertyName("prediction_window")] int PredictionWindow);

internal sealed record EndPredictionDto(
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("winning_outcome_id")] string? WinningOutcomeId);

// ── Mappings (file-scoped) ────────────────────────────────

static file class PredictionMappings
{
    public static Prediction ToModel(this PredictionDto dto) => new(
        dto.Id, dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName,
        dto.Title, dto.WinningOutcomeId,
        dto.Outcomes.Select(o => o.ToModel()).ToArray(),
        dto.PredictionWindow, dto.Status.ToPredictionStatus(),
        dto.CreatedAt, dto.EndedAt, dto.LockedAt);

    private static PredictionOutcome ToModel(this PredictionOutcomeDto dto) => new(
        dto.Id, dto.Title, dto.Users, dto.ChannelPoints,
        dto.TopPredictors?.Select(p => p.ToModel()).ToArray(),
        dto.Color);

    private static TopPredictor ToModel(this TopPredictorDto dto) => new(
        dto.UserId, dto.UserLogin, dto.UserName,
        dto.ChannelPointsUsed, dto.ChannelPointsWon);

    private static PredictionStatus ToPredictionStatus(this string status) => status switch
    {
        "ACTIVE" => PredictionStatus.Active,
        "CANCELED" => PredictionStatus.Canceled,
        "LOCKED" => PredictionStatus.Locked,
        "RESOLVED" => PredictionStatus.Resolved,
        _ => PredictionStatus.Active
    };

    public static CreatePredictionDto ToDto(this CreatePredictionRequest req) => new(
        req.BroadcasterId,
        req.Title,
        req.Outcomes.Select(o => new CreatePredictionOutcomeDto(o)).ToArray(),
        req.PredictionWindow);

    public static EndPredictionDto ToDto(this EndPredictionRequest req) => new(
        req.BroadcasterId,
        req.Id,
        req.Status switch
        {
            PredictionEndStatus.Resolved => "RESOLVED",
            PredictionEndStatus.Canceled => "CANCELED",
            PredictionEndStatus.Locked => "LOCKED",
            _ => throw new ArgumentOutOfRangeException(nameof(req.Status))
        },
        req.WinningOutcomeId);
}
