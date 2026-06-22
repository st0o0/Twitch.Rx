using R3;
using Twitch.Rx.EventSub.Events;

namespace Twitch.Rx.EventSub;

internal sealed class DisabledTwitchEventSub : ITwitchEventSub
{
    public Observable<EventSubConnectionState> ConnectionState =>
        Observable.Return(EventSubConnectionState.Disconnected);

    public Observable<ChannelFollowEvent> ChannelFollow => Observable.Empty<ChannelFollowEvent>();
    public Observable<StreamOnlineEvent> StreamOnline => Observable.Empty<StreamOnlineEvent>();
    public Observable<StreamOfflineEvent> StreamOffline => Observable.Empty<StreamOfflineEvent>();
    public Observable<ChatMessageEvent> ChatMessage => Observable.Empty<ChatMessageEvent>();
    public Observable<ChannelSubscribeEvent> ChannelSubscribe => Observable.Empty<ChannelSubscribeEvent>();
    public Observable<ChannelRaidEvent> ChannelRaid => Observable.Empty<ChannelRaidEvent>();
    public Observable<ChannelPointsRedemptionEvent> ChannelPointsRedemption => Observable.Empty<ChannelPointsRedemptionEvent>();
    public Observable<RawEventSubNotification> RawNotifications => Observable.Empty<RawEventSubNotification>();
    public Observable<EventSubError> Errors => Observable.Empty<EventSubError>();

    public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
