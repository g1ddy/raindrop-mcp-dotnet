using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mcp.Raindrops;

/// <summary>
/// Request payload for updating an existing bookmark.
/// </summary>
[Description("Request payload for updating an existing bookmark")]
public record RaindropUpdateRequest : IRaindropRequest
{
    [Url]
    [Description("The new URL for the bookmark.")]
    public string? Link { get; init; }

    [Description("The new title for the bookmark.")]
    public string? Title { get; init; }

    [MaxLength(10000)]
    [Description("The new description or excerpt for the bookmark. Max length: 10000 characters.")]
    public string? Excerpt { get; init; }

    [MaxLength(10000)]
    [Description("The new personal note for the bookmark. Max length: 10000 characters.")]
    public string? Note { get; init; }

    [Description("A new list of tags for the bookmark. This will replace the existing tags.")]
    public IReadOnlyList<string>? Tags { get; init; }

    [Description("A boolean flag to mark the bookmark as a favorite.")]
    public bool? Important { get; init; }

    [Description("The ID of the collection to move the bookmark to.")]
    public int? CollectionId { get; init; }
}
