using System.Diagnostics;
using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Mcp.Tests;

public class RaindropsToolsPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IRaindropsApi> _apiMock;
    private readonly RaindropCacheService _cacheService;
    private readonly RaindropsTools _tools;

    public RaindropsToolsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _apiMock = new Mock<IRaindropsApi>();
        _cacheService = new RaindropCacheService();
        var options = Options.Create(new RaindropOptions { ApiToken = "test-token-for-performance-testing-longer-string-to-make-hashing-sligtly-more-expensive" });
        _tools = new RaindropsTools(_apiMock.Object, _cacheService, options);
    }

    [Fact]
    public async Task CreateBookmarksAsync_PerformanceBaseline()
    {
        // Arrange
        int itemCount = 5000; // 50 chunks
        var raindrops = Enumerable.Range(0, itemCount).Select(i => new Raindrop
        {
            Title = $"Title {i}",
            Link = $"https://example.com/{i}"
        }).ToList();

        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken _) =>
                new ItemsResponse<Raindrop>(true, req.Items));

        // Warm up
        await _tools.CreateBookmarksAsync(0, raindrops.Take(100), CancellationToken.None);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);
        sw.Stop();

        // Assert
        Assert.True(result.Result);
        Assert.Equal(itemCount, result.Items.Count);

        _output.WriteLine($"Time taken for {itemCount} items (50 chunks): {sw.Elapsed.TotalMilliseconds}ms");
    }
}
