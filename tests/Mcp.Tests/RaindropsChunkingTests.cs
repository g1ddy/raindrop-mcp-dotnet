using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mcp.Tests;

public class RaindropsChunkingTests
{
    private readonly Mock<IRaindropsApi> _apiMock;
    private readonly Mock<IRaindropCacheService> _cacheMock;
    private readonly RaindropsTools _tools;

    public RaindropsChunkingTests()
    {
        _apiMock = new Mock<IRaindropsApi>();
        _cacheMock = new Mock<IRaindropCacheService>();
        var options = Options.Create(new RaindropOptions { ApiToken = "test-token" });
        _tools = new RaindropsTools(_apiMock.Object, _cacheMock.Object, options);
    }

    [Fact]
    public async Task CreateBookmarksAsync_WithSmallBatch_CallsApiOnce()
    {
        // Arrange
        var raindrops = CreateRaindrops(50);
        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Raindrop>(true, raindrops.ToList()));

        // Act
        await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        _apiMock.Verify(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _apiMock.Verify(x => x.CreateManyAsync(It.Is<RaindropCreateManyRequest>(r => r.Items.Count == 50), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookmarksAsync_WithLargeBatch_CallsApiMultipleTimes()
    {
        // Arrange
        var raindrops = CreateRaindrops(150);
        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken _) =>
                new ItemsResponse<Raindrop>(true, req.Items));

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        // Expecting 2 calls: one with 100 items, one with 50 items.
        _apiMock.Verify(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _apiMock.Verify(x => x.CreateManyAsync(It.Is<RaindropCreateManyRequest>(r => r.Items.Count == 100), It.IsAny<CancellationToken>()), Times.Once);
        _apiMock.Verify(x => x.CreateManyAsync(It.Is<RaindropCreateManyRequest>(r => r.Items.Count == 50), It.IsAny<CancellationToken>()), Times.Once);

        Assert.True(result.Result);
        Assert.Equal(150, result.Items.Count);
    }

    [Fact]
    public async Task CreateBookmarksAsync_AggregatesResults()
    {
        // Arrange
        var raindrops = CreateRaindrops(250); // 3 chunks: 100, 100, 50

        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken _) =>
                new ItemsResponse<Raindrop>(true, req.Items));

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        _apiMock.Verify(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.True(result.Result);
        Assert.Equal(250, result.Items.Count);
        // Verify order is preserved
        for (int i = 0; i < 250; i++)
        {
            Assert.Equal($"Title {i}", result.Items[i].Title);
        }
    }

    [Fact]
    public async Task CreateBookmarksAsync_WhenChunkFails_ReturnsPartialSuccess()
    {
        // Arrange
        var raindrops = CreateRaindrops(250); // 3 chunks

        _apiMock.Setup(api => api.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken ct) =>
            {
                if (req.Items.Count == 100 && req.Items[0].Title == "Title 100") // 2nd chunk fails
                    return new ItemsResponse<Raindrop>(false, Array.Empty<Raindrop>());

                return new ItemsResponse<Raindrop>(true, req.Items); // 1st and 3rd succeed
            });

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        Assert.False(result.Result); // Overall result is false because one failed
        Assert.Equal(150, result.Items.Count); // 100 from chunk 1, 50 from chunk 3
        _apiMock.Verify(api => api.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateBookmarksAsync_PartialFailure_InvalidatesCache()
    {
        // Arrange
        var raindrops = CreateRaindrops(150); // 2 chunks: 100 success, 50 failure

        _apiMock.Setup(api => api.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken ct) =>
            {
                if (req.Items.Count == 50) // 2nd chunk fails
                    return new ItemsResponse<Raindrop>(false, Array.Empty<Raindrop>());

                return new ItemsResponse<Raindrop>(true, req.Items); // 1st succeed
            });

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        Assert.False(result.Result);
        Assert.Equal(100, result.Items.Count);
        // Cache SHOULD be invalidated if we successfully created ANY items (100 items here)
        _cacheMock.Verify(x => x.InvalidateAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookmarksAsync_Success_InvalidatesCache()
    {
        // Arrange
        var raindrops = CreateRaindrops(150);
        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken _) =>
                new ItemsResponse<Raindrop>(true, req.Items));

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        Assert.True(result.Result);
        Assert.Equal(150, result.Items.Count);
        _cacheMock.Verify(x => x.InvalidateAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private IEnumerable<Raindrop> CreateRaindrops(int count)
    {
        return Enumerable.Range(0, count).Select(i => new Raindrop
        {
            Title = $"Title {i}",
            Link = $"https://example.com/{i}"
        });
    }

    [Fact]
    public async Task CreateBookmarksAsync_WithList_UsesOptimizedPath()
    {
        // Arrange
        // Using a List ensures TryGetNonEnumeratedCount returns true
        var raindrops = CreateRaindrops(150).ToList();

        _apiMock.Setup(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaindropCreateManyRequest req, CancellationToken _) =>
                new ItemsResponse<Raindrop>(true, req.Items));

        // Act
        var result = await _tools.CreateBookmarksAsync(0, raindrops, CancellationToken.None);

        // Assert
        Assert.True(result.Result);
        Assert.Equal(150, result.Items.Count);
        _apiMock.Verify(x => x.CreateManyAsync(It.IsAny<RaindropCreateManyRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
