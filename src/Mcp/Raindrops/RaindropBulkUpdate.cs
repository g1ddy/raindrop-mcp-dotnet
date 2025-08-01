using System.ComponentModel;
using System.Text.Json.Serialization;

using Mcp.Common;

namespace Mcp.Raindrops;

/// <summary>
/// Bulk update payload for modifying multiple bookmarks at once.
/// Set <c>Collection.Id</c> to <c>-99</c> to move items to the Trash for
/// bulk deletion.
/// </summary>
[Description("Bulk update payload for multiple bookmarks")]
public record RaindropBulkUpdate
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("List of bookmark IDs to update. If null, update all filtered items")]
    public List<long>? Ids { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Set or clear the favorite flag")]
    public bool? Important { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Tags to apply to all affected bookmarks")]
    public List<string>? Tags { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("New media attachments or covers")]
    public List<object>? Media { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Cover image URL")]
    public string? Cover { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Target collection for moved bookmarks")]
    public IdRef? Collection { get; init; }
}
