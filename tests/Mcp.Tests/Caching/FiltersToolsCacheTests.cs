using Mcp.Filters;
using Mcp.Common;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mcp.Tests.Caching;

public class FiltersToolsCacheTests
{
    [Fact]
    public async Task GetAvailableFiltersAsync_UsesCacheAndDifferentiatesByParams()
    {
        // Arrange
        var mockApi = new Mock<IFiltersApi>();
        var cacheService = new RaindropCacheService();
        var options = Options.Create(new RaindropOptions { ApiToken = "test-token" });

        var tools = new FiltersTools(mockApi.Object, cacheService, options);

        mockApi.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, string? sort, string? search, CancellationToken ct) => new AvailableFilters { Result = true, Tags = new List<FilterEntry> { new() { Id = $"{id}-{sort}-{search}" } } });

        // Act & Assert 1: First call, hits API
        var result1 = await tools.GetAvailableFiltersAsync(1, "-count", "test", CancellationToken.None);
        Assert.True(result1.Result);
        mockApi.Verify(x => x.GetAsync(1, "-count", "test", It.IsAny<CancellationToken>()), Times.Once);

        // Act & Assert 2: Second call with SAME params, hits cache (API count remains 1)
        var result2 = await tools.GetAvailableFiltersAsync(1, "-count", "test", CancellationToken.None);
        Assert.True(result2.Result);
        mockApi.Verify(x => x.GetAsync(1, "-count", "test", It.IsAny<CancellationToken>()), Times.Once);

        // Act & Assert 3: Call with DIFFERENT collectionId, hits API (API count becomes 2)
        var result3 = await tools.GetAvailableFiltersAsync(2, "-count", "test", CancellationToken.None);
        Assert.True(result3.Result);
        mockApi.Verify(x => x.GetAsync(2, "-count", "test", It.IsAny<CancellationToken>()), Times.Once);

        // Act & Assert 4: Call with DIFFERENT tagsSort, hits API (API count becomes 3)
        var result4 = await tools.GetAvailableFiltersAsync(1, "_id", "test", CancellationToken.None);
        Assert.True(result4.Result);
        mockApi.Verify(x => x.GetAsync(1, "_id", "test", It.IsAny<CancellationToken>()), Times.Once);

        // Act & Assert 5: Call with DIFFERENT search, hits API (API count becomes 4)
        var result5 = await tools.GetAvailableFiltersAsync(1, "-count", "different", CancellationToken.None);
        Assert.True(result5.Result);
        mockApi.Verify(x => x.GetAsync(1, "-count", "different", It.IsAny<CancellationToken>()), Times.Once);
    }
}
