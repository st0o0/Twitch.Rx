using System.Text.Json.Serialization;

namespace Twitch.Rx.Json;

internal sealed record TwitchTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null,
    [property: JsonPropertyName("scope")] string[]? Scope = null);

internal sealed record TwitchValidationResponse(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("login")] string? Login,
    [property: JsonPropertyName("scopes")] string[] Scopes,
    [property: JsonPropertyName("user_id")] string? UserId,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
