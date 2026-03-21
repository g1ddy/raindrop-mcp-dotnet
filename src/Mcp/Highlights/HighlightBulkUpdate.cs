using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mcp.Highlights;

/// <summary>
/// Represents a single highlight change used in bulk update operations.
/// </summary>
[Description("Highlight update payload")]
public record HighlightBulkUpdate
{
    [JsonPropertyName("_id")]
    [Description("Identifier of the highlight")]
    public string? Id { get; init; }

    [MaxLength(Raindrops.Raindrop.MaxTextFieldLength)]
    [Description("Updated highlight text or empty to delete")]
    public string? Text { get; init; }

    [MaxLength(Raindrops.Raindrop.MaxTextFieldLength)]
    [Description("Updated note text")]
    public string? Note { get; init; }

    [Description("Highlight color")]
    public string? Color { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Creation timestamp")]
    public string? Created { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Last update timestamp")]
    public string? LastUpdate { get; init; }
}
