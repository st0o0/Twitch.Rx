using Twitch.Rx.Api.Models;

namespace Twitch.Rx.Api.Endpoints;

public interface IUsersEndpoint
{
    Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<User?> GetByLoginAsync(string login, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default);
}
