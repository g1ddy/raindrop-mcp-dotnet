using Mcp.Raindrops;
using Mcp.Tags;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class TagsTests : TestBase
{
    public TagsTests() : base(s =>
    {
        s.AddTransient<RaindropsTools>();
        s.AddTransient<TagsTools>();
    }) { }

    [SkippableFact]
    public async Task Crud()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;
        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();
        var tagsTool = Provider.GetRequiredService<TagsTools>();

        var uniqueId = Guid.NewGuid().ToString("N");
        var tagName1 = $"TagRenameTestOne_{uniqueId}";
        var tagName2 = $"TagRenameTestTwo_{uniqueId}";

        var createResponse = await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            Link = $"https://example.com/tag/{uniqueId}",
            Title = $"Tags Crud - Raindrop {uniqueId}",
            Tags = [ tagName1 ],
            Note = "tag"
        }, cancellationToken);
        long raindropId = createResponse.Item.Id;

        // Mock IMcpServer for deletion confirmation
        var mcpServerMock = new Mock<IMcpServer>();
        mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities { Elicitation = new ElicitationCapability() });

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

        try
        {
            // Rename tag
            await tagsTool.RenameTagAsync(tagName1, tagName2, null, cancellationToken);

            // Poll for tag rename
            await PollUntilAsync(async () =>
            {
                var list = await tagsTool.ListTagsAsync(null, cancellationToken);
                return list.Items.Any(t => t.Id == tagName2) && !list.Items.Any(t => t.Id == tagName1);
            }, "Tag rename verification failed", cancellationToken);
        }
        finally
        {
            // Clean up tag
            await tagsTool.DeleteTagAsync(mcpServerMock.Object, tagName2, null, cancellationToken);
            // Clean up bookmark
            await raindropsTool.DeleteBookmarkAsync(raindropId, cancellationToken);
        }
    }

    [SkippableFact]
    public async Task CrudForCollection()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;
        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();
        var tagsTool = Provider.GetRequiredService<TagsTools>();

        var uniqueId = Guid.NewGuid().ToString("N");
        var tagName1 = $"TagCollectionTestOne_{uniqueId}";
        var tagName2 = $"TagCollectionTestTwo_{uniqueId}";

        var createResponse = await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            Link = $"https://example.com/tag/collection/{uniqueId}",
            Title = $"Tags CrudForCollection - Raindrop {uniqueId}",
            Tags = [ tagName1 ],
            Note = "tag-col"
        }, cancellationToken);
        long raindropId = createResponse.Item.Id;

        // Mock IMcpServer for deletion confirmation
        var mcpServerMock = new Mock<IMcpServer>();
        mcpServerMock.Setup(x => x.ClientCapabilities)
            .Returns(new ClientCapabilities { Elicitation = new ElicitationCapability() });

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

        try
        {
            // Rename tag within collection (0 is usually "All items" or "Unsorted" but API treats 0 specially sometimes,
            // however Raindrop API usually expects specific collection ID.
            // The original test used 0, assuming that's where it landed (Unsorted).
            // Default collection for create is usually Unsorted (0) or -1. Let's assume 0 works as per original test.

            await tagsTool.RenameTagAsync(tagName1, tagName2, 0, cancellationToken);

            // Poll for tag rename
            await PollUntilAsync(async () =>
            {
                var list = await tagsTool.ListTagsAsync(0, cancellationToken);
                return list.Items.Any(t => t.Id == tagName2);
            }, "Tag rename in collection verification failed", cancellationToken);

            // Delete tag
            await tagsTool.DeleteTagAsync(mcpServerMock.Object, tagName2, 0, cancellationToken);

            // Poll for deletion
            await PollUntilAsync(async () =>
            {
                var finalList = await tagsTool.ListTagsAsync(0, cancellationToken);
                return !finalList.Items.Any(t => t.Id == tagName2);
            }, "Tag deletion verification failed", cancellationToken);
        }
        finally
        {
            await raindropsTool.DeleteBookmarkAsync(raindropId, cancellationToken);
        }
    }

    private async Task PollUntilAsync(Func<Task<bool>> condition, string failureMessage, CancellationToken cancellationToken)
    {
        const int pollAttempts = 30; // Increased attempts for safety
        const int pollIntervalMs = 1000;

        for (var i = 0; i < pollAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await condition())
            {
                return; // Condition met
            }
            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        Assert.Fail(failureMessage);
    }
}
