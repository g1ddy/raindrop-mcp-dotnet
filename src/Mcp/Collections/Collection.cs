using System.ComponentModel;
using System.Text.Json.Serialization;

using Mcp.Common;

namespace Mcp.Collections;

/// <summary>
/// Represents a Raindrop.io collection (folder).
/// </summary>
[Description("Bookmark collection")]
public record Collection
{
    [JsonPropertyName("_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Description("Unique identifier of the collection")]
    public int Id { get; init; }

    [Description("Collection title")]
    public string? Title { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Description("Reference to the parent collection")]
    public IdRef? Parent { get; init; }

    [Description("Display color for the collection")]
    public string? Color { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Cover image URLs")]
    public List<string>? Cover { get; init; }

    [Description("Indicates if the collection is shared publicly")]
    public bool? Public { get; init; }
}
