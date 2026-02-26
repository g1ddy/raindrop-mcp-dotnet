using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Mcp.Collections;
using Mcp.Tags;
using Mcp.User;

namespace Mcp.Common;

/// <summary>
/// A singleton service that manages caching for Raindrop API responses.
/// This ensures thread-safe access and proper disposal of resources like semaphores.
/// caches are keyed by user identity (API token) to prevent data leakage in multi-user environments.
/// </summary>
public class RaindropCacheService : IDisposable
{
    private record CacheEntry<T>(T Response, DateTimeOffset Expiration);

    // Keyed by API Token
    private readonly ConcurrentDictionary<string, CacheEntry<ItemsResponse<Collection>>> _collectionsCache = new();
    private readonly SemaphoreSlim _collectionsLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CacheEntry<ItemsResponse<TagInfo>>> _tagsCache = new();
    private readonly SemaphoreSlim _tagsLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CacheEntry<ItemResponse<UserInfo>>> _userInfoCache = new();
    private readonly SemaphoreSlim _userInfoLock = new(1, 1);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Generic method to handle caching logic with double-checked locking.
    /// </summary>
    private async Task<TResponse> GetOrFetchAsync<TResponse>(
        string key,
        ConcurrentDictionary<string, CacheEntry<TResponse>> cache,
        SemaphoreSlim semaphore,
        Func<CancellationToken, Task<TResponse>> fetchFunc,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (TryGetValidCache(key, cache, out var cached)) return cached;

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValidCache(key, cache, out var lockedCached)) return lockedCached;

            var response = await fetchFunc(cancellationToken);

            // Check if response is successful before caching
            bool isSuccess = false;
            if (response is ItemsResponse<Collection> c) isSuccess = c.Result && c.Items != null;
            else if (response is ItemsResponse<TagInfo> t) isSuccess = t.Result && t.Items != null;
            else if (response is ItemResponse<UserInfo> u) isSuccess = u.Result && u.Item != null;

            if (isSuccess)
            {
                var entry = new CacheEntry<TResponse>(response, DateTimeOffset.UtcNow.Add(CacheDuration));
                cache[key] = entry;
                return CloneResponse(response);
            }
            return response;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static bool TryGetValidCache<T>(
        string key,
        ConcurrentDictionary<string, CacheEntry<T>> cache,
        [NotNullWhen(true)] out T? response) where T : class
    {
        if (cache.TryGetValue(key, out var entry) && entry.Expiration > DateTimeOffset.UtcNow && entry.Response is not null)
        {
            response = CloneResponse(entry.Response);
            return true;
        }
        response = default;
        return false;
    }

    // Helper to clone responses to avoid modifying the cached instance
    private static T CloneResponse<T>(T response)
    {
        if (response is ItemsResponse<Collection> c)
            return (T)(object)(c with { Items = [.. c.Items] });
        if (response is ItemsResponse<TagInfo> t)
            return (T)(object)(t with { Items = [.. t.Items] });
        if (response is ItemResponse<UserInfo> u)
            return (T)(object)(u with { }); // Shallow copy is enough for records if properties are immutable

        return response;
    }

    /// <summary>
    /// Computes a secure hash of the cache key (API token) to avoid storing it in memory.
    /// </summary>
    private static string ComputeCacheKey(string rawKey)
    {
        ArgumentNullException.ThrowIfNull(rawKey);
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Gets the cached collections list or fetches it using the provided function.
    /// </summary>
    public Task<ItemsResponse<Collection>> GetCollectionsAsync(
        string key,
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetchFunc,
        CancellationToken cancellationToken)
        => GetOrFetchAsync(ComputeCacheKey(key), _collectionsCache, _collectionsLock, fetchFunc, cancellationToken);

    /// <summary>
    /// Gets the cached tags list or fetches it using the provided function.
    /// </summary>
    public Task<ItemsResponse<TagInfo>> GetTagsAsync(
        string key,
        Func<CancellationToken, Task<ItemsResponse<TagInfo>>> fetchFunc,
        CancellationToken cancellationToken)
        => GetOrFetchAsync(ComputeCacheKey(key), _tagsCache, _tagsLock, fetchFunc, cancellationToken);

    /// <summary>
    /// Gets the cached user info or fetches it using the provided function.
    /// </summary>
    public Task<ItemResponse<UserInfo>> GetUserInfoAsync(
        string key,
        Func<CancellationToken, Task<ItemResponse<UserInfo>>> fetchFunc,
        CancellationToken cancellationToken)
        => GetOrFetchAsync(ComputeCacheKey(key), _userInfoCache, _userInfoLock, fetchFunc, cancellationToken);

    /// <summary>
    /// Invalidates all caches for a specific user.
    /// </summary>
    /// <param name="key">The user's API token used as the cache key.</param>
    public void InvalidateAll(string key)
    {
        var hashedKey = ComputeCacheKey(key);
        _collectionsCache.TryRemove(hashedKey, out _);
        _tagsCache.TryRemove(hashedKey, out _);
        _userInfoCache.TryRemove(hashedKey, out _);
    }

    public void InvalidateCollections(string key) => _collectionsCache.TryRemove(ComputeCacheKey(key), out _);
    public void InvalidateTags(string key) => _tagsCache.TryRemove(ComputeCacheKey(key), out _);
    public void InvalidateUserInfo(string key) => _userInfoCache.TryRemove(ComputeCacheKey(key), out _);

    public void Dispose()
    {
        _collectionsLock.Dispose();
        _tagsLock.Dispose();
        _userInfoLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
