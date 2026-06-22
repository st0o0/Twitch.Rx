using R3;
using Twitch.Rx;
using Twitch.Rx.EventSub;

// ──────────────────────────────────────────────
// Example 1: Standalone usage (no DI)
// ──────────────────────────────────────────────

Console.WriteLine("=== Twitch.Rx Standalone Example ===");
Console.WriteLine();

await using var client = TwitchRx.CreateBuilder(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID") ?? "your-client-id";
    options.ClientSecret = Environment.GetEnvironmentVariable("TWITCH_CLIENT_SECRET") ?? "your-client-secret";

    // All URLs have sensible defaults — override for testing:
    // options.Auth.BaseUrl = new Uri("https://id.twitch.tv");
    // options.Api.BaseUrl = new Uri("https://api.twitch.tv");
    // options.EventSub.WebSocketUrl = new Uri("wss://eventsub.wss.twitch.tv/ws");

    options.Api.Enabled = true;

    options.EventSub.Enabled = true;
    options.EventSub.AutoReconnect = true;
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.StreamOnline, "1",
        new() { ["broadcaster_user_id"] = "12345" }));
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.ChatMessage, "1",
        new() { ["broadcaster_user_id"] = "12345", ["user_id"] = "12345" }));
}).Build();

// Subscribe to events before connecting
using var onStreamOnline = client.EventSub.StreamOnline.Subscribe(e =>
    Console.WriteLine($"[LIVE] {e.BroadcasterUserName} went live ({e.Type})"));

using var onChatMessage = client.EventSub.ChatMessage.Subscribe(e =>
    Console.WriteLine($"[CHAT] {e.ChatterUserName}: {e.Message.Text}"));

using var onFollow = client.EventSub.ChannelFollow.Subscribe(e =>
    Console.WriteLine($"[FOLLOW] {e.UserName} followed {e.BroadcasterUserName}"));

using var onError = client.EventSub.Errors.Subscribe(e =>
    Console.WriteLine($"[ERROR] {e.Message}: {e.Exception?.Message}"));

using var onState = client.EventSub.ConnectionState.Subscribe(state =>
    Console.WriteLine($"[STATE] {state}"));

// Connect — establishes WebSocket + creates subscriptions
Console.WriteLine("Connecting...");
await client.ConnectAsync();

// Use the API
var user = await client.Api.Users.GetByLoginAsync("twitchdev");
if (user is not null)
{
    Console.WriteLine($"Found user: {user.DisplayName} (ID: {user.Id})");
}

Console.WriteLine("Listening for events. Press Enter to disconnect.");
Console.ReadLine();

await client.DisconnectAsync();
Console.WriteLine("Disconnected.");
