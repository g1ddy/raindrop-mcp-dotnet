using Mcp.Common;
using Mcp.Raindrops;
using Moq;
using Xunit;

namespace Mcp.Tests;

public class RaindropsChunkingTests
{
    private readonly Mock<IRaindropsApi> _apiMock;
    private readonly RaindropsTools _tools;

    public RaindropsChunkingTests()
    {
        _apiMock = new Mock<IRaindropsApi>();
        _tools = new RaindropsTools(_apiMock.Object);
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

    private IEnumerable<Raindrop> CreateRaindrops(int count)
    {
        return Enumerable.Range(0, count).Select(i => new Raindrop
        {
            Title = $"Title {i}",
            Link = $"https://example.com/{i}"
        });
    }
}
