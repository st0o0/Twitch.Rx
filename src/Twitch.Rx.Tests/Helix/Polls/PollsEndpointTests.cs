using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Polls;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Polls;

public sealed class PollsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsPollPage()
    {
        var endpoint = CreateEndpoint(PollPageResponse());

        var page = await endpoint.GetAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("poll-1", page.Items[0].Id);
        Assert.Equal("Test Poll", page.Items[0].Title);
        Assert.Equal(PollStatus.Active, page.Items[0].Status);
        Assert.Equal(2, page.Items[0].Choices.Count);
    }

    // ── GetAllAsync ───────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_IteratesAllPages()
    {
        var page1 = PollPageResponseWithCursor("poll-1", "cursor-next");
        var page2 = PollPageResponse("poll-2");
        var endpoint = CreateEndpoint(page1, page2);

        var polls = new List<Poll>();
        await foreach (var poll in endpoint.GetAllAsync("123", TestContext.Current.CancellationToken))
            polls.Add(poll);

        Assert.Equal(2, polls.Count);
        Assert.Equal("poll-1", polls[0].Id);
        Assert.Equal("poll-2", polls[1].Id);
    }

    // ── CreateAsync ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsCreatedPoll()
    {
        var endpoint = CreateEndpoint(PollResponse("poll-new", "New Poll", "ACTIVE"));

        var poll = await endpoint.CreateAsync(
            new CreatePollRequest("123", "New Poll", ["Yes", "No"], 300),
            TestContext.Current.CancellationToken);

        Assert.Equal("poll-new", poll.Id);
        Assert.Equal("New Poll", poll.Title);
        Assert.Equal(PollStatus.Active, poll.Status);
    }

    // ── EndAsync ──────────────────────────────────────────

    [Fact]
    public async Task EndAsync_ReturnsTerminatedPoll()
    {
        var endpoint = CreateEndpoint(PollResponse("poll-1", "Test Poll", "TERMINATED"));

        var poll = await endpoint.EndAsync(
            new EndPollRequest("123", "poll-1", PollEndStatus.Terminated),
            TestContext.Current.CancellationToken);

        Assert.Equal("poll-1", poll.Id);
        Assert.Equal(PollStatus.Terminated, poll.Status);
    }

    // ── Error handling ─────────────────────────────────────

    [Fact]
    public async Task GetAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetAsync("123", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────

    private PollsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new PollsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage PollPageResponse(string id = "poll-1", string cursor = "") =>
        JsonResponse($$"""
        {
          "data": [{{PollJson(id, "Test Poll", "ACTIVE")}}],
          "pagination": {{(string.IsNullOrEmpty(cursor) ? "{}" : $"{{\"cursor\":\"{cursor}\"}}") }}
        }
        """);

    private static HttpResponseMessage PollPageResponseWithCursor(string id, string cursor) =>
        PollPageResponse(id, cursor);

    private static HttpResponseMessage PollResponse(string id, string title, string status) =>
        JsonResponse($$"""{"data": [{{PollJson(id, title, status)}}]}""");

    private static string PollJson(string id, string title, string status) => $$"""
        {
          "id": "{{id}}",
          "broadcaster_id": "123",
          "broadcaster_login": "streamer",
          "broadcaster_name": "Streamer",
          "title": "{{title}}",
          "choices": [
            {"id": "c1", "title": "Yes", "votes": 10, "channel_points_votes": 5, "bits_votes": 0},
            {"id": "c2", "title": "No", "votes": 8, "channel_points_votes": 3, "bits_votes": 0}
          ],
          "bits_voting_enabled": false,
          "bits_per_vote": 0,
          "channel_points_voting_enabled": true,
          "channel_points_per_vote": 100,
          "status": "{{status}}",
          "duration": 300,
          "started_at": "2024-01-01T00:00:00Z",
          "ended_at": null
        }
        """;

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
