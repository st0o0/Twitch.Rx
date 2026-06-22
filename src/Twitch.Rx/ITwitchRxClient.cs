using Twitch.Rx.Api;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;

namespace Twitch.Rx;

public interface ITwitchRxClient : IAsyncDisposable
{
    ITwitchAuth Auth { get; }
    ITwitchApi Api { get; }
    ITwitchEventSub EventSub { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
