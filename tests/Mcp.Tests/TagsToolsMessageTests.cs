using Mcp.Tags;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mcp.Tests;

public class TagsToolsMessageTests
{
    [Fact]
    public async Task DeleteTagsAsync_PromptsWithTagNames()
    {
        // Arrange
        var apiMock = new Mock<ITagsApi>();
        var tools = new TagsTools(apiMock.Object);
        var tagsToDelete = new[] { "tag1", "tag2", "tag3" };

        var mcpServerMock = new Mock<McpServer>();
        mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities { Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() } });

        mcpServerMock
            .Setup(x => x.SendRequestAsync(
                It.Is<JsonRpcRequest>(r => r.Method == "elicitation/create"),
                It.IsAny<CancellationToken>()))
            .Callback<JsonRpcRequest, CancellationToken>((req, token) =>
            {
                // Deserialize params to inspect the message
                // The Params in JsonRpcRequest is object?, likely a generic type or JObject/JsonElement depending on library
                // But McpServer implementation likely wraps it.
                // Actually, I can mock ElicitAsync if it was virtual.
                // Looking at McpServer source (or usage), it is a concrete class.
                // But the method is likely using SendRequestAsync under the hood.

                // Let's rely on the fact that SendRequestAsync receives a JsonRpcRequest
                // with Params containing the ElicitRequestParams
            })
            .ReturnsAsync(new JsonRpcResponse
            {
                Result = JsonSerializer.SerializeToNode(new ElicitResult
                {
                    Action = "accept",
                    Content = new Dictionary<string, JsonElement>
                    {
                        ["confirm"] = JsonSerializer.SerializeToElement(true)
                    }
                })
            });

        // Act
        await tools.DeleteTagsAsync(mcpServerMock.Object, tagsToDelete, null);

        // Assert
        mcpServerMock.Verify(x => x.SendRequestAsync(
            It.Is<JsonRpcRequest>(r => VerifyMessageContent(r, tagsToDelete)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private bool VerifyMessageContent(JsonRpcRequest request, string[] expectedTags)
    {
        if (request.Method != "elicitation/create") return false;

        // Reflection or serialization needed to get to the Message property?
        // Params is generic.

        var json = JsonSerializer.Serialize(request.Params);
        // Simple string check for now
        foreach (var tag in expectedTags)
        {
            if (!json.Contains(tag)) return false;
        }
        return true;
    }
}
