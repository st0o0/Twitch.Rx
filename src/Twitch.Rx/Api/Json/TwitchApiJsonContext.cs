using System.Text.Json.Serialization;

namespace Twitch.Rx.Api.Json;

[JsonSerializable(typeof(TwitchDataResponse<TwitchUserDto>))]
internal partial class TwitchApiJsonContext : JsonSerializerContext;
