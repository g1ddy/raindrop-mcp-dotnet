using Mcp.Collections.Suggestions;
using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mcp.Tests;

public class RaindropsSuggestionInvalidationTests
{
    private readonly Mock<IRaindropsApi> _apiMock = new();
    private readonly Mock<ICollectionSuggestionService> _suggestionServiceMock = new();
    private readonly RaindropsTools _tools;

    public RaindropsSuggestionInvalidationTests()
    {
        _tools = new RaindropsTools(
            _apiMock.Object,
            Mock.Of<IRaindropCacheService>(),
            _suggestionServiceMock.Object,
            Options.Create(new RaindropOptions { ApiToken = "test-token" }));
    }

    [Fact]
    public async Task CreateBookmarkAsync_WhenSuccessful_InvalidatesSuggestionIndex()
    {
        _apiMock.Setup(x => x.CreateAsync(It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, new Raindrop()));

        await _tools.CreateBookmarkAsync(new RaindropCreateRequest(), CancellationToken.None);

        _suggestionServiceMock.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task UpdateBookmarkAsync_WhenSuccessful_InvalidatesSuggestionIndex()
    {
        _apiMock.Setup(x => x.UpdateAsync(It.IsAny<long>(), It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, new Raindrop()));

        await _tools.UpdateBookmarkAsync(1, new RaindropUpdateRequest(), CancellationToken.None);

        _suggestionServiceMock.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task DeleteBookmarkAsync_WhenSuccessful_InvalidatesSuggestionIndex()
    {
        _apiMock.Setup(x => x.DeleteAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        await _tools.DeleteBookmarkAsync(1, CancellationToken.None);

        _suggestionServiceMock.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task UpdateBookmarksAsync_WhenSuccessful_InvalidatesSuggestionIndex()
    {
        _apiMock.Setup(x => x.UpdateManyAsync(
                It.IsAny<int>(),
                It.IsAny<RaindropBulkUpdate>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        await _tools.UpdateBookmarksAsync(1, new RaindropBulkUpdate(), cancellationToken: CancellationToken.None);

        _suggestionServiceMock.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task Mutation_WhenUnsuccessful_DoesNotInvalidateSuggestionIndex()
    {
        _apiMock.Setup(x => x.DeleteAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(false));

        await _tools.DeleteBookmarkAsync(1, CancellationToken.None);

        _suggestionServiceMock.Verify(x => x.Invalidate(), Times.Never);
    }
}
