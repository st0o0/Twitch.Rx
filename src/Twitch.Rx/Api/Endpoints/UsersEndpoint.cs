using System.Net.Http.Json;
using Twitch.Rx.Api.Json;
using Twitch.Rx.Api.Models;

namespace Twitch.Rx.Api.Endpoints;

internal sealed class UsersEndpoint(HttpClient httpClient) : IUsersEndpoint
{
    public async Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var users = await FetchUsersAsync($"/helix/users?id={Uri.EscapeDataString(userId)}", ct);
        return users.Count > 0 ? users[0] : null;
    }

    public async Task<User?> GetByLoginAsync(string login, CancellationToken ct = default)
    {
        var users = await FetchUsersAsync($"/helix/users?login={Uri.EscapeDataString(login)}", ct);
        return users.Count > 0 ? users[0] : null;
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var query = string.Join("&", userIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
        return await FetchUsersAsync($"/helix/users?{query}", ct);
    }

    private async Task<IReadOnlyList<User>> FetchUsersAsync(string url, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync(
            TwitchApiJsonContext.Default.TwitchDataResponseTwitchUserDto, ct)
            ?? throw new InvalidOperationException("Failed to deserialize users response.");

        return data.Data.Select(d => new User(
            d.Id, d.Login, d.DisplayName, d.BroadcasterType,
            d.Description, d.ProfileImageUrl, d.CreatedAt)).ToArray();
    }
}
