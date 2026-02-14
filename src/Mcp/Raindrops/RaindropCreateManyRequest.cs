using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Raindrops;

/// <summary>
/// Request payload for creating multiple bookmarks.
/// </summary>
[Description("Create many bookmarks request")]
public record RaindropCreateManyRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Target collection ID for the new bookmarks")]
    public int? CollectionId { get; init; }

    [Description("Bookmarks to create")]
    public IReadOnlyList<Raindrop> Items { get; init; } = Array.Empty<Raindrop>();
}
