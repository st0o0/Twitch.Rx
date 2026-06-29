using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Games;

// ── Public Interface ──────────────────────────────────────

public interface IGamesEndpoint
{
    Task<Page<Game>> GetTopGamesAsync(string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<Game> GetAllTopGamesAsync(CancellationToken ct = default);
    Task<Game?> GetByIdAsync(string gameId, CancellationToken ct = default);
    Task<Game?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<string> gameIds, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Game(
    string Id,
    string Name,
    string BoxArtUrl,
    string IgdbId);

// ── Implementation ────────────────────────────────────────

internal sealed class GamesEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IGamesEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public Task<Page<Game>> GetTopGamesAsync(string? cursor = null, CancellationToken ct = default)
        => GetPageAsync("/helix/games/top", cursor, Ctx.HelixPaginatedResponseGameDto, ct, GameMappings.ToModel);

    public IAsyncEnumerable<Game> GetAllTopGamesAsync(CancellationToken ct = default)
        => GetAllPagesAsync("/helix/games/top", Ctx.HelixPaginatedResponseGameDto, ct, GameMappings.ToModel);

    public async Task<Game?> GetByIdAsync(string gameId, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/games?id={Uri.EscapeDataString(gameId)}",
            Ctx.HelixResponseGameDto, ct))?.ToModel();

    public async Task<Game?> GetByNameAsync(string name, CancellationToken ct = default)
        => (await GetFirstAsync($"/helix/games?name={Uri.EscapeDataString(name)}",
            Ctx.HelixResponseGameDto, ct))?.ToModel();

    public async Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<string> gameIds, CancellationToken ct = default)
    {
        var query = string.Join("&", gameIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
        var dtos = await GetListAsync($"/helix/games?{query}", Ctx.HelixResponseGameDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record GameDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("box_art_url")] string BoxArtUrl,
    [property: JsonPropertyName("igdb_id")] string IgdbId);

// ── Mappings (file-scoped) ────────────────────────────────

static file class GameMappings
{
    public static Game ToModel(this GameDto dto) => new(dto.Id, dto.Name, dto.BoxArtUrl, dto.IgdbId);
}
