using Microsoft.Extensions.DependencyInjection;
using R3;
using Twitch.Rx.EventSub;
using Twitch.Rx.Hosting;

namespace Twitch.Rx.Examples;

public static class DiExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Twitch.Rx DI Example ===");
        Console.WriteLine();

        var services = new ServiceCollection();

        services.AddTwitchRx(options =>
        {
            options.ClientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID") ?? "your-client-id";
            options.ClientSecret = Environment.GetEnvironmentVariable("TWITCH_CLIENT_SECRET") ?? "your-client-secret";

            options.EventSub.Enabled = true;
            options.EventSub.Subscriptions.Add(new EventSubSubscriptionConfig(
                EventSubType.StreamOnline, "1",
                new() { ["broadcaster_user_id"] = "12345" }));
        });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITwitchRxClient>();

        using var sub = client.EventSub.StreamOnline.Subscribe(e =>
            Console.WriteLine($"[LIVE] {e.BroadcasterUserName} went live"));

        await client.ConnectAsync();

        Console.WriteLine("DI example running. Press Enter to stop.");
        Console.ReadLine();

        await client.DisconnectAsync();
    }
}
