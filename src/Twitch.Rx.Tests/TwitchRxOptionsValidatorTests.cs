using Microsoft.Extensions.Options;
using Twitch.Rx;
using Xunit;

namespace Twitch.Rx.Tests;

public sealed class TwitchRxOptionsValidatorTests
{
    private readonly TwitchRxOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var options = new TwitchRxOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("  ", "secret")]
    [InlineData("id", "")]
    [InlineData("id", "  ")]
    public void Validate_MissingCredentials_ReturnsFail(string clientId, string clientSecret)
    {
        var options = new TwitchRxOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Options_HaveCorrectDefaults()
    {
        var options = new TwitchRxOptions
        {
            ClientId = "id",
            ClientSecret = "secret"
        };

        Assert.Equal(new Uri("https://id.twitch.tv"), options.Auth.BaseUrl);
        Assert.Equal(new Uri("https://api.twitch.tv"), options.Api.BaseUrl);
        Assert.True(options.Api.Enabled);
        Assert.Equal(new Uri("wss://eventsub.wss.twitch.tv/ws"), options.EventSub.WebSocketUrl);
        Assert.False(options.EventSub.Enabled);
        Assert.True(options.EventSub.AutoReconnect);
        Assert.Null(options.EventSub.KeepaliveTimeout);
        Assert.Empty(options.EventSub.Subscriptions);
    }
}
