using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Subscriptions;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Subscriptions;

public sealed class SubscriptionsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetBroadcasterSubscriptionsAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(SubscriptionsResponse("uid1", "userlogin", "UserName"));

        var page = await endpoint.GetBroadcasterSubscriptionsAsync("bid1", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("uid1", page.Items[0].UserId);
        Assert.Equal("UserName", page.Items[0].UserName);
    }

    [Fact]
    public async Task GetAllBroadcasterSubscriptionsAsync_IteratesAllPages()
    {
        var page1 = PaginatedSubscriptionsResponse("uid1", "cursor123");
        var page2 = PaginatedSubscriptionsResponse("uid2", null);
        var endpoint = CreateEndpoint(page1, page2);

        var subs = new List<Subscription>();
        await foreach (var s in endpoint.GetAllBroadcasterSubscriptionsAsync("bid1", TestContext.Current.CancellationToken))
            subs.Add(s);

        Assert.Equal(2, subs.Count);
        Assert.Equal("uid1", subs[0].UserId);
        Assert.Equal("uid2", subs[1].UserId);
    }

    [Fact]
    public async Task CheckUserSubscriptionAsync_ReturnsSubscription()
    {
        var json = """{"data":[{"broadcaster_id":"bid1","broadcaster_login":"broadlogin","broadcaster_name":"BroadName","is_gift":false,"plan_name":"Channel Subscription","tier":"1000","user_id":"uid1","user_login":"userlogin","user_name":"UserName"}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var sub = await endpoint.CheckUserSubscriptionAsync("bid1", "uid1", TestContext.Current.CancellationToken);

        Assert.NotNull(sub);
        Assert.Equal("uid1", sub!.UserId);
        Assert.Equal("bid1", sub.BroadcasterId);
        Assert.Equal("1000", sub.Tier);
    }

    [Fact]
    public async Task CheckUserSubscriptionAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(JsonResponse("""{"data":[]}"""));

        var sub = await endpoint.CheckUserSubscriptionAsync("bid1", "uid1", TestContext.Current.CancellationToken);

        Assert.Null(sub);
    }

    [Fact]
    public async Task GetBroadcasterSubscriptionsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetBroadcasterSubscriptionsAsync("bid1", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private SubscriptionsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new SubscriptionsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage SubscriptionsResponse(string userId, string userLogin, string userName) =>
        JsonResponse($$"""{"data":[{"broadcaster_id":"bid1","broadcaster_login":"broadlogin","broadcaster_name":"BroadName","gifter_id":"","gifter_login":"","gifter_name":"","is_gift":false,"plan_name":"Channel Subscription","tier":"1000","user_id":"{{userId}}","user_login":"{{userLogin}}","user_name":"{{userName}}"}]}""");

    private static HttpResponseMessage PaginatedSubscriptionsResponse(string userId, string? cursor)
    {
        var paginationJson = cursor is not null
            ? $$"""{"cursor":"{{cursor}}"}"""
            : "{}";
        return JsonResponse(
            $$"""{"data":[{"broadcaster_id":"bid1","broadcaster_login":"broadlogin","broadcaster_name":"BroadName","gifter_id":"","gifter_login":"","gifter_name":"","is_gift":false,"plan_name":"Channel Subscription","tier":"1000","user_id":"{{userId}}","user_login":"userlogin","user_name":"UserName"}],"pagination":""" + paginationJson + "}");
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
