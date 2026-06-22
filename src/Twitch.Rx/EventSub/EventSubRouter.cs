using System.Text.Json;
using R3;
using Twitch.Rx.EventSub.Events;
using Twitch.Rx.EventSub.Json;

namespace Twitch.Rx.EventSub;

internal sealed class EventSubRouter : IDisposable
{
    private static readonly HashSet<string> KnownTypes =
    [
        EventSubType.ChannelFollow, EventSubType.StreamOnline, EventSubType.StreamOffline,
        EventSubType.ChatMessage, EventSubType.ChannelSubscribe,
        EventSubType.ChannelRaid, EventSubType.ChannelPointsRedemption
    ];

    private readonly Subject<EventSubError> _errors = new();
    private readonly IDisposable _connection;

    public Observable<ChannelFollowEvent> ChannelFollow { get; }
    public Observable<StreamOnlineEvent> StreamOnline { get; }
    public Observable<StreamOfflineEvent> StreamOffline { get; }
    public Observable<ChatMessageEvent> ChatMessage { get; }
    public Observable<ChannelSubscribeEvent> ChannelSubscribe { get; }
    public Observable<ChannelRaidEvent> ChannelRaid { get; }
    public Observable<ChannelPointsRedemptionEvent> ChannelPointsRedemption { get; }
    public Observable<RawEventSubNotification> RawNotifications { get; }
    public Observable<EventSubError> Errors => _errors;

    public EventSubRouter(Observable<EventSubEnvelope> messages)
    {
        var notifications = messages
            .Where(e => e.Metadata.MessageType == "notification"
                        && e.Metadata.SubscriptionType is not null
                        && e.Payload.Event is not null)
            .Share();

        _connection = notifications.Subscribe(_ => { });

        ChannelFollow = Route<ChannelFollowEvent>(notifications, EventSubType.ChannelFollow);
        StreamOnline = Route<StreamOnlineEvent>(notifications, EventSubType.StreamOnline);
        StreamOffline = Route<StreamOfflineEvent>(notifications, EventSubType.StreamOffline);
        ChatMessage = Route<ChatMessageEvent>(notifications, EventSubType.ChatMessage);
        ChannelSubscribe = Route<ChannelSubscribeEvent>(notifications, EventSubType.ChannelSubscribe);
        ChannelRaid = Route<ChannelRaidEvent>(notifications, EventSubType.ChannelRaid);
        ChannelPointsRedemption =
            Route<ChannelPointsRedemptionEvent>(notifications, EventSubType.ChannelPointsRedemption);

        RawNotifications = notifications
            .Where(e => !KnownTypes.Contains(e.Metadata.SubscriptionType!))
            .Select(e => new RawEventSubNotification(
                e.Metadata.SubscriptionType!,
                e.Metadata.SubscriptionVersion ?? "1",
                e.Payload.Event!.Value))
            .Share();
    }

    private Observable<T> Route<T>(Observable<EventSubEnvelope> notifications, string type)
        where T : class
    {
        return notifications
            .Where(e => e.Metadata.SubscriptionType == type)
            .Select(e =>
            {
                try
                {
                    return e.Payload.Event!.Value.Deserialize<T>();
                }
                catch (JsonException ex)
                {
                    _errors.OnNext(new EventSubError($"Failed to deserialize {type}", ex));
                    return null;
                }
            })
            .Where(e => e is not null)
            .Select(e => e!)
            .Share();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _errors.Dispose();
    }
}