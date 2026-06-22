using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
    Console.WriteLine("Set these environment variables in a .env file:");
    Console.WriteLine();
    Console.WriteLine("  TWITCH_CLIENT_ID       - Your Twitch App Client ID");
    Console.WriteLine("  TWITCH_CLIENT_SECRET   - Your Twitch App Client Secret");
    Console.WriteLine();
    Console.WriteLine("1. Go to https://dev.twitch.tv/console/apps");
    Console.WriteLine("2. Create an app (set Redirect URL to http://localhost)");
    Console.WriteLine("3. Copy Client ID + generate Client Secret into .env");
    Console.WriteLine();
    Console.WriteLine("Then: dotnet run --project src/Twitch.Rx.Examples");
    return;
}

// --- Step 1: API test with client credentials ---
await using var apiClient = TwitchRx.CreateBuilder(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
}).Build();

Console.WriteLine("=== Twitch.Rx Example ===");
Console.WriteLine();
Console.WriteLine("[API] Authenticating with client credentials...");
try
{
    var validation = await apiClient.Auth.ValidateAsync();
    Console.WriteLine($"[API] Authenticated as: {validation.ClientId}");
}
catch (Exception ex)
{
    Console.WriteLine($"[API] Auth failed: {ex.Message}");
    Console.WriteLine("Check TWITCH_CLIENT_ID and TWITCH_CLIENT_SECRET in your .env file.");
    return;
}
Console.WriteLine();

Console.WriteLine("[API] Looking up user 'st0o0'...");
var user = await apiClient.Api.Users.GetByLoginAsync("st0o0");
if (user is not null)
    Console.WriteLine($"[API] Found: {user.DisplayName} (ID: {user.Id}, Type: {user.BroadcasterType})");
Console.WriteLine();

// --- Step 2: EventSub (needs user token) ---
if (string.IsNullOrEmpty(broadcasterId))
{
    if (user is not null)
    {
        Console.WriteLine($"[INFO] Add this to your .env to enable EventSub:");
        Console.WriteLine($"  TWITCH_BROADCASTER_ID={user.Id}");
    }
    Console.WriteLine("[INFO] Done! Re-run with TWITCH_BROADCASTER_ID set to enable EventSub.");
    return;
}

// Acquire user token via Device Code Flow if not provided
if (string.IsNullOrEmpty(accessToken))
{
    Console.WriteLine("[AUTH] No TWITCH_ACCESS_TOKEN set — starting Device Code Flow...");
    Console.WriteLine();
    accessToken = await AcquireUserTokenViaDeviceCodeAsync(clientId, clientSecret);
    if (accessToken is null)
    {
        Console.WriteLine("[AUTH] Device Code Flow failed or timed out.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine($"[TIP] Add this to your .env to skip this step next time:");
    Console.WriteLine($"  TWITCH_ACCESS_TOKEN={accessToken}");
    Console.WriteLine();
}

// Build EventSub client with user token
await using var eventSubClient = TwitchRx.CreateBuilder(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.AccessToken = accessToken;
    options.EventSub.Enabled = true;
    options.EventSub.AutoReconnect = true;
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.StreamOnline, "1",
        new() { ["broadcaster_user_id"] = broadcasterId }));
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.StreamOffline, "1",
        new() { ["broadcaster_user_id"] = broadcasterId }));
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.ChatMessage, "1",
        new() { ["broadcaster_user_id"] = broadcasterId, ["user_id"] = broadcasterId }));
}).Build();

Console.WriteLine("[EventSub] Subscribing to events...");

using var onState = eventSubClient.EventSub.ConnectionState.Subscribe(state =>
    Console.WriteLine($"  [STATE] {state}"));

using var onStreamOnline = eventSubClient.EventSub.StreamOnline.Subscribe(e =>
    Console.WriteLine($"  [LIVE] {e.BroadcasterUserName} went live ({e.Type})"));

