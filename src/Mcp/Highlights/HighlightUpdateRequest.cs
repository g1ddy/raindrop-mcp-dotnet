using System.ComponentModel;

namespace Mcp.Highlights;

/// <summary>
/// Request payload for updating an existing highlight.
/// </summary>
[Description("Request payload for updating an existing highlight")]
public record HighlightUpdateRequest
{
    [Description("The unique identifier of the highlight to update. This field is required.")]
    public string Id { get; init; } = string.Empty;

    [Description("The updated text content of the highlight.")]
    public string? Text { get; init; }

    [Description("The updated note for the highlight.")]
    public string? Note { get; init; }

    [Description("The new color for the highlight.")]
    public string? Color { get; init; }
}
