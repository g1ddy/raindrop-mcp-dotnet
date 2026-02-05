using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mcp.Common;
using Mcp.Highlights;

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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Url]
    [Description("URL of the page")]
    public string? Link { get; init; }

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
    [Description("Type of the bookmark: link, article, image, video, document, or audio")]
    public string? Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Raindrop cover URL")]
    public string? Cover { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("List of media attachments (covers)")]
    public IReadOnlyList<MediaItem>? Media { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Creation date")]
    public DateTime? Created { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Last update date")]
    public DateTime? LastUpdate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Hostname of the link")]
    public string? Domain { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Marked as broken")]
    public bool? Broken { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Permanent copy (cached version) details")]
    public RaindropCache? Cache { get; init; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("File uploaded from desktop")]
    public RaindropFile? File { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("List of highlights")]
    public IReadOnlyList<Highlight>? Highlights { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Reminder attached to the raindrop")]
    public RaindropReminder? Reminder { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Raindrop owner")]
    public IdRef? User { get; init; }
}
