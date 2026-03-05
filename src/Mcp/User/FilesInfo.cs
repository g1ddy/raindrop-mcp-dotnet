using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.User;

/// <summary>
/// File storage usage information.
/// </summary>
[Description("File storage usage information")]
public record FilesInfo
{
    [Description("Storage space used this month (bytes)")]
    public long? Used { get; init; }

    [Description("Total available storage space (bytes)")]
    public long? Size { get; init; }

    [Description("Date when upload quota was last reset")]
    [JsonPropertyName("lastCheckPoint")]
    public DateTimeOffset? LastCheckPoint { get; init; }
}
