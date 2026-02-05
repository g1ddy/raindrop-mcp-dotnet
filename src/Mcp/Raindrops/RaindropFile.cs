using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Raindrops;

/// <summary>
/// File uploaded from desktop.
/// </summary>
[Description("File uploaded from desktop")]
public record RaindropFile
{
    [JsonPropertyName("name")]
    [Description("File name")]
    public string? Name { get; init; }

    [JsonPropertyName("size")]
    [Description("File size in bytes")]
    public long? Size { get; init; }

    [JsonPropertyName("type")]
    [Description("Mime type")]
    public string? Type { get; init; }
}
