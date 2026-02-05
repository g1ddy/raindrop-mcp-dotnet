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

    [Description("Collection description")]
    public string? Description { get; init; }

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

    [Description("View mode (list, grid, masonry, simple)")]
    public string? View { get; init; }

    [Description("Number of bookmarks in the collection")]
    public int Count { get; init; }

    [Description("Collection sort order")]
    public int? Sort { get; init; }

    [Description("Collection expanded state")]
    public bool? Expanded { get; init; }

    [Description("User who created the collection")]
    public CollectionUser? User { get; init; }

    [Description("Creator reference")]
    public UserRef? CreatorRef { get; init; }

    [Description("Last action timestamp")]
    public DateTime? LastAction { get; init; }

    [Description("Creation timestamp")]
    public DateTime? Created { get; init; }

    [Description("Last update timestamp")]
    public DateTime? LastUpdate { get; init; }

    [Description("URL slug")]
    public string? Slug { get; init; }

    [Description("Access control details")]
    public Access? Access { get; init; }

    [Description("Indicates if the user is the author")]
    public bool? Author { get; init; }
}

public record CollectionUser
{
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }

    [JsonPropertyName("$id")]
    public int Id { get; init; }
}

public record UserRef
{
    [JsonPropertyName("_id")]
    public int Id { get; init; }

    public string? Name { get; init; }
    public string? Email { get; init; }
}

public record Access
{
    public int? For { get; init; }
    public int? Level { get; init; }
    public bool? Root { get; init; }
    public bool? Draggable { get; init; }
}
