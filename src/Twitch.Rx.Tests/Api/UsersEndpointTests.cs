using System.Net;
using Twitch.Rx.Api;
using Twitch.Rx.Api.Endpoints;
using Twitch.Rx.Tests.Helpers;
using Xunit;

namespace Twitch.Rx.Tests.Api;

public sealed class UsersEndpointTests
{
    [Fact]
    public async Task GetByLoginAsync_ReturnsUser()
    {
        var handler = new FakeHttpHandler(UserResponse("123", "testuser", "TestUser"));
        var endpoint = CreateEndpoint(handler);

        var user = await endpoint.GetByLoginAsync("testuser");

        Assert.NotNull(user);
        Assert.Equal("123", user!.Id);
        Assert.Equal("testuser", user.Login);
        Assert.Equal("TestUser", user.DisplayName);
    }

    [Fact]
    public async Task GetByLoginAsync_ReturnsNull_WhenNotFound()
    {
        var handler = new FakeHttpHandler(EmptyResponse());
        var endpoint = CreateEndpoint(handler);

        var user = await endpoint.GetByLoginAsync("nonexistent");

        Assert.Null(user);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser()
    {
        var handler = new FakeHttpHandler(UserResponse("123", "testuser", "TestUser"));
        var endpoint = CreateEndpoint(handler);

        var user = await endpoint.GetByIdAsync("123");

        Assert.NotNull(user);
        Assert.Contains("id=123", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public void DisabledTwitchApi_Throws()
    {
        var api = new DisabledTwitchApi();

        var ex = Assert.Throws<InvalidOperationException>(() => api.Users);
        Assert.Contains("not enabled", ex.Message);
    }

    private static UsersEndpoint CreateEndpoint(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.twitch.tv") };
        return new UsersEndpoint(httpClient);
    }

    private static HttpResponseMessage UserResponse(string id, string login, string displayName) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"data":[{"id":"{{id}}","login":"{{login}}","display_name":"{{displayName}}","type":"","broadcaster_type":"affiliate","description":"","profile_image_url":"https://example.com/img.png","offline_image_url":"","created_at":"2020-01-01T00:00:00Z"}]}""",
                System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage EmptyResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data":[]}""", System.Text.Encoding.UTF8, "application/json")
        };
}
