using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.Schedule;

// ── Public Interface ──────────────────────────────────────

public interface IScheduleEndpoint
{
    Task<Page<ScheduleSegment>> GetScheduleAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default);
    IAsyncEnumerable<ScheduleSegment> GetAllScheduleAsync(string broadcasterId, CancellationToken ct = default);
    Task<string> GetICalendarAsync(string broadcasterId, CancellationToken ct = default);
    Task<ScheduleSegment> CreateSegmentAsync(string broadcasterId, CreateScheduleSegmentRequest request, CancellationToken ct = default);
    Task<ScheduleSegment> UpdateSegmentAsync(string broadcasterId, string segmentId, UpdateScheduleSegmentRequest request, CancellationToken ct = default);
    Task DeleteSegmentAsync(string broadcasterId, string segmentId, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record ScheduleSegment(
    string Id,
    string StartTime,
    string EndTime,
    string Title,
    string? CanceledUntil,
    ScheduleCategory? Category,
    bool IsRecurring);

public sealed record ScheduleCategory(string Id, string Name);

public sealed record CreateScheduleSegmentRequest(
    string StartTime,
    string Timezone,
    bool IsRecurring,
    int? DurationMinutes = null,
    string? Title = null,
    string? CategoryId = null);

public sealed record UpdateScheduleSegmentRequest(
    string? StartTime = null,
    string? Timezone = null,
    string? Duration = null,
    string? Title = null,
    string? CategoryId = null,
    bool? IsCanceled = null);

// ── Implementation ────────────────────────────────────────

internal sealed class ScheduleEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IScheduleEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<Page<ScheduleSegment>> GetScheduleAsync(string broadcasterId, string? cursor = null, CancellationToken ct = default)
    {
        var url = $"/helix/schedule?broadcaster_id={Uri.EscapeDataString(broadcasterId)}";
        if (cursor is not null) url += $"&after={Uri.EscapeDataString(cursor)}";
        var response = await GetResponseAsync(url, Ctx.ScheduleWrapperDto, ct);
        var items = (response.Data.Segments ?? []).Select(s => s.ToModel()).ToArray();
        return new Page<ScheduleSegment>(items, response.Pagination?.Cursor);
    }

    public async IAsyncEnumerable<ScheduleSegment> GetAllScheduleAsync(string broadcasterId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? cursor = null;
        do
        {
            var url = $"/helix/schedule?broadcaster_id={Uri.EscapeDataString(broadcasterId)}";
            if (cursor is not null) url += $"&after={Uri.EscapeDataString(cursor)}";
            var response = await GetResponseAsync(url, Ctx.ScheduleWrapperDto, ct);
            foreach (var segment in response.Data.Segments ?? [])
                yield return segment.ToModel();
            cursor = response.Pagination?.Cursor;
        } while (!string.IsNullOrEmpty(cursor));
    }

    public async Task<string> GetICalendarAsync(string broadcasterId, CancellationToken ct = default)
    {
        using var response = await Http.GetAsync(
            $"/helix/schedule/icalendar?broadcaster_id={Uri.EscapeDataString(broadcasterId)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<ScheduleSegment> CreateSegmentAsync(string broadcasterId, CreateScheduleSegmentRequest request, CancellationToken ct = default)
    {
        var response = await PostResponseAsync(
            $"/helix/schedule/segment?broadcaster_id={Uri.EscapeDataString(broadcasterId)}",
            request.ToDto(),
            Ctx.CreateScheduleSegmentDto,
            Ctx.ScheduleWrapperDto,
            ct);
        var segments = response.Data.Segments ?? throw new InvalidOperationException("No segments in response.");
        return segments[0].ToModel();
    }

    public async Task<ScheduleSegment> UpdateSegmentAsync(string broadcasterId, string segmentId, UpdateScheduleSegmentRequest request, CancellationToken ct = default)
    {
        var response = await PatchResponseAsync(
            $"/helix/schedule/segment?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&id={Uri.EscapeDataString(segmentId)}",
            request.ToDto(),
            Ctx.UpdateScheduleSegmentDto,
            Ctx.ScheduleWrapperDto,
            ct);
        var segments = response.Data.Segments ?? throw new InvalidOperationException("No segments in response.");
        return segments[0].ToModel();
    }

    public async Task DeleteSegmentAsync(string broadcasterId, string segmentId, CancellationToken ct = default)
        => await DeleteAsync(
            $"/helix/schedule/segment?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&id={Uri.EscapeDataString(segmentId)}",
            ct);
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ScheduleSegmentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("end_time")] string EndTime,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("canceled_until")] string? CanceledUntil,
    [property: JsonPropertyName("category")] ScheduleCategoryDto? Category,
    [property: JsonPropertyName("is_recurring")] bool IsRecurring);

internal sealed record ScheduleCategoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record ScheduleDataDto(
    [property: JsonPropertyName("segments")] ScheduleSegmentDto[]? Segments,
    [property: JsonPropertyName("broadcaster_id")] string BroadcasterId,
    [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
    [property: JsonPropertyName("broadcaster_login")] string BroadcasterLogin);

internal sealed record ScheduleWrapperDto(
    [property: JsonPropertyName("data")] ScheduleDataDto Data,
    [property: JsonPropertyName("pagination")] PaginationInfo? Pagination);

internal sealed record CreateScheduleSegmentDto(
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("timezone")] string Timezone,
    [property: JsonPropertyName("is_recurring")] bool IsRecurring,
    [property: JsonPropertyName("duration")] int? DurationMinutes,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("category_id")] string? CategoryId);

internal sealed record UpdateScheduleSegmentDto(
    [property: JsonPropertyName("start_time")] string? StartTime,
    [property: JsonPropertyName("timezone")] string? Timezone,
    [property: JsonPropertyName("duration")] string? Duration,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("category_id")] string? CategoryId,
    [property: JsonPropertyName("is_canceled")] bool? IsCanceled);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ScheduleMappings
{
    public static ScheduleSegment ToModel(this ScheduleSegmentDto dto) => new(
        dto.Id, dto.StartTime, dto.EndTime, dto.Title, dto.CanceledUntil,
        dto.Category?.ToModel(), dto.IsRecurring);

    private static ScheduleCategory ToModel(this ScheduleCategoryDto dto) => new(dto.Id, dto.Name);

    public static CreateScheduleSegmentDto ToDto(this CreateScheduleSegmentRequest req) => new(
        req.StartTime, req.Timezone, req.IsRecurring, req.DurationMinutes, req.Title, req.CategoryId);

    public static UpdateScheduleSegmentDto ToDto(this UpdateScheduleSegmentRequest req) => new(
        req.StartTime, req.Timezone, req.Duration, req.Title, req.CategoryId, req.IsCanceled);
}
