using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Tags;

/// <summary>
/// Payload for renaming tags.
/// </summary>
[Description("Tag rename request")]
public record TagRenameRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("New tag value to replace existing tags")]
    public string? Replace { get; init; }

    [Description("List of tags to rename")]
    public List<string> Tags { get; init; } = new();
}
