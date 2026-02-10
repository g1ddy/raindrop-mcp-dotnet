using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.Raindrops;

/// <summary>
/// Reminder attached to the raindrop.
/// </summary>
[Description("Reminder attached to the raindrop")]
public record RaindropReminder
{
    [JsonPropertyName("date")]
    [Description("Reminder date")]
    public DateTime? Date { get; init; }
}
