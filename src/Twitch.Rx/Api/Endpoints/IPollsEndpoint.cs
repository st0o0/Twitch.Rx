using Twitch.Rx.Api.Models;

namespace Twitch.Rx.Api.Endpoints;

public interface IPollsEndpoint
{
    Task<Poll> CreateAsync(CreatePollRequest request, CancellationToken ct = default);
    Task EndAsync(string broadcasterId, string pollId, string status = "TERMINATED", CancellationToken ct = default);
}
