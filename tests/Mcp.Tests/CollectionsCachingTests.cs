using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Common;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using Xunit;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mcp.Tests;

public class CollectionsCachingTests
{
    private readonly Mock<ICollectionsApi> _collectionsApiMock;
    private readonly Mock<IRaindropsApi> _raindropsApiMock;
    private readonly Mock<McpServer> _mcpServerMock;
    private readonly CollectionsTools _tools;

    public CollectionsCachingTests()
    {
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _mcpServerMock = new Mock<McpServer>();
        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object);

        // Setup default responses
        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection>
            {
                new Collection { Id = 1, Title = "Tech" }
            }));

        _raindropsApiMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, new Raindrop { Id = 123, Title = "Test", Link = "url" }));

        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "Tech" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.Is<JsonRpcRequest>(r => r.Method == "sampling/createMessage"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse { Result = JsonSerializer.SerializeToNode(llmResponse) });

        var elicitResult = new ElicitResult
        {
            Action = "accept",
            Content = new Dictionary<string, JsonElement>
            {
                ["collectionName"] = JsonSerializer.SerializeToElement("Tech")
            }
        };
        _mcpServerMock.Setup(x => x.SendRequestAsync(It.Is<JsonRpcRequest>(r => r.Method == "elicitation/create"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse { Result = JsonSerializer.SerializeToNode(elicitResult) });

        _raindropsApiMock.Setup(x => x.UpdateAsync(It.IsAny<long>(), It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, new Raindrop()));
    }

    [Fact]
    public async Task ListCollectionsAsync_CalledMultipleTimes_CallsApiOnlyOnce()
    {
        // Act
        await _tools.ListCollectionsAsync(CancellationToken.None);
        await _tools.ListCollectionsAsync(CancellationToken.None);
        await _tools.ListCollectionsAsync(CancellationToken.None);

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_CalledMultipleTimes_CallsApiOnlyOnce()
    {
        // Act
        await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, 123, CancellationToken.None);
        await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, 123, CancellationToken.None);

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Cache_IsInvalidated_OnCreateCollection()
    {
        // Arrange
        await _tools.ListCollectionsAsync(CancellationToken.None); // Populates cache
        _collectionsApiMock.Setup(x => x.CreateAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Collection>(true, new Collection()));

        // Act
        await _tools.CreateCollectionAsync(new Collection(), CancellationToken.None); // Invalidates cache
        await _tools.ListCollectionsAsync(CancellationToken.None); // Triggers new API call

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_DoesNotCache_FailedApiCalls()
    {
        // Arrange
        _collectionsApiMock.SetupSequence(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(false, new List<Collection>())) // First call fails
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection> { new Collection { Id = 1 } })); // Second call succeeds

        // Act
        await _tools.ListCollectionsAsync(CancellationToken.None); // Fails, should not cache
        await _tools.ListCollectionsAsync(CancellationToken.None); // Succeeds, should cache
        await _tools.ListCollectionsAsync(CancellationToken.None); // Hits cache

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_IsInvalidated_OnUpdateCollection()
    {
        // Arrange
        await _tools.ListCollectionsAsync(CancellationToken.None);
        _collectionsApiMock.Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Collection>(true, new Collection()));

        // Act
        await _tools.UpdateCollectionAsync(1, new Collection(), CancellationToken.None);
        await _tools.ListCollectionsAsync(CancellationToken.None);

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_IsInvalidated_OnDeleteCollection()
    {
        // Arrange
        await _tools.ListCollectionsAsync(CancellationToken.None);
        _collectionsApiMock.Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        // Act
        await _tools.DeleteCollectionAsync(1, CancellationToken.None);
        await _tools.ListCollectionsAsync(CancellationToken.None);

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_IsInvalidated_OnMergeCollections()
    {
        // Arrange
        await _tools.ListCollectionsAsync(CancellationToken.None);
        _collectionsApiMock.Setup(x => x.MergeAsync(It.IsAny<CollectionsMergeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse(true));

        // Act
        await _tools.MergeCollectionsAsync(1, new HashSet<int> { 2, 3 }, CancellationToken.None);
        await _tools.ListCollectionsAsync(CancellationToken.None);

        // Assert
        _collectionsApiMock.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Cache_ReturnsClonedList_ToAvoidReferenceSharing()
    {
        // Arrange
        var initialList = new List<Collection> { new Collection { Id = 1, Title = "Initial" } };
        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, initialList));

        // Act
        var response1 = await _tools.ListCollectionsAsync(CancellationToken.None);
        response1.Items.Add(new Collection { Id = 2, Title = "Modified" });

        var response2 = await _tools.ListCollectionsAsync(CancellationToken.None);

        // Assert
        Assert.Single(response2.Items);
        Assert.Equal("Initial", response2.Items[0].Title);
        Assert.NotSame(response1.Items, response2.Items);
    }
}
