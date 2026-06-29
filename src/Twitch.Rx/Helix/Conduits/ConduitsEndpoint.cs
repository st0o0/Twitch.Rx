using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Conduits;

// ── Public Interface ──────────────────────────────────────

public interface IConduitsEndpoint
{
    Task<IReadOnlyList<Conduit>> GetAsync(CancellationToken ct = default);
    Task<Conduit> CreateAsync(int shardCount, CancellationToken ct = default);
    Task<Conduit> UpdateAsync(string conduitId, int shardCount, CancellationToken ct = default);
    Task DeleteAsync(string conduitId, CancellationToken ct = default);
    Task<Page<ConduitShard>> GetShardsAsync(string conduitId, string? cursor = null, CancellationToken ct = default);
    Task<IReadOnlyList<ConduitShard>> UpdateShardsAsync(string conduitId, IEnumerable<UpdateShardRequest> shards, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record Conduit(string Id, int ShardCount);

public sealed record ConduitShard(string Id, string Status, ConduitShardTransport Transport);

public sealed record ConduitShardTransport(string Method, string? Callback, string? SessionId);

public sealed record UpdateShardRequest(
    string Id,
    string Method,
    string? SessionId = null,
    string? Callback = null,
    string? Secret = null);

// ── Implementation ────────────────────────────────────────

internal sealed class ConduitsEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IConduitsEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<IReadOnlyList<Conduit>> GetAsync(CancellationToken ct = default)
    {
        var dtos = await GetListAsync("/helix/eventsub/conduits", Ctx.HelixResponseConduitDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }

    public async Task<Conduit> CreateAsync(int shardCount, CancellationToken ct = default)
    {
        var dto = await PostAsync("/helix/eventsub/conduits",
            new CreateConduitDto(shardCount),
            Ctx.CreateConduitDto,
            Ctx.HelixResponseConduitDto,
            ct);
        return dto.ToModel();
    }

    public async Task<Conduit> UpdateAsync(string conduitId, int shardCount, CancellationToken ct = default)
    {
        var dto = await PatchAsync("/helix/eventsub/conduits",
            new UpdateConduitDto(conduitId, shardCount),
            Ctx.UpdateConduitDto,
            Ctx.HelixResponseConduitDto,
            ct);
        return dto.ToModel();
    }

    public new async Task DeleteAsync(string conduitId, CancellationToken ct = default)
        => await base.DeleteAsync($"/helix/eventsub/conduits?id={Uri.EscapeDataString(conduitId)}", ct);

    public Task<Page<ConduitShard>> GetShardsAsync(string conduitId, string? cursor = null, CancellationToken ct = default)
        => GetPageAsync($"/helix/eventsub/conduits/shards?conduit_id={Uri.EscapeDataString(conduitId)}",
            cursor, Ctx.HelixPaginatedResponseConduitShardDto, ct, ConduitMappings.ToModel);

    public async Task<IReadOnlyList<ConduitShard>> UpdateShardsAsync(string conduitId, IEnumerable<UpdateShardRequest> shards, CancellationToken ct = default)
    {
        var dto = new UpdateShardsRequestDto(conduitId, shards.Select(s => s.ToDto()).ToArray());
        var dtos = await PatchListAsync("/helix/eventsub/conduits/shards",
            dto, Ctx.UpdateShardsRequestDto, Ctx.HelixResponseConduitShardDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ConduitDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shard_count")] int ShardCount);

internal sealed record CreateConduitDto(
    [property: JsonPropertyName("shard_count")] int ShardCount);

internal sealed record UpdateConduitDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("shard_count")] int ShardCount);

internal sealed record ConduitShardDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("transport")] ConduitShardTransportDto Transport);

internal sealed record ConduitShardTransportDto(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("callback")] string? Callback,
    [property: JsonPropertyName("session_id")] string? SessionId);

internal sealed record UpdateShardsRequestDto(
    [property: JsonPropertyName("conduit_id")] string ConduitId,
    [property: JsonPropertyName("shards")] UpdateShardDto[] Shards);

internal sealed record UpdateShardDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("transport")] UpdateShardTransportDto Transport);

internal sealed record UpdateShardTransportDto(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("session_id")] string? SessionId,
    [property: JsonPropertyName("callback")] string? Callback,
    [property: JsonPropertyName("secret")] string? Secret);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ConduitMappings
{
    public static Conduit ToModel(this ConduitDto dto) => new(dto.Id, dto.ShardCount);

    public static ConduitShard ToModel(this ConduitShardDto dto) => new(
        dto.Id, dto.Status, dto.Transport.ToModel());

    private static ConduitShardTransport ToModel(this ConduitShardTransportDto dto) => new(
        dto.Method, dto.Callback, dto.SessionId);

    public static UpdateShardDto ToDto(this UpdateShardRequest req) => new(
        req.Id,
        new UpdateShardTransportDto(req.Method, req.SessionId, req.Callback, req.Secret));
}
