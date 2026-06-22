using System.Text.Json;
using NSubstitute;
using R3;
using WebSocket.Rx;
using Twitch.Rx.EventSub;
using Twitch.Rx.EventSub.Json;
using Xunit;

namespace Twitch.Rx.Tests.EventSub;

public sealed class EventSubTransportTests : IDisposable
{
    private readonly IReactiveWebSocketClient _wsClient = Substitute.For<IReactiveWebSocketClient>();
    private readonly Subject<Message> _messageSubject = new();
    private readonly Subject<Connected> _connectedSubject = new();
    private readonly Subject<Disconnected> _disconnectedSubject = new();
    private readonly Subject<ErrorOccurred> _errorSubject = new();
    private readonly TwitchEventSubOptions _options = new();
    private readonly List<IDisposable> _subs = [];

    public EventSubTransportTests()
    {
        _wsClient.MessageReceived.Returns(_messageSubject);
        _wsClient.ConnectionHappened.Returns(_connectedSubject);
        _wsClient.DisconnectionHappened.Returns(_disconnectedSubject);
        _wsClient.ErrorOccurred.Returns(_errorSubject);
    }

    [Fact]
    public async Task ConnectAsync_StartsWebSocket_EmitsConnecting()
    {
        var states = new List<EventSubConnectionState>();
        var transport = CreateTransport();
        _subs.Add(transport.ConnectionState.Subscribe(s => states.Add(s)));

        await transport.ConnectAsync(TestContext.Current.CancellationToken);

        await _wsClient.Received(1).StartOrFailAsync(Arg.Any<CancellationToken>());
        Assert.Contains(EventSubConnectionState.Connecting, states);
    }

    [Fact]
    public async Task SessionId_EmittedOnWelcome()
    {
        var transport = CreateTransport();
        string? sessionId = null;
        _subs.Add(transport.SessionId.Subscribe(id => sessionId = id));

        await transport.ConnectAsync(TestContext.Current.CancellationToken);
        EmitWelcome("session-abc", 10);

        Assert.Equal("session-abc", sessionId);
    }

    [Fact]
    public async Task ConnectionState_Connected_AfterWelcome()
    {
        var states = new List<EventSubConnectionState>();
        var transport = CreateTransport();
        _subs.Add(transport.ConnectionState.Subscribe(s => states.Add(s)));

        await transport.ConnectAsync(TestContext.Current.CancellationToken);
        EmitWelcome("s1", 10);

        Assert.Contains(EventSubConnectionState.Connected, states);
    }

    [Fact]
    public async Task Messages_EmitsParsedEnvelopes()
    {
        var transport = CreateTransport();
        EventSubEnvelope? received = null;
        _subs.Add(transport.Messages.Subscribe(e => received = e));

        await transport.ConnectAsync(TestContext.Current.CancellationToken);
        EmitKeepalive();

        Assert.NotNull(received);
        Assert.Equal("session_keepalive", received!.Metadata.MessageType);
    }

    private EventSubTransport CreateTransport() => new(_wsClient, _options);

    private void EmitWelcome(string sessionId, int keepalive) =>
        _messageSubject.OnNext(Message.Create(
            $"{{\"metadata\":{{\"message_id\":\"1\",\"message_type\":\"session_welcome\",\"message_timestamp\":\"2023-01-01T00:00:00Z\"}},\"payload\":{{\"session\":{{\"id\":\"{sessionId}\",\"status\":\"connected\",\"keepalive_timeout_seconds\":{keepalive},\"reconnect_url\":null,\"connected_at\":\"2023-01-01T00:00:00Z\"}}}}}}"));

    private void EmitKeepalive() =>
        _messageSubject.OnNext(Message.Create(
            """{"metadata":{"message_id":"1","message_type":"session_keepalive","message_timestamp":"2023-01-01T00:00:00Z"},"payload":{}}"""));

    public void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _messageSubject.Dispose();
        _connectedSubject.Dispose();
        _disconnectedSubject.Dispose();
        _errorSubject.Dispose();
    }
}
