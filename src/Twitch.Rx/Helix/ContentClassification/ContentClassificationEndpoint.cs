using System.Text.Json.Serialization;
using R3;

namespace Twitch.Rx.Helix.ContentClassification;

// ── Public Interface ──────────────────────────────────────

public interface IContentClassificationEndpoint
{
    Task<IReadOnlyList<ContentClassificationLabel>> GetLabelsAsync(string? locale = null, CancellationToken ct = default);
}

// ── Public Models ─────────────────────────────────────────

public sealed record ContentClassificationLabel(string Id, string Description, string Name);

// ── Implementation ────────────────────────────────────────

internal sealed class ContentClassificationEndpoint(HttpClient http, Subject<HelixError> errors)
    : HelixEndpoint(http, errors), IContentClassificationEndpoint
{
    private static HelixJsonContext Ctx => HelixJsonContext.Default;

    public async Task<IReadOnlyList<ContentClassificationLabel>> GetLabelsAsync(string? locale = null, CancellationToken ct = default)
    {
        var url = locale is not null
            ? $"/helix/content_classification_labels?locale={Uri.EscapeDataString(locale)}"
            : "/helix/content_classification_labels";
        var dtos = await GetListAsync(url, Ctx.HelixResponseContentClassificationLabelDto, ct);
        return dtos.Select(d => d.ToModel()).ToArray();
    }
}

// ── DTOs (internal) ───────────────────────────────────────

internal sealed record ContentClassificationLabelDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("name")] string Name);

// ── Mappings (file-scoped) ────────────────────────────────

static file class ContentClassificationMappings
{
    public static ContentClassificationLabel ToModel(this ContentClassificationLabelDto dto) =>
        new(dto.Id, dto.Description, dto.Name);
}
