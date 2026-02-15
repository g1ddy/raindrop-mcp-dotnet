using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Highlights;
using Mcp.Tags;
using Mcp.Common;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class FullFlowTests : TestBase
{
    public FullFlowTests() : base(s =>
    {
        s.AddTransient<CollectionsTools>();
        s.AddTransient<RaindropsTools>();
        s.AddTransient<HighlightsTools>();
        s.AddTransient<TagsTools>();
    })
    { }

    [SkippableFact]
    public async Task FullFlow()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();
        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();
        var highlights = Provider.GetRequiredService<HighlightsTools>();
        var tags = Provider.GetRequiredService<TagsTools>();

        // Use a unique suffix to avoid collisions during concurrent test runs
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var tagOne = $"TagOne{uniqueSuffix}";
        var tagTwo = $"TagTwo{uniqueSuffix}";
        var tagTwoRenamed = $"TagTwoRenamed{uniqueSuffix}";

        int? rootCollectionId = null;
        int? childCollectionId = null;
        long? firstRaindropId = null;
        long? secondRaindropId = null;

        try
        {
            rootCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = $"Integration Root Collection {uniqueSuffix}" }, cancellationToken)).Item.Id;
            childCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = $"Integration Child Collection {uniqueSuffix}", Parent = new IdRef { Id = rootCollectionId.Value } }, cancellationToken)).Item.Id;

            firstRaindropId = (await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
            {
                CollectionId = rootCollectionId,
                Link = "https://example.com/1",
                Title = "Integration Raindrop One",
                Tags = [tagOne],
                Note = "first"
            }, cancellationToken)).Item.Id;

            secondRaindropId = (await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
            {
                CollectionId = rootCollectionId,
                Link = "https://example.com/2",
                Title = "Integration Raindrop Two",
                Tags = [tagTwo],
                Note = "second"
            }, cancellationToken)).Item.Id;

            await highlights.CreateHighlightAsync(firstRaindropId.Value, new HighlightCreateRequest { Text = "Integration Highlight", Note = "int" }, cancellationToken);

            // Re-fetch highlights to verify creation and ID presence
            var raindropHighlights = await highlights.GetBookmarkHighlightsAsync(firstRaindropId.Value, cancellationToken);
            Assert.Contains(raindropHighlights.Item.Highlights, h => h.Text == "Integration Highlight" && !string.IsNullOrEmpty(h.Id));

            await raindropsTool.UpdateBookmarkAsync(secondRaindropId.Value, new RaindropUpdateRequest { Link = "https://example.com/updated", CollectionId = childCollectionId }, cancellationToken);

            // Mock McpServer for confirmation
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

            await tags.RenameTagAsync(mcpServerMock.Object, tagTwo, tagTwoRenamed, null, cancellationToken);

            // Poll for tag rename consistency
            await PollUntilAsync(async () =>
            {
                var t = await tags.ListTagsAsync(null, cancellationToken);
                return t.Items.Any(tag => tag.Id == tagTwoRenamed);
            }, "Tag rename propagation", cancellationToken, 6, 5000);

            var childCollections = await collections.ListChildCollectionsAsync(cancellationToken);
            Assert.Contains(childCollections.Items, c => c.Id == childCollectionId);

            // Perform explicit cleanup as part of the test flow to verify "clean state" behavior
            // Set IDs to null after deletion to prevent double deletion in finally block
            if (firstRaindropId.HasValue) { await raindropsTool.DeleteBookmarkAsync(firstRaindropId.Value, cancellationToken); firstRaindropId = null; }
            if (secondRaindropId.HasValue) { await raindropsTool.DeleteBookmarkAsync(secondRaindropId.Value, cancellationToken); secondRaindropId = null; }
            if (childCollectionId.HasValue) { await collections.DeleteCollectionAsync(childCollectionId.Value, cancellationToken); childCollectionId = null; }
            if (rootCollectionId.HasValue) { await collections.DeleteCollectionAsync(rootCollectionId.Value, cancellationToken); rootCollectionId = null; }

            // Poll for tag deletion consistency
            await PollUntilAsync(async () =>
            {
                var t = await tags.ListTagsAsync(null, cancellationToken);
                return !t.Items.Any(tag => tag.Id == tagTwoRenamed);
            }, "Tag cleanup propagation", cancellationToken, 6, 5000);
        }
        finally
        {
            // Best effort cleanup in case of failure
            if (firstRaindropId.HasValue) try { await raindropsTool.DeleteBookmarkAsync(firstRaindropId.Value, cancellationToken); } catch (Exception ex) { Console.Error.WriteLine($"[CLEANUP-ERROR] Failed to delete bookmark '{firstRaindropId.Value}': {ex.Message}"); }
            if (secondRaindropId.HasValue) try { await raindropsTool.DeleteBookmarkAsync(secondRaindropId.Value, cancellationToken); } catch (Exception ex) { Console.Error.WriteLine($"[CLEANUP-ERROR] Failed to delete bookmark '{secondRaindropId.Value}': {ex.Message}"); }
            if (childCollectionId.HasValue) try { await collections.DeleteCollectionAsync(childCollectionId.Value, cancellationToken); } catch (Exception ex) { Console.Error.WriteLine($"[CLEANUP-ERROR] Failed to delete collection '{childCollectionId.Value}': {ex.Message}"); }
            if (rootCollectionId.HasValue) try { await collections.DeleteCollectionAsync(rootCollectionId.Value, cancellationToken); } catch (Exception ex) { Console.Error.WriteLine($"[CLEANUP-ERROR] Failed to delete collection '{rootCollectionId.Value}': {ex.Message}"); }
        }
    }
}
