using R3;
using Twitch.Rx;
using Twitch.Rx.EventSub;

LoadEnvFile();

var clientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("TWITCH_CLIENT_SECRET");
var accessToken = Environment.GetEnvironmentVariable("TWITCH_ACCESS_TOKEN");
var broadcasterId = Environment.GetEnvironmentVariable("TWITCH_BROADCASTER_ID");

if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
{
    Console.WriteLine("Twitch.Rx Example");
    Console.WriteLine("=================");
    Console.WriteLine();
    Console.WriteLine("Set these environment variables before running:");
    Console.WriteLine();
    Console.WriteLine("  TWITCH_CLIENT_ID       - Your Twitch App Client ID");
    Console.WriteLine("  TWITCH_CLIENT_SECRET   - Your Twitch App Client Secret");
    Console.WriteLine("  TWITCH_ACCESS_TOKEN    - (Optional) User Access Token for EventSub");
    Console.WriteLine("  TWITCH_BROADCASTER_ID  - (Optional) Broadcaster User ID for EventSub");
    Console.WriteLine();
    Console.WriteLine("1. Go to https://dev.twitch.tv/console/apps");
    Console.WriteLine("2. Create an app or use an existing one");
    Console.WriteLine("3. Copy the Client ID and generate a Client Secret");
    Console.WriteLine();
    Console.WriteLine("Quick test (API only, no EventSub):");
    Console.WriteLine("  $env:TWITCH_CLIENT_ID = 'your-id'");
    Console.WriteLine("  $env:TWITCH_CLIENT_SECRET = 'your-secret'");
    Console.WriteLine("  dotnet run --project src/Twitch.Rx.Examples");
    Console.WriteLine();
    Console.WriteLine("Full test (API + EventSub):");
    Console.WriteLine("  Also set TWITCH_ACCESS_TOKEN and TWITCH_BROADCASTER_ID");
    Console.WriteLine("  Generate a token at https://twitchtokengenerator.com");
    return;
}

var enableEventSub = !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(broadcasterId);

await using var client = TwitchRx.CreateBuilder(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;

    if (enableEventSub)
    {
        options.AccessToken = accessToken;
        options.EventSub.Enabled = true;
        options.EventSub.AutoReconnect = true;
        options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
            EventSubType.StreamOnline, "1",
            new() { ["broadcaster_user_id"] = broadcasterId! }));
        options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
            EventSubType.StreamOffline, "1",
            new() { ["broadcaster_user_id"] = broadcasterId! }));
        options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
            EventSubType.ChatMessage, "1",
            new() { ["broadcaster_user_id"] = broadcasterId!, ["user_id"] = broadcasterId! }));
    }
}).Build();

Console.WriteLine("=== Twitch.Rx Example ===");
Console.WriteLine();

// --- API Test ---
Console.WriteLine("[API] Authenticating with client credentials...");
var validation = await client.Auth.ValidateAsync();
Console.WriteLine($"[API] Authenticated as app: {validation.ClientId}");
Console.WriteLine();

Console.WriteLine("[API] Looking up user 'twitchdev'...");
var user = await client.Api.Users.GetByLoginAsync("twitchdev");
if (user is not null)
{
    Console.WriteLine($"[API] Found: {user.DisplayName} (ID: {user.Id}, Type: {user.BroadcasterType})");
}
else
{
    Console.WriteLine("[API] User not found.");
}
Console.WriteLine();

if (!enableEventSub)
{
    Console.WriteLine("[INFO] EventSub disabled — set TWITCH_ACCESS_TOKEN and TWITCH_BROADCASTER_ID to enable.");
    Console.WriteLine("[INFO] Done!");
    return;
}

// --- EventSub Test ---
Console.WriteLine("[EventSub] Subscribing to events...");

using var onState = client.EventSub.ConnectionState.Subscribe(state =>
    Console.WriteLine($"  [STATE] {state}"));

using var onStreamOnline = client.EventSub.StreamOnline.Subscribe(e =>
    Console.WriteLine($"  [LIVE] {e.BroadcasterUserName} went live ({e.Type})"));

using var onStreamOffline = client.EventSub.StreamOffline.Subscribe(e =>
    Console.WriteLine($"  [OFFLINE] {e.BroadcasterUserName} went offline"));

using var onChatMessage = client.EventSub.ChatMessage.Subscribe(e =>
    Console.WriteLine($"  [CHAT] {e.ChatterUserName}: {e.Message.Text}"));

using var onError = client.EventSub.Errors.Subscribe(e =>
    Console.WriteLine($"  [ERROR] {e.Message}: {e.Exception?.Message}"));

using var onRaw = client.EventSub.RawNotifications.Subscribe(e =>
    Console.WriteLine($"  [RAW] {e.SubscriptionType}"));

Console.WriteLine("[EventSub] Connecting...");
try
{
    await client.ConnectAsync();
    Console.WriteLine("[EventSub] Connected! Listening for events...");
    Console.WriteLine();
    Console.WriteLine("Press Enter to disconnect.");
    Console.ReadLine();
    await client.DisconnectAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[EventSub] Connection failed: {ex.Message}");
}

Console.WriteLine("Disconnected. Bye!");

static void LoadEnvFile()
{
    foreach (var path in new[] { ".env", "../.env", "../../.env", "../../../.env" })
    {
        if (!File.Exists(path)) continue;
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var sep = trimmed.IndexOf('=');
            if (sep <= 0) continue;
            var key = trimmed[..sep].Trim();
            var value = trimmed[(sep + 1)..].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
        Console.WriteLine($"[ENV] Loaded {Path.GetFullPath(path)}");
        return;
    }
}
