using System.Text.Json;
using System.Text.Json.Nodes;
using Mcp.Collections;
using Mcp.Collections.Suggestions;
using Mcp.Common;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Mcp.Tests;

[Collection("Sequential")]
public class CollectionsToolsTests
{
    private readonly Mock<ICollectionsApi> _collectionsApiMock = new();
    private readonly Mock<IRaindropsApi> _raindropsApiMock = new();
    private readonly Mock<ICollectionSuggestionService> _suggestionServiceMock = new();
    private readonly Mock<McpServer> _mcpServerMock = new();
    private readonly CollectionsTools _tools;

    public CollectionsToolsTests()
    {
        var options = Options.Create(new RaindropOptions { ApiToken = "dummy-token" });
        _tools = new CollectionsTools(
            _collectionsApiMock.Object,
            _raindropsApiMock.Object,
            new RaindropCacheService(),
            _suggestionServiceMock.Object,
            options);
        _mcpServerMock.Setup(server => server.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });
    }

    [Theory]
    [InlineData("Simple Text", "Simple Text")]
    [InlineData("Text with | pipe", "Text with  pipe")]
    [InlineData("Line\nBreak", "Line Break")]
    [InlineData("Line\rBreak", "Line Break")]
    [InlineData("Line\r\nBreak", "Line Break")]
    [InlineData("Line\vBreak", "Line Break")]
    [InlineData("Line\fBreak", "Line Break")]
    [InlineData("Line\u0085Break", "Line Break")]
    [InlineData("Line\u2028Break", "Line Break")]
    [InlineData("Line\u2029Break", "Line Break")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Sanitize_HandlesVariousCharacters(string? input, string expected) =>
        Assert.Equal(expected, CollectionsTools.Sanitize(input));

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_ReturnsFalse_WhenClassifierHasNoSuggestions()
    {
        var bookmark = ArrangeBookmarkAndCollections();
        _suggestionServiceMock
            .Setup(service => service.SuggestAsync(bookmark, It.IsAny<IReadOnlyCollection<Collection>>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmark.Id, CancellationToken.None);

        Assert.False(result.Result);
        _mcpServerMock.Verify(
            server => server.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_MovesBookmarkToAcceptedSuggestion()
    {
        var bookmark = ArrangeBookmarkAndCollections();
        var tech = new Collection { Id = 1, Title = "Science, Tech & Nature" };
        _suggestionServiceMock
            .Setup(service => service.SuggestAsync(bookmark, It.IsAny<IReadOnlyCollection<Collection>>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CollectionSuggestion(tech, 0.9)]);
        SetupElicitation("Science, Tech & Nature");
        _raindropsApiMock
            .Setup(api => api.UpdateAsync(bookmark.Id, It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmark.Id, CancellationToken.None);

        Assert.True(result.Result);
        _raindropsApiMock.Verify(api => api.UpdateAsync(
            bookmark.Id,
            It.Is<Raindrop>(update => update.Collection != null && update.Collection.Id == tech.Id),
            It.IsAny<CancellationToken>()));
        _suggestionServiceMock.Verify(service => service.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_DoesNotMoveBookmarkWhenUserDeclines()
    {
        var bookmark = ArrangeBookmarkAndCollections();
        var tech = new Collection { Id = 1, Title = "Tech" };
        _suggestionServiceMock
            .Setup(service => service.SuggestAsync(bookmark, It.IsAny<IReadOnlyCollection<Collection>>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CollectionSuggestion(tech, 0.8)]);
        SetupElicitation(null);

        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmark.Id, CancellationToken.None);

        Assert.False(result.Result);
        _raindropsApiMock.Verify(
            api => api.UpdateAsync(It.IsAny<long>(), It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Raindrop ArrangeBookmarkAndCollections()
    {
        var bookmark = new Raindrop { Id = 123, Title = "Test", Link = "https://example.com" };
        _raindropsApiMock.Setup(api => api.GetAsync(bookmark.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));
        _collectionsApiMock.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true,
            [
                new Collection { Id = 1, Title = "Science, Tech & Nature" },
                new Collection { Id = 2, Title = "News" }
            ]));
        return bookmark;
    }

    private void SetupElicitation(string? selectedCollection)
    {
        _mcpServerMock.Setup(server => server.SendRequestAsync(
                It.Is<JsonRpcRequest>(request => request.Method == "elicitation/create"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse
            {
                Result = JsonSerializer.SerializeToNode(new ElicitResult
                {
                    Action = selectedCollection is null ? "decline" : "accept",
                    Content = selectedCollection is null
                        ? null
                        : new Dictionary<string, JsonElement>
                        {
                            ["collectionName"] = JsonSerializer.SerializeToElement(selectedCollection)
                        }
                })
            });
    }
}
