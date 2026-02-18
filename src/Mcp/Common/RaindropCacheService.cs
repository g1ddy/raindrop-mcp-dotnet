using System.Diagnostics.CodeAnalysis;
using Mcp.Collections;
using Mcp.Tags;
using Mcp.User;

namespace Mcp.Common;

/// <summary>
/// A singleton service that manages caching for Raindrop API responses.
/// This ensures thread-safe access and proper disposal of resources like semaphores.
/// </summary>
public class RaindropCacheService : IDisposable
{
    private record CacheEntry<T>(ItemsResponse<T> Response, DateTimeOffset Expiration);
    private record CacheEntrySingle<T>(ItemResponse<T> Response, DateTimeOffset Expiration);

    private volatile CacheEntry<Collection>? _collectionsCache;
    private readonly SemaphoreSlim _collectionsLock = new(1, 1);

    private volatile CacheEntry<TagInfo>? _tagsCache;
    private readonly SemaphoreSlim _tagsLock = new(1, 1);

    private volatile CacheEntrySingle<UserInfo>? _userInfoCache;
    private readonly SemaphoreSlim _userInfoLock = new(1, 1);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the cached collections list or fetches it using the provided function.
    /// </summary>
    public async Task<ItemsResponse<Collection>> GetCollectionsAsync(
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetchFunc,
        CancellationToken cancellationToken)
    {
        if (TryGetValidCache(_collectionsCache, out var cached)) return cached;

        await _collectionsLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValidCache(_collectionsCache, out var lockedCached)) return lockedCached;

            var response = await fetchFunc(cancellationToken);
            if (response.Result && response.Items != null)
            {
                _collectionsCache = new CacheEntry<Collection>(response, DateTimeOffset.UtcNow.Add(CacheDuration));
                return response with { Items = [.. response.Items] };
            }
            return response;
        }
        finally
        {
            _collectionsLock.Release();
        }
    }

    /// <summary>
    /// Gets the cached tags list or fetches it using the provided function.
    /// </summary>
    public async Task<ItemsResponse<TagInfo>> GetTagsAsync(
        Func<CancellationToken, Task<ItemsResponse<TagInfo>>> fetchFunc,
        CancellationToken cancellationToken)
    {
        if (TryGetValidCache(_tagsCache, out var cached)) return cached;

        await _tagsLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValidCache(_tagsCache, out var lockedCached)) return lockedCached;

            var response = await fetchFunc(cancellationToken);
            if (response.Result && response.Items != null)
            {
                _tagsCache = new CacheEntry<TagInfo>(response, DateTimeOffset.UtcNow.Add(CacheDuration));
                return response with { Items = [.. response.Items] };
            }
            return response;
        }
        finally
        {
            _tagsLock.Release();
        }
    }

    /// <summary>
    /// Gets the cached user info or fetches it using the provided function.
    /// </summary>
    public async Task<ItemResponse<UserInfo>> GetUserInfoAsync(
        Func<CancellationToken, Task<ItemResponse<UserInfo>>> fetchFunc,
        CancellationToken cancellationToken)
    {
        if (TryGetValidCache(_userInfoCache, out var cached)) return cached;

        await _userInfoLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValidCache(_userInfoCache, out var lockedCached)) return lockedCached;

            var response = await fetchFunc(cancellationToken);
            if (response.Result && response.Item != null)
            {
                _userInfoCache = new CacheEntrySingle<UserInfo>(response, DateTimeOffset.UtcNow.Add(CacheDuration));
                return response with { };
            }
            return response;
        }
        finally
        {
            _userInfoLock.Release();
        }
    }

    private static bool TryGetValidCache<T>(CacheEntry<T>? entry, [NotNullWhen(true)] out ItemsResponse<T>? response)
    {
        if (entry != null && entry.Expiration > DateTimeOffset.UtcNow)
        {
            response = entry.Response with { Items = [.. entry.Response.Items] };
            return true;
        }
        response = null;
        return false;
    }

    private static bool TryGetValidCache<T>(CacheEntrySingle<T>? entry, [NotNullWhen(true)] out ItemResponse<T>? response)
    {
        if (entry != null && entry.Expiration > DateTimeOffset.UtcNow)
        {
            response = entry.Response with { };
            return true;
        }
        response = null;
        return false;
    }

    /// <summary>
    /// Invalidates the collections cache.
    /// </summary>
    public void InvalidateCollections() => _collectionsCache = null;

    /// <summary>
    /// Invalidates the tags cache.
    /// </summary>
    public void InvalidateTags() => _tagsCache = null;

    /// <summary>
    /// Invalidates the user info cache.
    /// </summary>
    public void InvalidateUserInfo() => _userInfoCache = null;

    /// <summary>
    /// Invalidates all caches.
    /// </summary>
    public void InvalidateAll()
    {
        InvalidateCollections();
        InvalidateTags();
        InvalidateUserInfo();
    }

    public void Dispose()
    {
        _collectionsLock.Dispose();
        _tagsLock.Dispose();
        _userInfoLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
