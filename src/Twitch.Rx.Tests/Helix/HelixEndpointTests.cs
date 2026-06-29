using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix;

public sealed class HelixEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetFirstAsync_ReturnsFirstItem()
    {
        var endpoint = CreateEndpoint(Json("""{"data":[{"value":"hello"}]}"""));

        var result = await endpoint.TestGetFirstAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("hello", result!.Value);
    }

    [Fact]
    public async Task GetFirstAsync_ReturnsNull_WhenEmpty()
    {
        var endpoint = CreateEndpoint(Json("""{"data":[]}"""));

        var result = await endpoint.TestGetFirstAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_ReturnsAllItems()
    {
        var endpoint = CreateEndpoint(Json("""{"data":[{"value":"a"},{"value":"b"}]}"""));

        var result = await endpoint.TestGetListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Value);
        Assert.Equal("b", result[1].Value);
    }

    [Fact]
    public async Task ErrorResponse_ThrowsHelixException()
    {
        var endpoint = CreateEndpoint(ErrorJson(401, "Unauthorized", "Invalid token"));

        var ex = await Assert.ThrowsAsync<HelixException>(
            () => endpoint.TestGetFirstAsync(TestContext.Current.CancellationToken));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("Unauthorized", ex.Error);
        Assert.Contains("Invalid token", ex.Message);
    }

    [Fact]
    public async Task ErrorResponse_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorJson(404, "Not Found", "Resource not found"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.TestGetFirstAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(404, received!.StatusCode);
        Assert.Equal("Not Found", received.Error);
    }

    [Fact]
    public async Task PostAsync_SendsBodyAndReturnsResult()
    {
        var endpoint = CreateEndpoint(Json("""{"data":[{"value":"created"}]}"""));

        var result = await endpoint.TestPostAsync(
            new TestRequest("input"), TestContext.Current.CancellationToken);

        Assert.Equal("created", result.Value);
    }

    [Fact]
    public async Task PostAsync_NoResponse_Succeeds()
    {
        var endpoint = CreateEndpoint(new HttpResponseMessage(HttpStatusCode.NoContent));

        await endpoint.TestPostNoResponseAsync(
            new TestRequest("input"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds()
    {
        var endpoint = CreateEndpoint(new HttpResponseMessage(HttpStatusCode.NoContent));

        await endpoint.TestDeleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPageWithCursor()
    {
        var endpoint = CreateEndpoint(Json(
            """{"data":[{"value":"a"}],"pagination":{"cursor":"abc123"}}"""));

        var page = await endpoint.TestGetPageAsync(null, TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Equal("a", page.Items[0]);
        Assert.Equal("abc123", page.Cursor);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPageWithoutCursor_WhenLastPage()
    {
        var endpoint = CreateEndpoint(Json(
            """{"data":[{"value":"z"}],"pagination":{}}"""));

        var page = await endpoint.TestGetPageAsync(null, TestContext.Current.CancellationToken);

        Assert.Single(page.Items);
        Assert.Null(page.Cursor);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task GetAllPagesAsync_IteratesAllPages()
    {
        var endpoint = CreateEndpoint(
            Json("""{"data":[{"value":"a"}],"pagination":{"cursor":"c1"}}"""),
            Json("""{"data":[{"value":"b"}],"pagination":{}}"""));

        var items = new List<string>();
        await foreach (var item in endpoint.TestGetAllPagesAsync(TestContext.Current.CancellationToken))
            items.Add(item);

        Assert.Equal(["a", "b"], items);
    }

    private TestEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new TestEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ErrorJson(int status, string error, string message) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(
                $$"""{"status":{{status}},"error":"{{error}}","message":"{{message}}"}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    public void Dispose() => _errors.Dispose();
}

// ── Test infrastructure ───────────────────────────────────

internal sealed record TestDto(
    [property: JsonPropertyName("value")] string Value);

internal sealed record TestRequest(
    [property: JsonPropertyName("input")] string Input);

[JsonSerializable(typeof(HelixResponse<TestDto>))]
[JsonSerializable(typeof(HelixPaginatedResponse<TestDto>))]
[JsonSerializable(typeof(TestRequest))]
internal partial class TestHelixJsonContext : JsonSerializerContext;

internal sealed class TestEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors)
{
    public Task<TestDto?> TestGetFirstAsync(CancellationToken ct)
        => GetFirstAsync("/test", TestHelixJsonContext.Default.HelixResponseTestDto, ct);

    public Task<IReadOnlyList<TestDto>> TestGetListAsync(CancellationToken ct)
        => GetListAsync("/test", TestHelixJsonContext.Default.HelixResponseTestDto, ct);

    public Task<TestDto> TestPostAsync(TestRequest req, CancellationToken ct)
        => PostAsync("/test", req,
            TestHelixJsonContext.Default.TestRequest,
            TestHelixJsonContext.Default.HelixResponseTestDto, ct);

    public Task TestPostNoResponseAsync(TestRequest req, CancellationToken ct)
        => PostAsync("/test", req, TestHelixJsonContext.Default.TestRequest, ct);

    public Task TestDeleteAsync(CancellationToken ct)
        => DeleteAsync("/test", ct);

    public Task<Page<string>> TestGetPageAsync(string? cursor, CancellationToken ct)
        => GetPageAsync("/test", cursor,
            TestHelixJsonContext.Default.HelixPaginatedResponseTestDto, ct,
            dto => dto.Value);

    public IAsyncEnumerable<string> TestGetAllPagesAsync(CancellationToken ct)
        => GetAllPagesAsync("/test",
            TestHelixJsonContext.Default.HelixPaginatedResponseTestDto, ct,
            dto => dto.Value);
}
