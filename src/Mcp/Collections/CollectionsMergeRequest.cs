using System.ComponentModel;

namespace Mcp.Collections;

/// <summary>
/// Payload for merging multiple collections into a destination collection.
/// </summary>
[Description("Merge collections request")]
public record CollectionsMergeRequest
{
    [Description("Collection ID where listed collection ids will be merged")]
    public int To { get; init; }

    [Description("Collection IDs to merge")]
    public List<int> Ids { get; init; } = new();
}
