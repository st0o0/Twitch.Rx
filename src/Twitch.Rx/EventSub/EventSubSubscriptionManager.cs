using System.Net.Http.Json;
using R3;
using Twitch.Rx.EventSub.Events;
using Twitch.Rx.EventSub.Json;

namespace Twitch.Rx.EventSub;

internal sealed class EventSubSubscriptionManager(
    HttpClient httpClient,
    IReadOnlyList<EventSubSubscriptionConfig> subscriptions)
{
    public async Task CreateSubscriptionsAsync(
        string sessionId, Subject<EventSubError> errors, CancellationToken ct = default)
    {
        foreach (var sub in subscriptions)
        {
            try
            {
                var body = new CreateSubscriptionRequest(
                    sub.Type, sub.Version, sub.Condition,
                    new SubscriptionTransport("websocket", sessionId));

                var request = new HttpRequestMessage(HttpMethod.Post, "/helix/eventsub/subscriptions")
                {
                    Content = JsonContent.Create(body, EventSubJsonContext.Default.CreateSubscriptionRequest)
                };

                var response = await httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                errors.OnNext(new EventSubError($"Failed to create subscription for {sub.Type}", ex));
            }
        }
    }
}
