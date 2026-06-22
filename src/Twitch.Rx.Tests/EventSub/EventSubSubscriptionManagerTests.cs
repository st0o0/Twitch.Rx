using System.Net;
using R3;
using Twitch.Rx.EventSub;
using Twitch.Rx.EventSub.Events;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.EventSub;

public sealed class EventSubSubscriptionManagerTests
{
    [Fact]
    public async Task CreateSubscriptionsAsync_PostsForEachSubscription()
    {
        var handler = new FakeHttpHandler(
            new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") },
            new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };

        var subs = new List<EventSubSubscriptionConfig>
        {
            new("stream.online", "1", new() { ["broadcaster_user_id"] = "123" }),
            new("stream.offline", "1", new() { ["broadcaster_user_id"] = "123" })
        };

        var manager = new EventSubSubscriptionManager(httpClient, subs);
        var errors = new Subject<EventSubError>();

        await manager.CreateSubscriptionsAsync("session-abc", errors);

        Assert.Equal(2, handler.RequestCount);
        errors.Dispose();
    }

    [Fact]
    public async Task CreateSubscriptionsAsync_EmitsError_OnFailure_ContinuesWithRest()
    {
        var handler = new FakeHttpHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") },
            new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };

        var subs = new List<EventSubSubscriptionConfig>
        {
            new("stream.online", "1", new() { ["broadcaster_user_id"] = "123" }),
            new("stream.offline", "1", new() { ["broadcaster_user_id"] = "123" })
        };

        var manager = new EventSubSubscriptionManager(httpClient, subs);
        var errors = new Subject<EventSubError>();
        var errorList = new List<EventSubError>();
        using var sub = errors.Subscribe(e => errorList.Add(e));

        await manager.CreateSubscriptionsAsync("session-abc", errors);

        Assert.Equal(2, handler.RequestCount);
        Assert.Single(errorList);
        Assert.Contains("stream.online", errorList[0].Message);
        errors.Dispose();
    }
}
