using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Mcp.Collections;
using Mcp.Tags;
using Mcp.User;
using Mcp.Filters;

namespace Mcp.Common;

/// <summary>
/// A singleton service that manages caching for Raindrop API responses.
/// This ensures thread-safe access and proper disposal of resources like semaphores.
/// caches are keyed by user identity (API token) to prevent data leakage in multi-user environments.
/// </summary>
public class RaindropCacheService : IRaindropCacheService
{
    private record CacheEntry<T>(T Response, DateTimeOffset Expiration);

    // Keyed by API Token
    private readonly ConcurrentDictionary<string, CacheEntry<ItemsResponse<Collection>>> _collectionsCache = new();
    private readonly SemaphoreSlim _collectionsLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CacheEntry<ItemsResponse<TagInfo>>> _tagsCache = new();
    private readonly SemaphoreSlim _tagsLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CacheEntry<ItemResponse<UserInfo>>> _userInfoCache = new();
    private readonly SemaphoreSlim _userInfoLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CacheEntry<AvailableFilters>> _filtersCache = new();
    private readonly SemaphoreSlim _filtersLock = new(1, 1);

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
            else if (response is AvailableFilters f) isSuccess = f.Result;

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
        if (response is AvailableFilters f)
        {
            var tags = f.Tags != null ? new List<FilterEntry>(f.Tags) : null;
            var types = f.Types != null ? new List<FilterEntry>(f.Types) : null;
            return (T)(object)(f with { Tags = tags, Types = types });
        }

        return response;
    }

    /// <summary>
    /// Computes a secure hash of the cache key (API token) to avoid storing it in memory.
    /// </summary>
    private static string ComputeCacheKey(string rawKey)
    {
        ArgumentNullException.ThrowIfNull(rawKey);

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(rawKey.Length);
        byte[]? rentedBytes = null;
        Span<byte> buffer = maxByteCount <= 512
            ? stackalloc byte[maxByteCount]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(maxByteCount));

        try
        {
            int byteCount = Encoding.UTF8.GetBytes(rawKey, buffer);
            Span<byte> hashBuffer = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(buffer.Slice(0, byteCount), hashBuffer);
            return Convert.ToHexString(hashBuffer);
        }
        finally
        {
            buffer.Clear();
            if (rentedBytes != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
            }
        }
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
    /// Gets the cached available filters or fetches them using the provided function.
    /// </summary>
    public Task<AvailableFilters> GetAvailableFiltersAsync(
        string key,
        long collectionId,
        string? tagsSort,
        string? search,
        Func<CancellationToken, Task<AvailableFilters>> fetchFunc,
        CancellationToken cancellationToken)
    {
        var rawKey = $"{key}_{collectionId}_{tagsSort ?? "null"}_{search ?? "null"}";
        return GetOrFetchAsync(ComputeCacheKey(rawKey), _filtersCache, _filtersLock, fetchFunc, cancellationToken);
    }

    /// <summary>
    /// Invalidates all caches for a specific user.
    /// </summary>
    /// <param name="key">The user's API token used as the cache key.</param>
    public Task InvalidateAllAsync(string key, CancellationToken cancellationToken = default)
    {
        var hashedKey = ComputeCacheKey(key);
        _collectionsCache.TryRemove(hashedKey, out _);
        _tagsCache.TryRemove(hashedKey, out _);
        _userInfoCache.TryRemove(hashedKey, out _);

        // We clear the entire filters cache since we don't store the raw keys to isolate by token,
        // and InvalidateAll is rare.
        _filtersCache.Clear();

        return Task.CompletedTask;
    }

    public Task InvalidateCollectionsAsync(string key, CancellationToken cancellationToken = default)
    {
        _collectionsCache.TryRemove(ComputeCacheKey(key), out _);
        return Task.CompletedTask;
    }

    public Task InvalidateTagsAsync(string key, CancellationToken cancellationToken = default)
    {
        _tagsCache.TryRemove(ComputeCacheKey(key), out _);
        return Task.CompletedTask;
    }

    public Task InvalidateUserInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        _userInfoCache.TryRemove(ComputeCacheKey(key), out _);
        return Task.CompletedTask;
    }

    public Task InvalidateFiltersAsync(string key, CancellationToken cancellationToken = default)
    {
        _filtersCache.Clear();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _collectionsLock.Dispose();
        _tagsLock.Dispose();
        _userInfoLock.Dispose();
        _filtersLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
