using R3;
using WebSocket.Rx;
using Twitch.Rx.EventSub.Events;

namespace Twitch.Rx.EventSub;

internal sealed class TwitchEventSub : ITwitchEventSub
{
    private readonly EventSubTransport _transport;
    private readonly EventSubRouter _router;
    private readonly EventSubSubscriptionManager _subscriptionManager;
    private readonly Subject<EventSubError> _errors = new();

    public Observable<EventSubConnectionState> ConnectionState => _transport.ConnectionState;
    public Observable<ChannelFollowEvent> ChannelFollow => _router.ChannelFollow;
    public Observable<StreamOnlineEvent> StreamOnline => _router.StreamOnline;
    public Observable<StreamOfflineEvent> StreamOffline => _router.StreamOffline;
    public Observable<ChatMessageEvent> ChatMessage => _router.ChatMessage;
    public Observable<ChannelSubscribeEvent> ChannelSubscribe => _router.ChannelSubscribe;
    public Observable<ChannelRaidEvent> ChannelRaid => _router.ChannelRaid;
    public Observable<ChannelPointsRedemptionEvent> ChannelPointsRedemption => _router.ChannelPointsRedemption;
    public Observable<PollBeginEvent> PollBegin => _router.PollBegin;
    public Observable<PollProgressEvent> PollProgress => _router.PollProgress;
    public Observable<PollEndEvent> PollEnd => _router.PollEnd;
    public Observable<RawEventSubNotification> RawNotifications => _router.RawNotifications;
    public Observable<EventSubError> Errors => Observable.Merge(_router.Errors, _errors);

    public TwitchEventSub(TwitchEventSubOptions options, HttpClient apiHttpClient, string clientId)
    {
        var wsClient = new ReactiveWebSocketClient(options.WebSocketUrl);
        _transport = new EventSubTransport(wsClient, options);
        _router = new EventSubRouter(_transport.Messages);
        _subscriptionManager = new EventSubSubscriptionManager(apiHttpClient, options.Subscriptions);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        var sessionTcs = new TaskCompletionSource<string>();
        using var sub = _transport.SessionId.Subscribe(id => sessionTcs.TrySetResult(id));

        await _transport.ConnectAsync(ct);

        var sessionId = await sessionTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        await _subscriptionManager.CreateSubscriptionsAsync(sessionId, _errors, ct);
    }

    public Task DisconnectAsync(CancellationToken ct = default) =>
        _transport.DisconnectAsync(ct);

    public async ValueTask DisposeAsync()
    {
        _router.Dispose();
        _errors.Dispose();
        await _transport.DisposeAsync();
    }
}
