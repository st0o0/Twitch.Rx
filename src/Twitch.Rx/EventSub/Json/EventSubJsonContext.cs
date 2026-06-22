using System.Text.Json.Serialization;

namespace Twitch.Rx.EventSub.Json;

[JsonSerializable(typeof(EventSubEnvelope))]
[JsonSerializable(typeof(CreateSubscriptionRequest))]
internal partial class EventSubJsonContext : JsonSerializerContext;
