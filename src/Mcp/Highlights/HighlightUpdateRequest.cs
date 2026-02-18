using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mcp.Raindrops;

namespace Mcp.Highlights;

/// <summary>
/// Request payload for updating an existing highlight.
/// </summary>
[Description("Request payload for updating an existing highlight")]
public record HighlightUpdateRequest
{
    [Required(AllowEmptyStrings = false)]
    [Description("The unique identifier of the highlight to update. This field is required.")]
    public string Id { get; init; } = string.Empty;

    [MaxLength(Raindrop.MaxTextFieldLength)]
    [Description("The updated text content of the highlight.")]
    public string? Text { get; init; }

    [MaxLength(Raindrop.MaxTextFieldLength)]
    [Description("The updated note for the highlight.")]
    public string? Note { get; init; }

    [Description("The new color for the highlight.")]
    public string? Color { get; init; }
}
