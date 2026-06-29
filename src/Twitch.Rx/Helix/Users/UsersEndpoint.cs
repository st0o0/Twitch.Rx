using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Users
{
    // ── Public Interface ──────────────────────────────────────

    public interface IUsersEndpoint
    {
        Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
        Task<User?> GetByLoginAsync(string login, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetByLoginsAsync(IEnumerable<string> logins, CancellationToken ct = default);
        Task UpdateAsync(string description, CancellationToken ct = default);
        IAsyncEnumerable<User> GetAllBlockedAsync(string broadcasterId, CancellationToken ct = default);
        Task<Page<User>> GetBlockListAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
        Task BlockAsync(string targetUserId, CancellationToken ct = default);
        Task UnblockAsync(string targetUserId, CancellationToken ct = default);
    }

    // ── Public Models ─────────────────────────────────────────

    public sealed record User(
        string Id, string Login, string DisplayName,
        string BroadcasterType, string Description,
        string ProfileImageUrl, string CreatedAt);

    // ── Implementation ────────────────────────────────────────

    internal sealed class UsersEndpoint : HelixEndpoint, IUsersEndpoint
    {
        private static HelixJsonContext Ctx => HelixJsonContext.Default;
        private readonly HttpClient _http;

        public UsersEndpoint(HttpClient httpClient, Subject<HelixError> errors)
            : base(httpClient, errors)
        {
            _http = httpClient;
        }

        public async Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
            => (await GetFirstAsync($"/helix/users?id={Uri.EscapeDataString(userId)}",
                Ctx.HelixResponseUserDto, ct))?.ToModel();

        public async Task<User?> GetByLoginAsync(string login, CancellationToken ct = default)
            => (await GetFirstAsync($"/helix/users?login={Uri.EscapeDataString(login)}",
                Ctx.HelixResponseUserDto, ct))?.ToModel();

        public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default)
        {
            var query = string.Join("&", userIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
            var dtos = await GetListAsync($"/helix/users?{query}", Ctx.HelixResponseUserDto, ct);
            return dtos.Select(d => d.ToModel()).ToArray();
        }

        public async Task<IReadOnlyList<User>> GetByLoginsAsync(IEnumerable<string> logins, CancellationToken ct = default)
        {
            var query = string.Join("&", logins.Select(l => $"login={Uri.EscapeDataString(l)}"));
            var dtos = await GetListAsync($"/helix/users?{query}", Ctx.HelixResponseUserDto, ct);
            return dtos.Select(d => d.ToModel()).ToArray();
        }

        public async Task UpdateAsync(string description, CancellationToken ct = default)
            => await PatchAsync("/helix/users", new UpdateUserDto(description), Ctx.UpdateUserDto, ct);

        public IAsyncEnumerable<User> GetAllBlockedAsync(string broadcasterId, CancellationToken ct = default)
            => GetAllPagesAsync($"/helix/users/blocks?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
                Ctx.HelixPaginatedResponseUserDto, ct, UserMappings.ToModel);

        public Task<Page<User>> GetBlockListAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
            => GetPageAsync($"/helix/users/blocks?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
                cursor, Ctx.HelixPaginatedResponseUserDto, ct, UserMappings.ToModel);

        public async Task BlockAsync(string targetUserId, CancellationToken ct = default)
        {
            var url = $"/helix/users/blocks?target_user_id={Uri.EscapeDataString(targetUserId)}";
            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            using var response = await _http.SendAsync(request, ct);
        }

        public async Task UnblockAsync(string targetUserId, CancellationToken ct = default)
            => await DeleteAsync($"/helix/users/blocks?target_user_id={Uri.EscapeDataString(targetUserId)}", ct);
    }

    // ── DTOs (internal) ───────────────────────────────────────

    internal sealed record UserDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("broadcaster_type")] string BroadcasterType,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("profile_image_url")] string ProfileImageUrl,
        [property: JsonPropertyName("offline_image_url")] string OfflineImageUrl,
        [property: JsonPropertyName("created_at")] string CreatedAt);

    internal sealed record UpdateUserDto(
        [property: JsonPropertyName("description")] string Description);

    // ── Mappings (file-scoped) ────────────────────────────────

    static file class UserMappings
    {
        public static User ToModel(this UserDto dto) => new(
            dto.Id, dto.Login, dto.DisplayName,
            dto.BroadcasterType, dto.Description,
            dto.ProfileImageUrl, dto.CreatedAt);
    }
}
