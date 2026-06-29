using System.Text.Json.Serialization;

namespace Twitch.Rx.Helix;

internal sealed record HelixErrorDto(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("message")] string Message);

[JsonSerializable(typeof(HelixErrorDto))]
internal partial class HelixJsonContext : JsonSerializerContext;
