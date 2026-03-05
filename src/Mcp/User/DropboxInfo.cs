using System.ComponentModel;

namespace Mcp.User;

/// <summary>
/// Dropbox integration status.
/// </summary>
[Description("Dropbox integration status")]
public record DropboxInfo
{
    [Description("Whether Dropbox backup is enabled")]
    public bool Enabled { get; init; }
}
