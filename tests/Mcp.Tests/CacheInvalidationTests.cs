using Mcp.Common;
using Mcp.Collections;
using Xunit;

namespace Mcp.Tests;

public class CacheInvalidationTests
{
    [Fact]
    public async Task InvalidateAll_ShouldOnlyAffectSpecificUser()
    {
        var cacheService = new RaindropCacheService();
        var user1 = "user1-token";
        var user2 = "user2-token";

        int fetchCount1 = 0;
        int fetchCount2 = 0;

        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetch1 = _ => { fetchCount1++; return Task.FromResult(new ItemsResponse<Collection>(true, [])); };
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetch2 = _ => { fetchCount2++; return Task.FromResult(new ItemsResponse<Collection>(true, [])); };

        // Initial fetch
        await cacheService.GetCollectionsAsync(user1, fetch1, default);
        await cacheService.GetCollectionsAsync(user2, fetch2, default);

        Assert.Equal(1, fetchCount1);
        Assert.Equal(1, fetchCount2);

        // Fetch again - should be cached
        await cacheService.GetCollectionsAsync(user1, fetch1, default);
        await cacheService.GetCollectionsAsync(user2, fetch2, default);

        Assert.Equal(1, fetchCount1);
        Assert.Equal(1, fetchCount2);

        // New behavior: InvalidateAll only affects specific user
        cacheService.InvalidateAll(user1);

        await cacheService.GetCollectionsAsync(user1, fetch1, default);
        await cacheService.GetCollectionsAsync(user2, fetch2, default);

        // User1 should have been re-fetched, User2 should NOT
        Assert.Equal(2, fetchCount1);
        Assert.Equal(1, fetchCount2);
    }

    [Fact]
    public async Task InvalidateCollections_ShouldOnlyAffectSpecificUser()
    {
        var cacheService = new RaindropCacheService();
        var user1 = "user1-token";
        var user2 = "user2-token";

        int fetchCount1 = 0;
        int fetchCount2 = 0;

        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetch1 = _ => { fetchCount1++; return Task.FromResult(new ItemsResponse<Collection>(true, [])); };
        Func<CancellationToken, Task<ItemsResponse<Collection>>> fetch2 = _ => { fetchCount2++; return Task.FromResult(new ItemsResponse<Collection>(true, [])); };

        await cacheService.GetCollectionsAsync(user1, fetch1, default);
        await cacheService.GetCollectionsAsync(user2, fetch2, default);

        cacheService.InvalidateCollections(user1);

        await cacheService.GetCollectionsAsync(user1, fetch1, default);
        await cacheService.GetCollectionsAsync(user2, fetch2, default);

        Assert.Equal(2, fetchCount1);
        Assert.Equal(1, fetchCount2);
    }
}
