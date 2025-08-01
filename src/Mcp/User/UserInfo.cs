using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.User;

/// <summary>
/// Information about the authenticated Raindrop user.
/// </summary>
[Description("User account information")]
public record UserInfo
{
    [JsonPropertyName("_id")]
    [Description("User identifier")]
    public int Id { get; init; }

    [Description("User email address")]
    public string? Email { get; init; }

    [Description("Full name of the user")]
    public string? FullName { get; init; }

    [Description("Whether the user has a Pro subscription")]
    public bool Pro { get; init; }

    [Description("User configuration settings")]
    public object? Config { get; init; }

    [Description("Linked Dropbox account information")]
    public object? Dropbox { get; init; }

    [Description("File storage information")]
    public object? Files { get; init; }

    [Description("Account type")]
    public string? Type { get; init; }
}
