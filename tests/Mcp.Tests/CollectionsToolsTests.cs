using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Common;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using Xunit;
using System.Text.Json;
using Xunit.Abstractions;
using System.Text.Json.Nodes;

namespace Mcp.Tests;

[Collection("Sequential")]
public class CollectionsToolsTests
{
    private readonly Mock<ICollectionsApi> _collectionsApiMock;
    private readonly Mock<IRaindropsApi> _raindropsApiMock;
    private readonly Mock<McpServer> _mcpServerMock;
    private readonly CollectionsTools _tools;
    private readonly ITestOutputHelper _output;

    public CollectionsToolsTests(ITestOutputHelper output)
    {
        CollectionsTools.InvalidateCache();
        _output = output;
        _collectionsApiMock = new Mock<ICollectionsApi>();
        _raindropsApiMock = new Mock<IRaindropsApi>();
        _mcpServerMock = new Mock<McpServer>();
        _tools = new CollectionsTools(_collectionsApiMock.Object, _raindropsApiMock.Object);
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
    public void Sanitize_HandlesVariousCharacters(string? input, string expected)
    {
        // Act
        var result = CollectionsTools.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_ReturnsFalse_WhenParsingFails()
    {
        // Arrange
        long bookmarkId = 123;
        var bookmark = new Raindrop { Id = bookmarkId, Title = "Test", Link = "url", Excerpt = "excerpt" };

        _raindropsApiMock.Setup(x => x.GetAsync(bookmarkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection>
            {
                new Collection { Id = 1, Title = "Tech" }
            }));

        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "This is a sentence, not a list." }],
            Model = "test-model"
        };

        // Mock ClientCapabilities to support sampling
        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        // Mock SendRequestAsync to return the LLM response
        _mcpServerMock.Setup(x => x.SendRequestAsync(It.Is<JsonRpcRequest>(r => r.Method == "sampling/createMessage"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse
            {
                Result = JsonSerializer.SerializeToNode(llmResponse)
            });

        // Act
        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmarkId, CancellationToken.None);

        // Assert
        Assert.False(result.Result);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_HandlesBulletPoints()
    {
        // Arrange
        long bookmarkId = 123;
        var bookmark = new Raindrop { Id = bookmarkId, Title = "Test", Link = "url", Excerpt = "excerpt" };

        _raindropsApiMock.Setup(x => x.GetAsync(bookmarkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection>
            {
                new Collection { Id = 1, Title = "Tech" },
                new Collection { Id = 2, Title = "News" }
            }));

        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "- Tech\n- News" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        // Spy on SendRequestAsync
        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonRpcRequest r, CancellationToken t) =>
            {
                _output.WriteLine($"Request Method: {r.Method}");

                if (r.Method == "sampling/createMessage")
                {
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(llmResponse)
                    };
                }

                if (r.Method == "elicitation/create") // or whatever the method was
                {
                    var elicitResult = new ElicitResult
                    {
                        Action = "accept",
                        Content = new Dictionary<string, JsonElement>
                        {
                            ["collectionName"] = JsonSerializer.SerializeToElement("Tech")
                        }
                    };
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(elicitResult)
                    };
                }

                // Return empty response for others to avoid NRE if possible, but clean inspection is goal.
                return new JsonRpcResponse { Result = JsonNode.Parse("{}") };
            });

        _raindropsApiMock.Setup(x => x.UpdateAsync(bookmarkId, It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        // Act
        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmarkId, CancellationToken.None);

        // Assert
        Assert.True(result.Result);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_HandlesPipeSeparatedList()
    {
        // Arrange
        long bookmarkId = 123;
        var bookmark = new Raindrop { Id = bookmarkId, Title = "Test", Link = "url", Excerpt = "excerpt" };

        _raindropsApiMock.Setup(x => x.GetAsync(bookmarkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection>
            {
                new Collection { Id = 1, Title = "Tech" },
                new Collection { Id = 2, Title = "News" }
            }));

        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "Tech | News" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonRpcRequest r, CancellationToken t) =>
            {
                if (r.Method == "sampling/createMessage")
                {
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(llmResponse)
                    };
                }

                if (r.Method == "elicitation/create")
                {
                    var elicitResult = new ElicitResult
                    {
                        Action = "accept",
                        Content = new Dictionary<string, JsonElement>
                        {
                            ["collectionName"] = JsonSerializer.SerializeToElement("Tech")
                        }
                    };
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(elicitResult)
                    };
                }

                return new JsonRpcResponse { Result = JsonNode.Parse("{}") };
            });

        _raindropsApiMock.Setup(x => x.UpdateAsync(bookmarkId, It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        // Act
        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmarkId, CancellationToken.None);

        // Assert
        Assert.True(result.Result);
    }

    [Fact]
    public async Task SuggestCollectionForBookmarkAsync_HandlesCollectionWithComma()
    {
        // Arrange
        long bookmarkId = 123;
        var bookmark = new Raindrop { Id = bookmarkId, Title = "Test", Link = "url", Excerpt = "excerpt" };

        _raindropsApiMock.Setup(x => x.GetAsync(bookmarkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        _collectionsApiMock.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, new List<Collection>
            {
                new Collection { Id = 1, Title = "Science, Tech & Nature" },
                new Collection { Id = 2, Title = "Other" }
            }));

        var llmResponse = new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "Science, Tech & Nature | Other" }],
            Model = "test-model"
        };

        _mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities
            {
                Sampling = new SamplingCapability(),
                Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
            });

        _mcpServerMock.Setup(x => x.SendRequestAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonRpcRequest r, CancellationToken t) =>
            {
                if (r.Method == "sampling/createMessage")
                {
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(llmResponse)
                    };
                }

                if (r.Method == "elicitation/create")
                {
                    var elicitResult = new ElicitResult
                    {
                        Action = "accept",
                        Content = new Dictionary<string, JsonElement>
                        {
                            ["collectionName"] = JsonSerializer.SerializeToElement("Science, Tech & Nature")
                        }
                    };
                    return new JsonRpcResponse
                    {
                        Result = JsonSerializer.SerializeToNode(elicitResult)
                    };
                }

                return new JsonRpcResponse { Result = JsonNode.Parse("{}") };
            });

        _raindropsApiMock.Setup(x => x.UpdateAsync(bookmarkId, It.IsAny<Raindrop>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemResponse<Raindrop>(true, bookmark));

        // Act
        var result = await _tools.SuggestCollectionForBookmarkAsync(_mcpServerMock.Object, bookmarkId, CancellationToken.None);

        // Assert
        Assert.True(result.Result);
    }
}
