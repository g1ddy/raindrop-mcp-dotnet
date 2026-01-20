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
public class TagsBulkTests : TestBase
{
    public TagsBulkTests() : base(s =>
    {
        s.AddTransient<RaindropsTools>();
        s.AddTransient<TagsTools>();
    }) { }

    [SkippableFact]
    public async Task BulkEndpoints()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        var cancellationToken = cts.Token;
        var raindrops = Provider.GetRequiredService<RaindropsTools>();
        var tags = Provider.GetRequiredService<TagsTools>();

        var uniqueId = Guid.NewGuid().ToString("N");
        var tag1 = $"TagBulkOne_{uniqueId}";
        var tag2 = $"TagBulkTwo_{uniqueId}";
        var tagRenamed = $"TagBulkRenamed_{uniqueId}";

        var items = new List<Raindrop>
        {
            new Raindrop { Link = $"https://example.com/tags-bulk1/{uniqueId}", Title = $"Tags Bulk One {uniqueId}", Tags = [tag1] },
            new Raindrop { Link = $"https://example.com/tags-bulk2/{uniqueId}", Title = $"Tags Bulk Two {uniqueId}", Tags = [tag2] }
        };

        var created = await raindrops.CreateBookmarksAsync(0, items, cancellationToken);
        var ids = created.Items.Select(r => r.Id).ToList();

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
            // Wait for tags to be indexed and available
            await PollUntilAsync(async () =>
            {
                var list = await tags.ListTagsAsync(null, cancellationToken);
                return list.Items.Any(t => t.Id == tag1) && list.Items.Any(t => t.Id == tag2);
            }, "Initial tags not found", cancellationToken);

            // Rename tags
            await tags.RenameTagsAsync([tag1, tag2], tagRenamed, null, cancellationToken);

            // Poll for rename to propagate
            await PollUntilAsync(async () =>
            {
                var list = await tags.ListTagsAsync(null, cancellationToken);
                return list.Items.Any(t => t.Id == tagRenamed) &&
                       !list.Items.Any(t => t.Id == tag1) &&
                       !list.Items.Any(t => t.Id == tag2);
            }, "Renamed tag not found or old tags still exist", cancellationToken);

            // Delete tag
            await tags.DeleteTagsAsync(mcpServerMock.Object, [tagRenamed], null, cancellationToken);

            // Poll for deletion
            await PollUntilAsync(async () =>
            {
                var finalList = await tags.ListTagsAsync(null, cancellationToken);
                return !finalList.Items.Any(t => t.Id == tagRenamed);
            }, "Tag deletion verification failed", cancellationToken);
        }
        finally
        {
            foreach (var id in ids)
            {
                try
                {
                    await raindrops.DeleteBookmarkAsync(id, cancellationToken);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
