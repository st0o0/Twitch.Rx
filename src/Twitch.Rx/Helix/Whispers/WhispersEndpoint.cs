using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Whispers;

// ── Public Interface ──────────────────────────────────────

public interface IWhispersEndpoint
{
    Task SendAsync(string fromUserId, string toUserId, string message, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────

internal sealed class WhispersEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IWhispersEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task SendAsync(string fromUserId, string toUserId, string message, CancellationToken ct = default)
        => await PostAsync(
            $"/helix/whispers?from_user_id={Uri.EscapeDataString(fromUserId)}&to_user_id={Uri.EscapeDataString(toUserId)}",
            new WhisperDto(message),
            Ctx.WhisperDto,
            ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record WhisperDto(
    [property: JsonPropertyName("message")] string Message);
