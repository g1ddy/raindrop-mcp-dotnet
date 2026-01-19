using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Highlights;

/// <summary>
/// Represents a user created highlight on a bookmarked page.
/// </summary>
[Description("Bookmark highlight data")]
public record Highlight
{
    [JsonPropertyName("_id")]
    [Description("Unique identifier for the highlight")]
    public string? Id { get; init; }

    [Description("Highlighted text")]
    public string? Text { get; init; }

    [Description("Title of the bookmarked page")]
    public string? Title { get; init; }

    [Description("Color of the highlight")]
    public string? Color { get; init; }

    [Description("Optional note attached to the highlight")]
    public string? Note { get; init; }

    [Description("Timestamp when the highlight was created")]
    public DateTime? Created { get; init; }

    [Description("Tags associated with the highlight")]
    public List<string>? Tags { get; init; }

    [Description("URL of the bookmarked page")]
    public string? Link { get; init; }

    [Description("Timestamp of the last update")]
    public DateTime? LastUpdate { get; init; }

    [Description("ID of the bookmark that owns the highlight")]
    public long? RaindropRef { get; init; }
}
