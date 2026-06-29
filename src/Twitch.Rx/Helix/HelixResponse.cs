using System.Text.Json.Serialization;

namespace Twitch.Rx.Helix;

internal sealed record HelixResponse<T>(
    [property: JsonPropertyName("data")] T[] Data);

internal sealed record HelixPaginatedResponse<T>(
    [property: JsonPropertyName("data")] T[] Data,
    [property: JsonPropertyName("pagination")] PaginationInfo? Pagination);

internal sealed record PaginationInfo(
    [property: JsonPropertyName("cursor")] string? Cursor);
