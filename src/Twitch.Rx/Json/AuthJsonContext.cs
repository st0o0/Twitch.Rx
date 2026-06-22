using System.Text.Json.Serialization;

namespace Twitch.Rx.Json;

[JsonSerializable(typeof(TwitchTokenResponse))]
[JsonSerializable(typeof(TwitchValidationResponse))]
internal partial class AuthJsonContext : JsonSerializerContext;
