using Mcp.Collections;
using Mcp.Tags;
using Mcp.User;

namespace Mcp.Common;

/// <summary>
/// Interface for the Raindrop API cache service.
/// </summary>
public interface IRaindropCacheService : IDisposable
{
    /// <summary>
    /// Gets the cached collections list or fetches it using the provided function.
    /// </summary>
    Task<ItemsResponse<Collection>> GetCollectionsAsync(
        string key,
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetchFunc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the cached tags list or fetches it using the provided function.
    /// </summary>
    Task<ItemsResponse<TagInfo>> GetTagsAsync(
        string key,
        Func<CancellationToken, Task<ItemsResponse<TagInfo>>> fetchFunc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the cached user info or fetches it using the provided function.
    /// </summary>
    Task<ItemResponse<UserInfo>> GetUserInfoAsync(
        string key,
        Func<CancellationToken, Task<ItemResponse<UserInfo>>> fetchFunc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates all caches for a specific user.
    /// </summary>
    void InvalidateAll(string key);

    /// <summary>
    /// Invalidates collections cache for a specific user.
    /// </summary>
    void InvalidateCollections(string key);

    /// <summary>
    /// Invalidates tags cache for a specific user.
    /// </summary>
    void InvalidateTags(string key);

    /// <summary>
    /// Invalidates user info cache for a specific user.
    /// </summary>
    void InvalidateUserInfo(string key);
}
