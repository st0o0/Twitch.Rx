using System.Text.Json;
using R3;
using WebSocket.Rx;
using Twitch.Rx.EventSub.Json;

namespace Twitch.Rx.EventSub;

internal sealed class EventSubTransport : IAsyncDisposable
{
    private readonly IReactiveWebSocketClient _wsClient;
    private readonly TwitchEventSubOptions _options;
    private readonly Subject<EventSubEnvelope> _messages = new();
    private readonly Subject<string> _sessionId = new();
    private readonly ReactiveProperty<EventSubConnectionState> _connectionState = new(EventSubConnectionState.Disconnected);
    private IDisposable? _messageSubscription;
    private CancellationTokenSource? _keepaliveCts;

    public Observable<EventSubEnvelope> Messages => _messages;
    public Observable<string> SessionId => _sessionId;
    public Observable<EventSubConnectionState> ConnectionState => _connectionState;

    public EventSubTransport(IReactiveWebSocketClient wsClient, TwitchEventSubOptions options)
    {
        _wsClient = wsClient;
        _options = options;
        _wsClient.IsReconnectionEnabled = options.AutoReconnect;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _connectionState.Value = EventSubConnectionState.Connecting;
        _messageSubscription = _wsClient.MessageReceived.Subscribe(OnMessage);
        await _wsClient.StartOrFailAsync(ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _connectionState.Value = EventSubConnectionState.Disconnected;
        await _wsClient.StopAsync(
            System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
            "Client disconnecting", ct);
    }

    private void OnMessage(Message message)
    {
        if (!message.IsText) return;

        var envelope = JsonSerializer.Deserialize(
            message.Text.Span, EventSubJsonContext.Default.EventSubEnvelope);
        if (envelope is null) return;

        ResetKeepaliveTimer();
        _messages.OnNext(envelope);

        switch (envelope.Metadata.MessageType)
        {
            case "session_welcome" when envelope.Payload.Session is not null:
                _connectionState.Value = EventSubConnectionState.Connected;
                _sessionId.OnNext(envelope.Payload.Session.Id);
                var timeout = _options.KeepaliveTimeout
                    ?? (envelope.Payload.Session.KeepaliveTimeoutSeconds is { } secs
                        ? TimeSpan.FromSeconds(secs + 5)
                        : null);
                if (timeout is not null)
                    StartKeepaliveTimer(timeout.Value);
                break;

            case "session_reconnect" when envelope.Payload.Session?.ReconnectUrl is not null:
                _connectionState.Value = EventSubConnectionState.Reconnecting;
                _ = ReconnectWithErrorHandlingAsync(new Uri(envelope.Payload.Session.ReconnectUrl));
                break;
        }
    }

    private async Task ReconnectWithErrorHandlingAsync(Uri newUrl)
    {
        try
        {
            _wsClient.Url = newUrl;
            await _wsClient.ReconnectOrFailAsync();
        }
        catch (Exception)
        {
            _connectionState.Value = EventSubConnectionState.Disconnected;
        }
    }

    private void StartKeepaliveTimer(TimeSpan timeout)
    {
        _keepaliveCts?.Cancel();
        _keepaliveCts?.Dispose();
        _keepaliveCts = new CancellationTokenSource();
        _ = KeepaliveWatchdogAsync(timeout, _keepaliveCts.Token);
    }

    private void ResetKeepaliveTimer()
    {
        if (_keepaliveCts is not null)
        {
            var timeout = _options.KeepaliveTimeout ?? TimeSpan.FromSeconds(15);
            _keepaliveCts.Cancel();
            _keepaliveCts.Dispose();
            _keepaliveCts = new CancellationTokenSource();
            _ = KeepaliveWatchdogAsync(timeout, _keepaliveCts.Token);
        }
    }

    private async Task KeepaliveWatchdogAsync(TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            await Task.Delay(timeout, ct);
            _connectionState.Value = EventSubConnectionState.Reconnecting;
            await _wsClient.ReconnectOrFailAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            _connectionState.Value = EventSubConnectionState.Disconnected;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _keepaliveCts?.Cancel();
        _keepaliveCts?.Dispose();
        _messageSubscription?.Dispose();
        _messages.Dispose();
        _sessionId.Dispose();
        _connectionState.Dispose();
        await _wsClient.DisposeAsync();
    }
}
