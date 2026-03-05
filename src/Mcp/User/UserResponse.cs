namespace Mcp.User;

/// <summary>
/// API response containing user information.
/// </summary>
public record UserResponse(bool Result, UserInfo User);
