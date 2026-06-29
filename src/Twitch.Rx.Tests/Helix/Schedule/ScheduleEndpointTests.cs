using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Schedule;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Schedule;

public sealed class ScheduleEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    private static string ScheduleResponse(string segmentsJson, string? cursor = null)
    {
        var paginationJson = cursor is not null ? $$"""{"cursor":"{{cursor}}"}""" : "{}";
        return $$"""{"data":{"segments":[{{segmentsJson}}],"broadcaster_id":"123","broadcaster_name":"Broadcaster","broadcaster_login":"broadcaster"},"pagination":{{paginationJson}}}""";
    }

    private const string SegmentJson =
        """{"id":"seg-1","start_time":"2024-01-01T18:00:00Z","end_time":"2024-01-01T20:00:00Z","title":"Stream","canceled_until":null,"category":null,"is_recurring":false}""";

    [Fact]
    public async Task GetScheduleAsync_ReturnsPage()
    {
        var endpoint = CreateEndpoint(JsonResponse(ScheduleResponse(SegmentJson)));

        var page = await endpoint.GetScheduleAsync("123", ct: TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("seg-1", page.Items[0].Id);
        Assert.Equal("Stream", page.Items[0].Title);
        Assert.False(page.Items[0].IsRecurring);
    }

    [Fact]
    public async Task GetAllScheduleAsync_IteratesAllPages()
    {
        var page1 = JsonResponse(ScheduleResponse(SegmentJson, cursor: "next"));
        var page2 = JsonResponse(ScheduleResponse(SegmentJson.Replace("seg-1", "seg-2")));
        var endpoint = CreateEndpoint(page1, page2);

        var items = new List<ScheduleSegment>();
        await foreach (var seg in endpoint.GetAllScheduleAsync("123", TestContext.Current.CancellationToken))
            items.Add(seg);

        Assert.Equal(2, items.Count);
        Assert.Equal("seg-1", items[0].Id);
        Assert.Equal("seg-2", items[1].Id);
    }

    [Fact]
    public async Task GetICalendarAsync_ReturnsICalString()
    {
        var endpoint = CreateEndpoint(PlainTextResponse("BEGIN:VCALENDAR\r\nEND:VCALENDAR"));

        var result = await endpoint.GetICalendarAsync("123", TestContext.Current.CancellationToken);

        Assert.Contains("BEGIN:VCALENDAR", result);
    }

    [Fact]
    public async Task CreateSegmentAsync_ReturnsNewSegment()
    {
        var endpoint = CreateEndpoint(JsonResponse(ScheduleResponse(SegmentJson)));

        var result = await endpoint.CreateSegmentAsync("123",
            new CreateScheduleSegmentRequest("2024-01-01T18:00:00Z", "America/New_York", false, Title: "Stream"),
            TestContext.Current.CancellationToken);

        Assert.Equal("seg-1", result.Id);
        Assert.Equal("Stream", result.Title);
    }

    [Fact]
    public async Task UpdateSegmentAsync_ReturnsUpdatedSegment()
    {
        var updatedSegment = SegmentJson.Replace("\"Stream\"", "\"Updated Stream\"");
        var endpoint = CreateEndpoint(JsonResponse(ScheduleResponse(updatedSegment)));

        var result = await endpoint.UpdateSegmentAsync("123", "seg-1",
            new UpdateScheduleSegmentRequest(Title: "Updated Stream"),
            TestContext.Current.CancellationToken);

        Assert.Equal("seg-1", result.Id);
        Assert.Equal("Updated Stream", result.Title);
    }

    [Fact]
    public async Task DeleteSegmentAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(NoContentResponse());

        await endpoint.DeleteSegmentAsync("123", "seg-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetScheduleAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetScheduleAsync("123", ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private ScheduleEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ScheduleEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage PlainTextResponse(string text) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(text, System.Text.Encoding.UTF8, "text/calendar")
        };

    private static HttpResponseMessage NoContentResponse() => new(HttpStatusCode.NoContent);

    private static HttpResponseMessage ErrorResponse(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}
