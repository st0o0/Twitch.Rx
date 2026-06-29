using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Teams;

// ── Public Interface ──────────────────────────────────────

public interface ITeamsEndpoint
{
    Task<Team?> GetByIdAsync(string teamId, CancellationToken ct = default);
    Task<Team?> GetByNameAsync(string teamName, CancellationToken ct = default);
    Task<IReadOnlyList<ChannelTeam>> GetChannelTeamsAsync(string broadcasterId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Team(
    string Id,
    string TeamName,
    string TeamDisplayName,
    string Info,
    string ThumbnailUrl,
    string BackgroundImageUrl,
    string Banner,
    string CreatedAt,
    string UpdatedAt);

public sealed record ChannelTeam(
    string Id,
    string TeamName,
    string TeamDisplayName,
    string Info,
    string ThumbnailUrl,
    string BackgroundImageUrl,
    string Banner,
    string CreatedAt,
    string UpdatedAt,
    string BroadcasterId,
    string BroadcasterLogin,
    string BroadcasterName);

// ── Implementation ────────────────────────────────────────

internal sealed class TeamsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), ITeamsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<Team?> GetByIdAsync(string teamId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/teams?id={Uri.EscapeDataString(teamId)}",
            Ctx.HelixResponseTeamDto, ct))?.ToModel();

    public async Task<Team?> GetByNameAsync(string teamName, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/teams?name={Uri.EscapeDataString(teamName)}",
            Ctx.HelixResponseTeamDto, ct))?.ToModel();

    public async Task<IReadOnlyList<ChannelTeam>> GetChannelTeamsAsync(string broadcasterId, CancellationToken ct = default)
    {
        var dtos = await GetListAsync(
            $"/helix/teams/channel?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            Ctx.HelixResponseChannelTeamDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record TeamDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("team_name")] string TeamName,
    [property: JsonPropertyName("team_display_name")] string TeamDisplayName,
    [property: JsonPropertyName("info")] string Info,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("background_image_url")] string BackgroundImageUrl,
    [property: JsonPropertyName("banner")] string Banner,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("updated_at")] string UpdatedAt);

internal sealed record ChannelTeamDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("team_name")] string TeamName,
    [property: JsonPropertyName("team_display_name")] string TeamDisplayName,
    [property: JsonPropertyName("info")] string Info,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("background_image_url")] string BackgroundImageUrl,
    [property: JsonPropertyName("banner")] string Banner,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("updated_at")] string UpdatedAt,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName);

// ── Mappings (file-scoped) ────────────────────────────────

static file class TeamsMappings
{
    public static Team ToModel(this TeamDto dto) => new(
        dto.Id, dto.TeamName, dto.TeamDisplayName, dto.Info,
        dto.ThumbnailUrl, dto.BackgroundImageUrl, dto.Banner,
        dto.CreatedAt, dto.UpdatedAt);

    public static ChannelTeam ToModel(this ChannelTeamDto dto) => new(
        dto.Id, dto.TeamName, dto.TeamDisplayName, dto.Info,
        dto.ThumbnailUrl, dto.BackgroundImageUrl, dto.Banner,
        dto.CreatedAt, dto.UpdatedAt,
        dto.BroadcasterId, dto.BroadcasterLogin, dto.BroadcasterName);
}
