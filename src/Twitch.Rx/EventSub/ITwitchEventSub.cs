using R3;
using Twitch.Rx.EventSub.Events;

namespace Twitch.Rx.EventSub;

public interface ITwitchEventSub : IAsyncDisposable
{
    Observable<EventSubConnectionState> ConnectionState { get; }

    Observable<ChannelFollowEvent> ChannelFollow { get; }
    Observable<StreamOnlineEvent> StreamOnline { get; }
    Observable<StreamOfflineEvent> StreamOffline { get; }
    Observable<ChatMessageEvent> ChatMessage { get; }
    Observable<ChannelSubscribeEvent> ChannelSubscribe { get; }
    Observable<ChannelRaidEvent> ChannelRaid { get; }
    Observable<ChannelPointsRedemptionEvent> ChannelPointsRedemption { get; }

    Observable<RawEventSubNotification> RawNotifications { get; }
    Observable<EventSubError> Errors { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
