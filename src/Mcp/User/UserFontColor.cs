using System.Text.Json.Serialization;

namespace Mcp.User;

public enum UserFontColor
{
    [JsonStringEnumMemberName("sunset")]
    Sunset,
    [JsonStringEnumMemberName("night")]
    Night,
    [JsonStringEnumMemberName("empty")]
    Empty
}
