using R3;
using Twitch.Rx.EventSub;
using Twitch.Rx.EventSub.Events;
using Xunit;

namespace Twitch.Rx.Tests.EventSub;

public sealed class DisabledTwitchEventSubTests
{
    [Fact]
    public void ConnectionState_ReturnsDisconnected()
    {
        var disabled = new DisabledTwitchEventSub();
        EventSubConnectionState? state = null;
        using var sub = disabled.ConnectionState.Subscribe(s => state = s);

        Assert.Equal(EventSubConnectionState.Disconnected, state);
    }

    [Fact]
    public async Task ConnectAsync_IsNoOp()
    {
        var disabled = new DisabledTwitchEventSub();

        await disabled.ConnectAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ChannelFollow_CompletesImmediately()
    {
        var disabled = new DisabledTwitchEventSub();
        bool completed = false;
        using var sub = disabled.ChannelFollow.Subscribe(
            _ => { },
            _ => { },
            _ => completed = true);

        Assert.True(completed);
    }
}
