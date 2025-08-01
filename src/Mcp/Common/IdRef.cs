using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Common;

/// <summary>
/// Represents a reference to another Raindrop entity by its identifier.
/// </summary>
[Description("Reference to another entity by its identifier")]
public record IdRef
{
    [JsonPropertyName("$id")]
    [Description("Unique identifier of the referenced object")]
    public int Id { get; init; }
}
