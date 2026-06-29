using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Goals;

// ── Public Interface ──────────────────────────────────────

public interface IGoalsEndpoint
{
    Task<IReadOnlyList<CreatorGoal>> GetAsync(string broadcasterId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record CreatorGoal(
    string Id,
    string BroadcasterId,
    string BroadcasterName,
    string BroadcasterLogin,
    string Type,
    string Description,
    int CurrentAmount,
    int TargetAmount,
    string CreatedAt);

// ── Implementation ────────────────────────────────────────

internal sealed class GoalsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IGoalsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<IReadOnlyList<CreatorGoal>> GetAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync(
            $"/helix/goals?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseCreatorGoalDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record CreatorGoalDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("current_amount")] int CurrentAmount,
    [property: JsonPropertyName("target_amount")] int TargetAmount,
    [property: JsonPropertyName("created_at")] string CreatedAt);

// ── Mappings (file-scoped) ────────────────────────────────

static file class GoalsMappings
{
    public static CreatorGoal ToModel(this CreatorGoalDto dto) => new(
        dto.Id, dto.BroadcasterId, dto.BroadcasterName, dto.BroadcasterLogin,
        dto.Type, dto.Description, dto.CurrentAmount, dto.TargetAmount, dto.CreatedAt);
}
