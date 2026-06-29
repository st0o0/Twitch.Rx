using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.ContentClassification;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.ContentClassification;

public sealed class ContentClassificationEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetLabelsAsync_ReturnsLabels()
    {
        var endpoint = CreateEndpoint(JsonResponse(
            """{"data":[{"id":"DrugsIntoxication","description":"Presence or consumption of illegal drugs","name":"Drugs, Intoxication, or Excessive Tobacco Use"}]}"""));

        var labels = await endpoint.GetLabelsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(labels);
        Assert.Equal("DrugsIntoxication", labels[0].Id);
    }

    [Fact]
    public async Task GetLabelsAsync_WithLocale_PassesLocaleParam()
    {
        var handler = new FakeHttpHandler(JsonResponse("""{"data":[]}"""));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        var endpoint = new ContentClassificationEndpoint(httpClient, _errors);

        await endpoint.GetLabelsAsync("de-DE", TestContext.Current.CancellationToken);

        Assert.Contains("locale=de-DE", handler.LastRequest?.RequestUri?.Query);
    }

    [Fact]
    public async Task GetLabelsAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetLabelsAsync(ct: TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private ContentClassificationEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new ContentClassificationEndpoint(httpClient, _errors);
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
