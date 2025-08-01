using System.ComponentModel;

namespace Mcp.Highlights;

/// <summary>
/// Request payload for creating a new highlight.
/// </summary>
[Description("Request payload for creating a new highlight")]
public record HighlightCreateRequest
{
    [Description("The text content of the highlight. This field is required.")]
    public string Text { get; init; } = string.Empty;

    [Description("An optional note to add to the highlight.")]
    public string? Note { get; init; }

    [Description("The color of the highlight.")]
    public string? Color { get; init; }
}
