using System;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Highlights;
using Moq;
using Xunit;

namespace Mcp.Tests;

public class HighlightsToolsTests
{
    private readonly Mock<IHighlightsApi> _apiMock;
    private readonly HighlightsTools _tools;

    public HighlightsToolsTests()
    {
        _apiMock = new Mock<IHighlightsApi>();
        _tools = new HighlightsTools(_apiMock.Object);
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, 51)]
    [InlineData(-1, 0)]
    public async Task ListHighlightsAsync_InvalidPagination_ThrowsArgumentOutOfRangeException(int? page, int? perPage)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _tools.ListHighlightsAsync(page, perPage, CancellationToken.None));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, 51)]
    [InlineData(-1, 51)]
    public async Task ListHighlightsByCollectionAsync_InvalidPagination_ThrowsArgumentOutOfRangeException(int? page, int? perPage)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _tools.ListHighlightsByCollectionAsync(123, page, perPage, CancellationToken.None));
    }
}
