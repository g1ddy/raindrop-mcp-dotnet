using Mcp.Tags;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mcp.Tests;

public class TagsToolsOptimizedTests
{
    [Fact]
    public async Task DeleteTagsAsync_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        var apiMock = new Mock<ITagsApi>();
        var tools = new TagsTools(apiMock.Object);
        var tagsToDelete = new[] { "tag \"quoted\"", "tag\nnewline", "normal" };

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
            It.Is<JsonRpcRequest>(r => VerifyMessageContent(r)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private bool VerifyMessageContent(JsonRpcRequest request)
    {
        if (request.Method != "elicitation/create") return false;

        var elicitParams = request.Params.Deserialize<ElicitRequestParams>();
        if (elicitParams == null) return false;

        // "tag \"quoted\"" -> "- \"tag \\\"quoted\\\"\""
        // "tag\nnewline"   -> "- \"tag newline\"" (newline replaced by space)

        bool hasQuoted = elicitParams.Message.Contains("- \"tag \\\"quoted\\\"\"");
        bool hasNewline = elicitParams.Message.Contains("- \"tag newline\"");
        bool hasNormal = elicitParams.Message.Contains("- \"normal\"");

        return hasQuoted && hasNewline && hasNormal;
    }
}
