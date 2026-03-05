using System.Text.Json.Serialization;

namespace Mcp.User;

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
