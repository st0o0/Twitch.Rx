using System.Text.Json.Serialization;
using Twitch.Rx.Api.Endpoints;

namespace Twitch.Rx.Api.Json;

[JsonSerializable(typeof(TwitchDataResponse<TwitchUserDto>))]
[JsonSerializable(typeof(CreatePollDto))]
[JsonSerializable(typeof(EndPollDto))]
[JsonSerializable(typeof(TwitchDataResponse<PollDto>))]
[JsonSerializable(typeof(SendChatMessageDto))]
internal partial class TwitchApiJsonContext : JsonSerializerContext;
