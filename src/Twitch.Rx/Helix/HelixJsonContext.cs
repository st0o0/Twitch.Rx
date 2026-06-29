using System.Text.Json.Serialization;
using Twitch.Rx.Helix.Users;

namespace Twitch.Rx.Helix;

internal sealed record HelixErrorDto(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string Message);

[JsonSerializable(typeof(HelixErrorDto))]
[JsonSerializable(typeof(HelixResponse<UserDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<UserDto>))]
[JsonSerializable(typeof(UpdateUserDto))]
internal partial class HelixJsonContext : JsonSerializerContext;
