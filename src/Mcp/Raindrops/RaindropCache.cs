using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Raindrops;

/// <summary>
/// Permanent copy (cached version) details.
/// </summary>
[Description("Permanent copy (cached version) details")]
public record RaindropCache
{
    [JsonPropertyName("status")]
    [Description("Cache status: ready, retry, failed, invalid-origin, invalid-timeout, or invalid-size")]
    public string? Status { get; init; }

    [JsonPropertyName("size")]
    [Description("Full size in bytes")]
    public long? Size { get; init; }

    [JsonPropertyName("created")]
    [Description("Date when copy was successfully made")]
    public DateTime? Created { get; init; }
}
