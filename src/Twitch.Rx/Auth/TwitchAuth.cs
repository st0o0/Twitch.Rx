using System.Net.Http.Json;
using R3;
using Twitch.Rx.Auth.Models;
using Twitch.Rx.Json;

namespace Twitch.Rx.Auth;

public sealed class TwitchAuth : ITwitchAuth
{
    private readonly TwitchRxOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Subject<AccessToken> _tokenChanged = new();
    private readonly Subject<AuthError> _errors = new();
    private volatile bool _seeded;

    public Observable<AccessToken> TokenChanged => _tokenChanged;
    public Observable<AuthError> Errors => _errors;

    public TwitchAuth(TwitchRxOptions options, HttpClient httpClient, ITokenStore tokenStore)
    {
        _options = options;
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }

    public async ValueTask<AccessToken> GetTokenAsync(CancellationToken ct = default)
    {
        if (!_seeded && _options.AccessToken is not null)
        {
            await _lock.WaitAsync(ct);
            try
            {
                if (!_seeded)
                {
                    var initial = new AccessToken(
                        _options.AccessToken, "bearer", 3600,
                        _options.RefreshToken, [], DateTimeOffset.UtcNow);
                    await _tokenStore.SetAsync(initial, ct);
                    _seeded = true;
                }
            }
            finally { _lock.Release(); }
        }

        var cached = await _tokenStore.GetAsync(ct);
        if (cached is not null && !cached.IsExpired)
            return cached;

        await _lock.WaitAsync(ct);
        try
        {
            cached = await _tokenStore.GetAsync(ct);
            if (cached is not null && !cached.IsExpired)
                return cached;

            if (cached?.RefreshToken is not null)
                return await RefreshTokenCoreAsync(cached.RefreshToken, ct);

            return await AcquireClientCredentialsAsync(ct);
        }
        finally { _lock.Release(); }
    }

    public async ValueTask<AccessToken> RefreshTokenAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var cached = await _tokenStore.GetAsync(ct);
            var refreshToken = cached?.RefreshToken ?? _options.RefreshToken;

            if (refreshToken is not null)
                return await RefreshTokenCoreAsync(refreshToken, ct);

            // No refresh token available — re-acquire via client credentials
            return await AcquireClientCredentialsAsync(ct);
        }
        finally { _lock.Release(); }
    }

    public async ValueTask<TokenValidation> ValidateAsync(CancellationToken ct = default)
    {
        var token = await GetTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/oauth2/validate");
        request.Headers.Authorization = new("Bearer", token.Token);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync(
            AuthJsonContext.Default.TwitchValidationResponse, ct)
            ?? throw new InvalidOperationException("Failed to deserialize validation response.");

        return new TokenValidation(dto.ClientId, dto.Login, dto.Scopes, dto.UserId, dto.ExpiresIn);
    }

    private async Task<AccessToken> AcquireClientCredentialsAsync(CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "client_credentials"
        });

        var response = await _httpClient.PostAsync("/oauth2/token", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Token acquisition failed ({response.StatusCode}): {body}");
        }
        return await StoreAndEmitAsync(response, ct);
    }

    private async Task<AccessToken> RefreshTokenCoreAsync(string refreshToken, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await _httpClient.PostAsync("/oauth2/token", content, ct);
        response.EnsureSuccessStatusCode();
        return await StoreAndEmitAsync(response, ct);
    }

    private async Task<AccessToken> StoreAndEmitAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var dto = await response.Content.ReadFromJsonAsync(
            AuthJsonContext.Default.TwitchTokenResponse, ct)
            ?? throw new InvalidOperationException("Failed to deserialize token response.");

        var token = new AccessToken(
            dto.AccessToken, dto.TokenType, dto.ExpiresIn,
            dto.RefreshToken, dto.Scope ?? [], DateTimeOffset.UtcNow);

        await _tokenStore.SetAsync(token, ct);
        _tokenChanged.OnNext(token);
        _seeded = true;
        return token;
    }

    public ValueTask DisposeAsync()
    {
        _tokenChanged.Dispose();
        _errors.Dispose();
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }
}
