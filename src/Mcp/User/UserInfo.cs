using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.User;

/// <summary>
/// API response containing user information.
/// </summary>
public record UserResponse(bool Result, UserInfo User);

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
    public UserConfig? Config { get; init; }

    [Description("Linked Dropbox account information")]
    public DropboxInfo? Dropbox { get; init; }

    [Description("File storage information")]
    public FilesInfo? Files { get; init; }

    [Description("Account type")]
    public string? Type { get; init; }
}

/// <summary>
/// User configuration settings.
/// </summary>
[Description("User configuration settings")]
public record UserConfig
{
    [JsonPropertyName("broken_level")]
    [Description("Broken links finder configuration")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserBrokenLevel? BrokenLevel { get; init; }

    [JsonPropertyName("font_color")]
    [Description("Bookmark preview style")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserFontColor? FontColor { get; init; }

    [JsonPropertyName("font_size")]
    [Description("Bookmark preview font size (0 to 9)")]
    public int? FontSize { get; init; }

    [JsonPropertyName("lang")]
    [Description("UI language code")]
    public string? Lang { get; init; }

    [JsonPropertyName("last_collection")]
    [Description("ID of the last viewed collection")]
    public int? LastCollection { get; init; }

    [JsonPropertyName("raindrops_sort")]
    [Description("Default bookmark sort order")]
    public string? RaindropsSort { get; init; }

    [JsonPropertyName("raindrops_view")]
    [Description("Default bookmark view mode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRaindropsView? RaindropsView { get; init; }
}

/// <summary>
/// Dropbox integration status.
/// </summary>
[Description("Dropbox integration status")]
public record DropboxInfo
{
    [Description("Whether Dropbox backup is enabled")]
    public bool Enabled { get; init; }
}

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

public enum UserBrokenLevel
{
    [JsonStringEnumMemberName("basic")]
    Basic,
    [JsonStringEnumMemberName("default")]
    Default,
    [JsonStringEnumMemberName("strict")]
    Strict,
    [JsonStringEnumMemberName("off")]
    Off
}

public enum UserFontColor
{
    [JsonStringEnumMemberName("sunset")]
    Sunset,
    [JsonStringEnumMemberName("night")]
    Night,
    [JsonStringEnumMemberName("empty")]
    Empty
}

public enum UserRaindropsView
{
    [JsonStringEnumMemberName("grid")]
    Grid,
    [JsonStringEnumMemberName("list")]
    List,
    [JsonStringEnumMemberName("simple")]
    Simple,
    [JsonStringEnumMemberName("masonry")]
    Masonry
}
