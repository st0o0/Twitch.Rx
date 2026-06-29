using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using R3;

namespace Twitch.Rx.Helix;

internal abstract class HelixEndpoint(HttpClient httpClient, Subject<HelixError> errors)
{
    protected async Task<T?> GetFirstAsync<T>(
        string url, JsonTypeInfo<HelixResponse<T>> typeInfo, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        await EnsureSuccessAsync(response, HttpMethod.Get, url, ct);
        var result = await response.Content.ReadFromJsonAsync(typeInfo, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
        return result.Data.Length > 0 ? result.Data[0] : default;
    }

    protected async Task<IReadOnlyList<T>> GetListAsync<T>(
        string url, JsonTypeInfo<HelixResponse<T>> typeInfo, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        await EnsureSuccessAsync(response, HttpMethod.Get, url, ct);
        var result = await response.Content.ReadFromJsonAsync(typeInfo, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
        return result.Data;
    }

    protected async Task<TRes> PostAsync<TReq, TRes>(
        string url, TReq body,
        JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<HelixResponse<TRes>> resInfo,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, HttpMethod.Post, url, ct);
        var result = await response.Content.ReadFromJsonAsync(resInfo, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
        return result.Data[0];
    }

    protected async Task PostAsync<TReq>(
        string url, TReq body, JsonTypeInfo<TReq> reqInfo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, HttpMethod.Post, url, ct);
    }

    protected async Task PatchAsync<TReq>(
        string url, TReq body, JsonTypeInfo<TReq> reqInfo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, HttpMethod.Patch, url, ct);
    }

    protected async Task<TRes> PatchAsync<TReq, TRes>(
        string url, TReq body,
        JsonTypeInfo<TReq> reqInfo, JsonTypeInfo<HelixResponse<TRes>> resInfo,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, reqInfo)
        };
        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, HttpMethod.Patch, url, ct);
        var result = await response.Content.ReadFromJsonAsync(resInfo, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
        return result.Data[0];
    }

    protected async Task PutAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        using var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, HttpMethod.Put, url, ct);
    }

    protected async Task DeleteAsync(string url, CancellationToken ct)
    {
        using var response = await httpClient.DeleteAsync(url, ct);
        await EnsureSuccessAsync(response, HttpMethod.Delete, url, ct);
    }

    protected async IAsyncEnumerable<TModel> GetAllPagesAsync<TDto, TModel>(
        string url,
        JsonTypeInfo<HelixPaginatedResponse<TDto>> typeInfo,
        [EnumeratorCancellation] CancellationToken ct,
        Func<TDto, TModel> mapper)
    {
        string? cursor = null;
        do
        {
            var pageUrl = cursor is null ? url : AppendCursor(url, cursor);
            using var response = await httpClient.GetAsync(pageUrl, ct);
            await EnsureSuccessAsync(response, HttpMethod.Get, pageUrl, ct);
            var result = await response.Content.ReadFromJsonAsync(typeInfo, ct)
                ?? throw new InvalidOperationException("Failed to deserialize response.");

            foreach (var item in result.Data)
                yield return mapper(item);

            cursor = result.Pagination?.Cursor;
        } while (!string.IsNullOrEmpty(cursor));
    }

    protected async Task<Page<TModel>> GetPageAsync<TDto, TModel>(
        string url, string? cursor,
        JsonTypeInfo<HelixPaginatedResponse<TDto>> typeInfo,
        CancellationToken ct,
        Func<TDto, TModel> mapper)
    {
        var pageUrl = cursor is null ? url : AppendCursor(url, cursor);
        using var response = await httpClient.GetAsync(pageUrl, ct);
        await EnsureSuccessAsync(response, HttpMethod.Get, pageUrl, ct);
        var result = await response.Content.ReadFromJsonAsync(typeInfo, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response.");
        var items = result.Data.Select(mapper).ToArray();
        return new Page<TModel>(items, result.Pagination?.Cursor);
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response, HttpMethod method, string url, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var statusCode = (int)response.StatusCode;
        string errorMessage;
        string errorType;

        try
        {
            var errorDto = await response.Content.ReadFromJsonAsync(
                HelixJsonContext.Default.HelixErrorDto, ct);
            errorMessage = errorDto?.Message ?? "";
            errorType = errorDto?.Error ?? response.StatusCode.ToString();
        }
        catch (JsonException)
        {
            errorMessage = await response.Content.ReadAsStringAsync(ct);
            errorType = response.StatusCode.ToString();
        }

        var error = new HelixError(statusCode, errorType, errorMessage, method, url);
        errors.OnNext(error);
        throw new HelixException(statusCode, errorType, errorMessage, method, url);
    }

    private static string AppendCursor(string url, string cursor)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}after={Uri.EscapeDataString(cursor)}";
    }
}
