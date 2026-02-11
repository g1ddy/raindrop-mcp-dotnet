using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mcp.Common;

namespace Mcp.Raindrops;

/// <summary>
/// Represents a bookmark stored in Raindrop.io.
/// </summary>
[Description("Bookmark item")]
public record Raindrop
{
    /// <summary>
    /// Maximum length for text fields like Excerpt and Note.
    /// </summary>
    public const int MaxTextFieldLength = 10000;

    [JsonPropertyName("_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Description("Unique identifier of the bookmark")]
    public long Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Title of the bookmarked page")]
    public string? Title { get; init; }

    [Required]
    [Url]
    [Description("URL of the page")]
    public string Link { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [MaxLength(MaxTextFieldLength)]
    [Description("Excerpt from the page")]
    public string? Excerpt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [MaxLength(MaxTextFieldLength)]
    [Description("Personal note for the bookmark")]
    public string? Note { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Associated tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    [Description("Indicates a favorite bookmark")]
    public bool? Important { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Reference to the parent collection")]
    public IdRef? Collection { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Identifier of the parent collection")]
    public int? CollectionId { get; init; }
}
