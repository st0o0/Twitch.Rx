using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;
using Twitch.Rx.Helix;

namespace Twitch.Rx;

internal sealed class TwitchRxClient(
    ITwitchAuth auth,
    ITwitchHelixApi helix,
    ITwitchEventSub eventSub,
    HttpClient[] ownedHttpClients) : ITwitchRxClient
{
    public ITwitchAuth Auth => auth;
    public ITwitchHelixApi Helix => helix;
    public ITwitchEventSub EventSub => eventSub;

    public Task ConnectAsync(CancellationToken ct = default) => eventSub.ConnectAsync(ct);
    public Task DisconnectAsync(CancellationToken ct = default) => eventSub.DisconnectAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await eventSub.DisposeAsync();
        await auth.DisposeAsync();
        foreach (var client in ownedHttpClients)
        {
            client.Dispose();
        }
    }
}
