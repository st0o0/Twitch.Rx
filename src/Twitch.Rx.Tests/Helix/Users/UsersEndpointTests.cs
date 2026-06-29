using System.Net;
using R3;
using Twitch.Rx.Helix;
using Twitch.Rx.Helix.Users;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Helix.Users;

public sealed class UsersEndpointTests : IDisposable
{
    private readonly Subject<HelixError> _errors = new();

    [Fact]
    public async Task GetByLoginAsync_ReturnsUser()
    {
        var endpoint = CreateEndpoint(UserResponse("123", "testuser", "TestUser"));

        var user = await endpoint.GetByLoginAsync("testuser", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal("123", user!.Id);
        Assert.Equal("testuser", user.Login);
        Assert.Equal("TestUser", user.DisplayName);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsNull_WhenNotFound()
    {
        var endpoint = CreateEndpoint(EmptyResponse());

        var user = await endpoint.GetByLoginAsync("nonexistent", TestContext.Current.CancellationToken);

        Assert.Null(user);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser()
    {
        var endpoint = CreateEndpoint(UserResponse("123", "testuser", "TestUser"));

        var user = await endpoint.GetByIdAsync("123", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal("123", user!.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMultipleUsers()
    {
        var json = """{"data":[{"id":"1","login":"a","display_name":"A","type":"","broadcaster_type":"","description":"","profile_image_url":"","offline_image_url":"","created_at":"2020-01-01T00:00:00Z"},{"id":"2","login":"b","display_name":"B","type":"","broadcaster_type":"","description":"","profile_image_url":"","offline_image_url":"","created_at":"2020-01-01T00:00:00Z"}]}""";
        var endpoint = CreateEndpoint(JsonResponse(json));

        var users = await endpoint.GetByIdsAsync(["1", "2"], TestContext.Current.CancellationToken);

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task GetByIdAsync_OnError_PublishesToErrorObservable()
    {
        var endpoint = CreateEndpoint(ErrorResponse(401, "Unauthorized", "Invalid token"));
        HelixError? received = null;
        using var sub = _errors.Subscribe(e => received = e);

        await Assert.ThrowsAsync<HelixException>(
            () => endpoint.GetByIdAsync("123", TestContext.Current.CancellationToken));

        Assert.NotNull(received);
        Assert.Equal(401, received!.StatusCode);
    }

    private UsersEndpoint CreateEndpoint(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new UsersEndpoint(httpClient, _errors);
    }

    private static HttpResponseMessage UserResponse(string id, string login, string displayName) =>
        JsonResponse($$"""{"data":[{"id":"{{id}}","login":"{{login}}","display_name":"{{displayName}}","type":"","broadcaster_type":"affiliate","description":"Test","profile_image_url":"https://example.com/img.png","offline_image_url":"","created_at":"2020-01-01T00:00:00Z"}]}""");

    private static HttpResponseMessage EmptyResponse() =>
        JsonResponse("""{"data":[]}""");

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
