using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Goals;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Goals;

public sealed class GoalsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetAsync_ReturnsGoals()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"goal-1","broadcaster_id":"123","broadcaster_name":"Broadcaster","broadcaster_login":"broadcaster","type":"follower","description":"Reach 1000","current_amount":500,"target_amount":1000,"created_at":"2024-01-01T00:00:00Z"}]}"""));

        var goals = await endpoint.GetAsync("123", TestContext.Current.CancellationToken);

        Assert.Single(goals);
        Assert.Equal("goal-1", goals[0].Id);
        Assert.Equal("follower", goals[0].Type);
        Assert.Equal(500, goals[0].CurrentAmount);
        Assert.Equal(1000, goals[0].TargetAmount);
    }

    [Fact]
    public async Task GetAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private GoalsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new GoalsEndpoint(httpClient, _errors);
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
