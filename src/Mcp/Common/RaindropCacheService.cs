using System.Diagnostics.CodeAnalysis;
using Mcp.Collections;
using Mcp.Tags;

namespace Mcp.Common;

/// <summary>
/// A singleton service that manages caching for Raindrop API responses.
/// This ensures thread-safe access and proper disposal of resources like semaphores.
/// </summary>
public class RaindropCacheService : IDisposable
{
    private record CacheEntry<T>(ItemsResponse<T> Response, DateTimeOffset Expiration);

    private volatile CacheEntry<Collection>? _collectionsCache;
    private readonly SemaphoreSlim _collectionsLock = new(1, 1);

    private volatile CacheEntry<TagInfo>? _tagsCache;
    private readonly SemaphoreSlim _tagsLock = new(1, 1);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the cached collections list or fetches it using the provided function.
    /// </summary>
    public Task<ItemsResponse<Collection>> GetCollectionsAsync(
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetchFunc,
        CancellationToken cancellationToken)
        => GetCachedAsync(_collectionsLock, () => _collectionsCache, v => _collectionsCache = v, fetchFunc, cancellationToken);

    /// <summary>
    /// Gets the cached tags list or fetches it using the provided function.
    /// </summary>
    public Task<ItemsResponse<TagInfo>> GetTagsAsync(
        Func<CancellationToken, Task<ItemsResponse<TagInfo>>> fetchFunc,
        CancellationToken cancellationToken)
        => GetCachedAsync(_tagsLock, () => _tagsCache, v => _tagsCache = v, fetchFunc, cancellationToken);

    private async Task<ItemsResponse<T>> GetCachedAsync<T>(
        SemaphoreSlim semaphore,
        Func<CacheEntry<T>?> getCache,
        Action<CacheEntry<T>?> setCache,
        Func<CancellationToken, Task<ItemsResponse<T>>> fetchFunc,
        CancellationToken cancellationToken)
    {
        if (TryGetValidCache(getCache(), out var cached)) return cached;

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValidCache(getCache(), out var lockedCached)) return lockedCached;

            var response = await fetchFunc(cancellationToken);
            if (response.Result && response.Items != null)
            {
                setCache(new CacheEntry<T>(response, DateTimeOffset.UtcNow.Add(CacheDuration)));
                return response with { Items = [.. response.Items] };
            }
            return response;
        }
        finally
        {
            semaphore.Release();
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

    /// <summary>
    /// Invalidates the collections cache.
    /// </summary>
    public void InvalidateCollections() => _collectionsCache = null;

    /// <summary>
    /// Invalidates the tags cache.
    /// </summary>
    public void InvalidateTags() => _tagsCache = null;

    /// <summary>
    /// Invalidates both caches.
    /// </summary>
    public void InvalidateAll()
    {
        InvalidateCollections();
        InvalidateTags();
    }

    public void Dispose()
    {
        _collectionsLock.Dispose();
        _tagsLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
