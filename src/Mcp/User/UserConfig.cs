using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Mcp.User;

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
