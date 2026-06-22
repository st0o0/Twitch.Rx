using System.Net;
using System.Net.Http.Headers;

namespace Twitch.Rx.Auth;

internal sealed class TwitchAuthHandler(ITwitchAuth auth, string clientId) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        byte[]? requestBody = null;
        MediaTypeHeaderValue? contentType = null;
        if (request.Content is not null)
        {
            requestBody = await request.Content.ReadAsByteArrayAsync(ct);
            contentType = request.Content.Headers.ContentType;
        }

        await ApplyAuthAsync(request, ct);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshed = await auth.RefreshTokenAsync(ct);
            using var retry = CloneRequestFromBytes(request, requestBody, contentType);
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Token);
            retry.Headers.TryAddWithoutValidation("Client-Id", clientId);
            response = await base.SendAsync(retry, ct);
        }
        else if (response.StatusCode == (HttpStatusCode)429)
        {
            var delay = GetRateLimitDelay(response);
            await Task.Delay(delay, ct);
            using var retry = CloneRequestFromBytes(request, requestBody, contentType);
            await ApplyAuthAsync(retry, ct);
            response = await base.SendAsync(retry, ct);
        }

        return response;
    }

    private async Task ApplyAuthAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await auth.GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.TryAddWithoutValidation("Client-Id", clientId);
    }

    private static TimeSpan GetRateLimitDelay(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Ratelimit-Reset", out var values))
        {
            var value = values.FirstOrDefault();
            if (value is not null && long.TryParse(value, out var epoch))
            {
                var resetAt = DateTimeOffset.FromUnixTimeSeconds(epoch);
                var delay = resetAt - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    return delay;
                }
            }
        }
        return TimeSpan.FromSeconds(1);
    }

    private static HttpRequestMessage CloneRequestFromBytes(
        HttpRequestMessage request, byte[]? body, MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            if (contentType is not null)
            {
                clone.Content.Headers.ContentType = contentType;
            }
        }
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
