using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Tags;

/// <summary>
/// Represents a tag and its usage count.
/// </summary>
[Description("Tag information")]
public record TagInfo
{
    [JsonPropertyName("_id")]
    [Description("Tag value")]
    public string Id { get; init; } = string.Empty;

    [Description("Number of bookmarks with this tag")]
    public int Count { get; init; }
}
