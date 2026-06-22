<div align="center">
  <img src="assets/logo.svg" alt="Twitch.Rx Logo" width="128" height="128" />

  <h1>Twitch.Rx</h1>
  <p><strong>Reactive Twitch integration for .NET — EventSub, Helix API, and OAuth2 powered by R3</strong></p>

  [![NuGet](https://img.shields.io/nuget/v/Twitch.Rx?style=flat-square)](https://www.nuget.org/packages/Twitch.Rx)
  [![License](https://img.shields.io/github/license/st0o0/Twitch.Rx?style=flat-square)](LICENSE.md)
  [![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com)
  [![CI](https://img.shields.io/github/actions/workflow/status/st0o0/Twitch.Rx/ci.yml?style=flat-square&label=CI)](https://github.com/st0o0/Twitch.Rx/actions)
</div>

---

## 📋 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Core Concepts](#-core-concepts)
- [Advanced Usage](#-advanced-usage)
- [API Reference](#-api-reference)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

### EventSub (WebSocket)
- 🔌 Reactive WebSocket connection via [WebSocket.Rx](https://github.com/st0o0/WebSocket.Rx)
- 📡 Typed `Observable<T>` streams for all event types (follows, subs, raids, chat, channel points)
- 🔄 Automatic reconnection with `session_reconnect` handling
- 💓 Keepalive timeout detection with configurable watchdog
- 📊 `ConnectionState` observable for monitoring connection lifecycle
- 🔗 Raw notification stream for unsupported event types

### Helix REST API
- 👤 Users endpoint with lookup by ID and login
- 🔐 Automatic Bearer token + Client-Id injection via `DelegatingHandler`
- 🔁 Automatic token refresh on 401 and rate-limit handling on 429
- 🏭 `IHttpClientFactory` support for DI, injectable `HttpClient` for standalone

### Authentication
- 🎫 Client Credentials flow (app access tokens)
- 🔄 Token refresh flow (user access tokens)
- 💾 Pluggable `ITokenStore` (in-memory default, bring your own)
- ⚡ `ValueTask` on hot paths — zero allocation for cached tokens
- 📢 `TokenChanged` and `Errors` observables for monitoring

### General
- ⚙️ Options pattern with sensible defaults — no hardcoded URLs
- 🏗️ Standalone builder and DI integration (`AddTwitchRx()`)
- 🧊 AOT-compatible via `System.Text.Json` source generators
- 🎯 .NET 10 only — modern APIs, no legacy baggage

---

## 📦 Installation

```bash
dotnet add package Twitch.Rx
```

**Requirements:** .NET 10.0+

---

## 🚀 Quick Start

### Standalone Usage

```csharp
using Twitch.Rx;
using Twitch.Rx.EventSub;

await using var client = TwitchRx.CreateBuilder(options =>
{
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.AccessToken = "your-user-access-token";

    options.EventSub.Enabled = true;
    options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
        EventSubType.ChatMessage, "1",
        new() { ["broadcaster_user_id"] = "12345", ["user_id"] = "12345" }));
}).Build();

// Subscribe to events before connecting
using var sub = client.EventSub.ChatMessage.Subscribe(e =>
    Console.WriteLine($"{e.ChatterUserName}: {e.Message.Text}"));

await client.ConnectAsync();

// Use the Helix API
var user = await client.Api.Users.GetByLoginAsync("twitchdev");
Console.WriteLine($"Found: {user?.DisplayName}");
```

### Dependency Injection

```csharp
using Twitch.Rx.Hosting;

services.AddTwitchRx(options =>
{
    options.ClientId = config["Twitch:ClientId"];
    options.ClientSecret = config["Twitch:ClientSecret"];
    options.EventSub.Enabled = true;
});

// Resolve from DI
var client = serviceProvider.GetRequiredService<ITwitchRxClient>();
await client.ConnectAsync();
```

---

## 🎓 Core Concepts

### Observable Event Streams

All Twitch events are exposed as hot R3 `Observable<T>` streams. Subscribe before connecting to receive all events:

```csharp
client.EventSub.StreamOnline.Subscribe(e =>
    Console.WriteLine($"{e.BroadcasterUserName} went live!"));

client.EventSub.ChannelFollow.Subscribe(e =>
    Console.WriteLine($"{e.UserName} followed!"));

client.EventSub.ChannelPointsRedemption.Subscribe(e =>
    Console.WriteLine($"{e.UserName} redeemed: {e.Status}"));
```

### Connection State

Monitor the WebSocket connection lifecycle:

```csharp
client.EventSub.ConnectionState.Subscribe(state =>
{
    // Disconnected → Connecting → Connected → Reconnecting → ...
    Console.WriteLine($"State: {state}");
});
```

### Error Handling

Errors are surfaced as observables — never silently swallowed:

```csharp
client.EventSub.Errors.Subscribe(error =>
    Console.WriteLine($"EventSub error: {error.Message}"));

client.Auth.Errors.Subscribe(error =>
    Console.WriteLine($"Auth error: {error.Message}"));
```

### Raw Notifications

Receive events that don't have a typed model yet:

```csharp
client.EventSub.RawNotifications.Subscribe(raw =>
    Console.WriteLine($"Untyped event: {raw.SubscriptionType} → {raw.Event}"));
```

---

## 🔧 Advanced Usage

### Custom Configuration

All URLs have sensible defaults but can be overridden:

```csharp
TwitchRx.CreateBuilder(options =>
{
    options.ClientId = "...";
    options.ClientSecret = "...";

    options.Auth.BaseUrl = new Uri("https://id.twitch.tv");
    options.Api.BaseUrl = new Uri("https://api.twitch.tv");
    options.EventSub.WebSocketUrl = new Uri("wss://eventsub.wss.twitch.tv/ws");

    options.EventSub.AutoReconnect = true;
    options.EventSub.KeepaliveTimeout = TimeSpan.FromSeconds(30);
});
```

### Custom Token Store

Implement `ITokenStore` for persistent token storage:

```csharp
public class FileTokenStore : ITokenStore
{
    public ValueTask<AccessToken?> GetAsync(CancellationToken ct = default) { ... }
    public ValueTask SetAsync(AccessToken token, CancellationToken ct = default) { ... }
    public ValueTask ClearAsync(CancellationToken ct = default) { ... }
}

var client = TwitchRx.CreateBuilder(options => { ... })
    .WithTokenStore(new FileTokenStore())
    .Build();
```

### Injecting Your Own HttpClient

Control HTTP behavior (proxies, custom handlers, Polly policies):

```csharp
var client = TwitchRx.CreateBuilder(options => { ... })
    .WithAuthHttpClient(myAuthClient)
    .WithApiHttpClient(myApiClient)
    .Build();
```

### Disabling Modules

Disable what you don't need — no exceptions, just empty streams:

```csharp
TwitchRx.CreateBuilder(options =>
{
    options.ClientId = "...";
    options.ClientSecret = "...";
    options.Api.Enabled = false;      // Disable Helix API
    options.EventSub.Enabled = false; // Disable EventSub (default)
});
```

---

## 📚 API Reference

### ITwitchRxClient

| Property | Type | Description |
|----------|------|-------------|
| `Auth` | `ITwitchAuth` | OAuth2 token management |
| `Api` | `ITwitchApi` | Helix REST API endpoints |
| `EventSub` | `ITwitchEventSub` | EventSub WebSocket event streams |

| Method | Returns | Description |
|--------|---------|-------------|
| `ConnectAsync(ct)` | `Task` | Connect EventSub WebSocket |
| `DisconnectAsync(ct)` | `Task` | Disconnect gracefully |

### ITwitchEventSub

| Observable | Event Type | Description |
|-----------|------------|-------------|
| `ChannelFollow` | `ChannelFollowEvent` | User followed a channel |
| `StreamOnline` | `StreamOnlineEvent` | Stream went live |
| `StreamOffline` | `StreamOfflineEvent` | Stream went offline |
| `ChatMessage` | `ChatMessageEvent` | Chat message received |
| `ChannelSubscribe` | `ChannelSubscribeEvent` | New subscription |
| `ChannelRaid` | `ChannelRaidEvent` | Incoming raid |
| `ChannelPointsRedemption` | `ChannelPointsRedemptionEvent` | Channel points redeemed |
| `RawNotifications` | `RawEventSubNotification` | Untyped event fallback |
| `ConnectionState` | `EventSubConnectionState` | Connection lifecycle |
| `Errors` | `EventSubError` | Error stream |

### ITwitchAuth

| Method | Returns | Description |
|--------|---------|-------------|
| `GetTokenAsync(ct)` | `ValueTask<AccessToken>` | Get cached or acquire token |
| `RefreshTokenAsync(ct)` | `ValueTask<AccessToken>` | Force token refresh |
| `ValidateAsync(ct)` | `ValueTask<TokenValidation>` | Validate current token |

---

## 🤝 Contributing

### How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes
4. Run tests (`dotnet test src/Twitch.Rx.slnx`)
5. Open a Pull Request

### Development Setup

```bash
git clone https://github.com/st0o0/Twitch.Rx.git
cd Twitch.Rx
dotnet build src/Twitch.Rx.slnx
dotnet test src/Twitch.Rx.slnx
```

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE.md](LICENSE.md) file for details.

---

<div align="center">
  <p>Built with ❤️ using <a href="https://github.com/Cysharp/R3">R3</a> and <a href="https://github.com/st0o0/WebSocket.Rx">WebSocket.Rx</a></p>
  <p>
    <a href="https://github.com/st0o0/Twitch.Rx/issues">Report Bug</a> ·
    <a href="https://github.com/st0o0/Twitch.Rx/issues">Request Feature</a>
  </p>
</div>
