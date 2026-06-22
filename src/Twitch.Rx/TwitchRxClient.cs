using Twitch.Rx.Api;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;

namespace Twitch.Rx;

internal sealed class TwitchRxClient(
    ITwitchAuth auth,
    ITwitchApi api,
    ITwitchEventSub eventSub,
    HttpClient[] ownedHttpClients) : ITwitchRxClient
{
    public ITwitchAuth Auth => auth;
    public ITwitchApi Api => api;
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