using var onStreamOffline = eventSubClient.EventSub.StreamOffline.Subscribe(e =>
    Console.WriteLine($"  [OFFLINE] {e.BroadcasterUserName} went offline"));

using var onChatMessage = eventSubClient.EventSub.ChatMessage.Subscribe(e =>
    Console.WriteLine($"  [CHAT] {e.ChatterUserName}: {e.Message.Text}"));

using var onError = eventSubClient.EventSub.Errors.Subscribe(e =>
    Console.WriteLine($"  [ERROR] {e.Message}: {e.Exception?.Message}"));

using var onRaw = eventSubClient.EventSub.RawNotifications.Subscribe(e =>
    Console.WriteLine($"  [RAW] {e.SubscriptionType}"));

Console.WriteLine("[EventSub] Connecting...");
try
{
    await eventSubClient.ConnectAsync();
    Console.WriteLine("[EventSub] Connected! Listening for events...");
    Console.WriteLine();
    Console.WriteLine("Press Enter to disconnect.");
    Console.ReadLine();
    await eventSubClient.DisconnectAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[EventSub] Connection failed: {ex.Message}");
}

Console.WriteLine("Disconnected. Bye!");
return;

// ── Device Code Flow ──────────────────────────────────────────────

static async Task<string?> AcquireUserTokenViaDeviceCodeAsync(string clientId, string clientSecret)
{
    using var http = new HttpClient();

    var deviceResponse = await http.PostAsync("https://id.twitch.tv/oauth2/device",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scopes"] = "user:read:chat channel:read:subscriptions moderator:read:followers"
        }));

    if (!deviceResponse.IsSuccessStatusCode)
    {
        var err = await deviceResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[AUTH] Device code request failed: {err}");
        return null;
    }

    var device = await deviceResponse.Content.ReadFromJsonAsync(DeviceJsonContext.Default.DeviceCodeResponse);
    if (device is null) return null;

    Console.WriteLine("┌─────────────────────────────────────────────┐");
    Console.WriteLine("│  Go to: https://www.twitch.tv/activate      │");
    Console.WriteLine($"│  Enter code: {device.UserCode,-31}│");
    Console.WriteLine("└─────────────────────────────────────────────┘");
    Console.WriteLine();
    Console.WriteLine($"Waiting for authorization (expires in {device.ExpiresIn}s)...");

    var interval = Math.Max(device.Interval, 5);
    var deadline = DateTime.UtcNow.AddSeconds(device.ExpiresIn);

    while (DateTime.UtcNow < deadline)
    {
        await Task.Delay(TimeSpan.FromSeconds(interval));

        var tokenResponse = await http.PostAsync("https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["device_code"] = device.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            }));

        if (tokenResponse.IsSuccessStatusCode)
        {
            var token = await tokenResponse.Content.ReadFromJsonAsync(DeviceJsonContext.Default.TokenResponse);
            if (token is not null)
            {
                Console.WriteLine("[AUTH] Authorization successful!");
                return token.AccessToken;
            }
        }

        var body = await tokenResponse.Content.ReadAsStringAsync();
        if (body.Contains("authorization_pending"))
        {
            Console.Write(".");
            continue;
        }

        Console.WriteLine($"\n[AUTH] Token request failed: {body}");
        return null;
    }

    Console.WriteLine("\n[AUTH] Timed out waiting for authorization.");
    return null;
}

// ── .env loader ──────────────────────────────────────────────

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

// ── JSON DTOs for Device Code Flow ──────────────────────────

internal sealed record DeviceCodeResponse(
    [property: JsonPropertyName("device_code")] string DeviceCode,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("interval")] int Interval,
    [property: JsonPropertyName("user_code")] string UserCode,
    [property: JsonPropertyName("verification_uri")] string VerificationUri);

internal sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType);

[JsonSerializable(typeof(DeviceCodeResponse))]
[JsonSerializable(typeof(TokenResponse))]
internal partial class DeviceJsonContext : JsonSerializerContext;
