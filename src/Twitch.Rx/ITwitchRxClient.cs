using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;
using Twitch.Rx.Helix;

namespace Twitch.Rx;

public interface ITwitchRxClient : IAsyncDisposable
{
    ITwitchAuth Auth { get; }
    ITwitchHelixApi Helix { get; }
    ITwitchEventSub EventSub { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
