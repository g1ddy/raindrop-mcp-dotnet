using System.Text.Json.Serialization;

namespace Mcp.Raindrops;

public record MediaItem
{
    [JsonPropertyName("link")]
    public string Link { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }
}
