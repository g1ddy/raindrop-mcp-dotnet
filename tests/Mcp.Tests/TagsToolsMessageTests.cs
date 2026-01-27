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

        var elicitParams = request.Params.Deserialize<ElicitRequestParams>();
        if (elicitParams == null)
        {
            return false;
        }

        // Verify that each tag is present in the confirmation message, with the expected formatting.
        foreach (var tag in expectedTags)
        {
            if (!elicitParams.Message.Contains($"- \"{tag}\""))
            {
                return false;
            }
        }

        return true;
    }
}
