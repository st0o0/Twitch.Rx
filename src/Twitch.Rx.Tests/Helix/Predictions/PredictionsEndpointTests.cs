using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Predictions;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Predictions;

public sealed class PredictionsEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    // ── GetAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsPredictionPage()
    {
        var endpoint = CreateEndpoint(PredictionPageResponse());

        var page = await endpoint.GetAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("pred-1", page.Items[0].Id);
        Assert.Equal("Will I win?", page.Items[0].Title);
        Assert.Equal(PredictionStatus.Active, page.Items[0].Status);
        Assert.Equal(2, page.Items[0].Outcomes.Count);
    }

    // ── GetAllAsync ───────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_IteratesAllPages()
    {
        var page1 = PredictionPageResponseWithCursor("pred-1", "cursor-next");
        var page2 = PredictionPageResponse("pred-2");
        var endpoint = CreateEndpoint(page1, page2);

        var predictions = new List<Prediction>();
        await foreach (var p in endpoint.GetAllAsync("123", TestContext.Current.CancellationToken))
            predictions.Add(p);

        Assert.Equal(2, predictions.Count);
        Assert.Equal("pred-1", predictions[0].Id);
        Assert.Equal("pred-2", predictions[1].Id);
    }

    // ── CreateAsync ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsCreatedPrediction()
    {
        var endpoint = CreateEndpoint(PredictionResponse("pred-new", "New Prediction", "ACTIVE"));

        var pred = await endpoint.CreateAsync(
            new CreatePredictionRequest("123", "New Prediction", ["Yes", "No"], 1200),
            TestContext.Current.CancellationToken);

        Assert.Equal("pred-new", pred.Id);
        Assert.Equal("New Prediction", pred.Title);
        Assert.Equal(PredictionStatus.Active, pred.Status);
    }

    // ── EndAsync ──────────────────────────────────────────

    [Fact]
    public async Task EndAsync_ReturnsResolvedPrediction()
    {
        var endpoint = CreateEndpoint(PredictionResponse("pred-1", "Will I win?", "RESOLVED",
            winningOutcomeId: "outcome-1"));

        var pred = await endpoint.EndAsync(
            new EndPredictionRequest("123", "pred-1", PredictionEndStatus.Resolved, "outcome-1"),
            TestContext.Current.CancellationToken);

        Assert.Equal("pred-1", pred.Id);
        Assert.Equal(PredictionStatus.Resolved, pred.Status);
        Assert.Equal("outcome-1", pred.WinningOutcomeId);
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

    private PredictionsEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new PredictionsEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage PredictionPageResponse(string id = "pred-1", string cursor = "") =>
        JsonResponse($$"""
        {
          "data": [{{PredictionJson(id, "Will I win?", "ACTIVE")}}],
          "pagination": {{(string.IsNullOrEmpty(cursor) ? "{}" : $"{{\"cursor\":\"{cursor}\"}}") }}
        }
        """);

    private static HttpResponseMessage PredictionPageResponseWithCursor(string id, string cursor) =>
        PredictionPageResponse(id, cursor);

    private static HttpResponseMessage PredictionResponse(string id, string title, string status,
        string? winningOutcomeId = null) =>
        JsonResponse($$"""{"data": [{{PredictionJson(id, title, status, winningOutcomeId)}}]}""");

    private static string PredictionJson(string id, string title, string status,
        string? winningOutcomeId = null) => $$"""
        {
          "id": "{{id}}",
          "broadcaster_id": "123",
          "broadcaster_login": "streamer",
          "broadcaster_name": "Streamer",
          "title": "{{title}}",
          "winning_outcome_id": {{(winningOutcomeId is null ? "null" : $"\"{winningOutcomeId}\"")}},
          "outcomes": [
            {"id": "outcome-1", "title": "Yes", "users": 5, "channel_points": 500, "top_predictors": null, "color": "BLUE"},
            {"id": "outcome-2", "title": "No", "users": 3, "channel_points": 300, "top_predictors": null, "color": "PINK"}
          ],
          "prediction_window": 1200,
          "status": "{{status}}",
          "created_at": "2024-01-01T00:00:00Z",
          "ended_at": null,
          "locked_at": null
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
