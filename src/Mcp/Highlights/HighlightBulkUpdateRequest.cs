using System.ComponentModel;

namespace Mcp.Highlights;

/// <summary>
/// Payload for bulk updating highlights for a bookmark.
/// </summary>
[Description("Bulk update request for highlights")]
public record HighlightBulkUpdateRequest
{
    [Description("List of highlight updates to apply")]
    public List<HighlightBulkUpdate> Highlights { get; init; } = new();
}
