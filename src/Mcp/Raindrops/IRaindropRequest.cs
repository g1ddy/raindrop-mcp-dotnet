namespace Mcp.Raindrops;

/// <summary>
/// Represents the common data properties for creating and updating a Raindrop bookmark.
/// </summary>
public interface IRaindropRequest
{
    /// <summary>
    /// The URL of the bookmark.
    /// </summary>
    string? Link { get; }

    /// <summary>
    /// The title of the bookmark.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// A short description or excerpt from the webpage.
    /// </summary>
    string? Excerpt { get; }

    /// <summary>
    /// A personal note attached to the bookmark.
    /// </summary>
    string? Note { get; }

    /// <summary>
    /// A list of tags to associate with the bookmark.
    /// </summary>
    IReadOnlyList<string>? Tags { get; }

    /// <summary>
    /// A boolean flag to mark the bookmark as a favorite.
    /// </summary>
    bool? Important { get; }

    /// <summary>
    /// The ID of the collection to save the bookmark in.
    /// </summary>
    int? CollectionId { get; }
}
