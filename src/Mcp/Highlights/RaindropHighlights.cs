using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Highlights;

/// <summary>
/// Container for highlights associated with a bookmark.
/// </summary>
[Description("Bookmark with its highlights")]
public record RaindropHighlights
{
    [JsonPropertyName("_id")]
    [Description("Identifier of the bookmark")]
    public long? Id { get; init; }

    [Description("Collection of highlight objects")]
    public List<Highlight> Highlights { get; init; } = new();
}
