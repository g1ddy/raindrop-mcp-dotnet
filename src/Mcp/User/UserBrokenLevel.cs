using System.Text.Json.Serialization;

namespace Mcp.User;

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
