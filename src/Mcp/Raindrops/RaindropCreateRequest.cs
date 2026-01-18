using System.ComponentModel;

namespace Mcp.Raindrops;

/// <summary>
/// Request payload for creating a new bookmark.
/// </summary>
[Description("Request payload for creating a new bookmark")]
public record RaindropCreateRequest : IRaindropData
{
    [Description("The URL of the bookmark. This field is required.")]
    public string Link { get; init; } = string.Empty;

    [Description("The title of the bookmark. If not provided, Raindrop.io will attempt to parse it from the URL.")]
    public string? Title { get; init; }

    [Description("A short description or excerpt from the webpage. Max length: 10000 characters.")]
    public string? Excerpt { get; init; }

    [Description("A personal note attached to the bookmark. Max length: 10000 characters.")]
    public string? Note { get; init; }

    [Description("A list of tags to associate with the bookmark.")]
    public IEnumerable<string>? Tags { get; init; }

    [Description("A boolean flag to mark the bookmark as a favorite.")]
    public bool? Important { get; init; }

    [Description("The ID of the collection to save the bookmark in. If not provided, it will be saved in the 'Unsorted' collection.")]
    public int? CollectionId { get; init; }
}
