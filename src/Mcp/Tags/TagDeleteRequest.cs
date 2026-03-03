using System.ComponentModel;

namespace Mcp.Tags;

/// <summary>
/// Payload for deleting tags.
/// </summary>
[Description("Tag delete request")]
public record TagDeleteRequest
{
    [Description("Tags to remove")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
