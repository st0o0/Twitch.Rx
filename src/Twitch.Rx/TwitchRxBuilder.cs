using Twitch.Rx.Api;
using Twitch.Rx.Auth;
using Twitch.Rx.EventSub;

namespace Twitch.Rx;

public sealed class TwitchRxBuilder
{
    private readonly Action<TwitchRxOptions> _configure;
    private ITokenStore? _tokenStore;
    private HttpClient? _authHttpClient;
    private HttpClient? _apiHttpClient;

    internal TwitchRxBuilder(Action<TwitchRxOptions> configure) => _configure = configure;

    public TwitchRxBuilder WithTokenStore(ITokenStore store) { _tokenStore = store; return this; }
    public TwitchRxBuilder WithAuthHttpClient(HttpClient client) { _authHttpClient = client; return this; }
    public TwitchRxBuilder WithApiHttpClient(HttpClient client) { _apiHttpClient = client; return this; }

    public ITwitchRxClient Build()
    {
        var options = new TwitchRxOptions { ClientId = "", ClientSecret = "" };
        _configure(options);

        var validator = new TwitchRxOptionsValidator();
        var result = validator.Validate(null, options);
        if (result.Failed) throw new InvalidOperationException(result.FailureMessage);

        var tokenStore = _tokenStore ?? new InMemoryTokenStore();
        var ownedClients = new List<HttpClient>();

        var authHttpClient = _authHttpClient;
        if (authHttpClient is null)
        {
            authHttpClient = new HttpClient { BaseAddress = options.Auth.BaseUrl };
            ownedClients.Add(authHttpClient);
        }
        var auth = new TwitchAuth(options, authHttpClient, tokenStore);

        var apiHttpClient = _apiHttpClient;
        if (apiHttpClient is null)
        {
            var handler = new TwitchAuthHandler(auth, options.ClientId) { InnerHandler = new HttpClientHandler() };
            apiHttpClient = new HttpClient(handler) { BaseAddress = options.Api.BaseUrl };
            ownedClients.Add(apiHttpClient);
        }

        ITwitchApi api = options.Api.Enabled ? new TwitchApi(apiHttpClient) : new DisabledTwitchApi();

        ITwitchEventSub eventSub = options.EventSub.Enabled
            ? new TwitchEventSub(options.EventSub, apiHttpClient, options.ClientId)
            : new DisabledTwitchEventSub();

        return new TwitchRxClient(auth, api, eventSub, [.. ownedClients]);
    }
}
