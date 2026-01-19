using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Highlights;
using Mcp.Tags;
using Mcp.Common;
using Microsoft.Extensions.DependencyInjection;
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
    }) { }

    [SkippableFact]
    public async Task FullFlow()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();

        // Use a unique suffix to avoid collisions during concurrent test runs
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var tagOne = $"TagOne{uniqueSuffix}";
        var tagTwo = $"TagTwo{uniqueSuffix}";
        var tagTwoRenamed = $"TagTwoRenamed{uniqueSuffix}";

        int rootCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = $"Integration Root Collection {uniqueSuffix}" }, cancellationToken)).Item.Id;
        int childCollectionId = (await collections.CreateCollectionAsync(new Collection { Title = $"Integration Child Collection {uniqueSuffix}", Parent = new IdRef { Id = rootCollectionId } }, cancellationToken)).Item.Id;

        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();
        long firstRaindropId = (await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            CollectionId = rootCollectionId,
            Link = "https://example.com/1",
            Title = "Integration Raindrop One",
            Tags = [tagOne],
            Note = "first"
        }, cancellationToken)).Item.Id;

        long secondRaindropId = (await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            CollectionId = rootCollectionId,
            Link = "https://example.com/2",
            Title = "Integration Raindrop Two",
            Tags = [tagTwo],
            Note = "second"
        }, cancellationToken)).Item.Id;

        var highlights = Provider.GetRequiredService<HighlightsTools>();
        var tags = Provider.GetRequiredService<TagsTools>();

        try
        {
            var highlight = await highlights.CreateHighlightAsync(firstRaindropId, new HighlightCreateRequest { Text = "Integration Highlight", Note = "int" }, cancellationToken);
            string highlightId = highlight.Item.Highlights.Last().Id!;

            await raindropsTool.UpdateBookmarkAsync(secondRaindropId, new RaindropUpdateRequest { Link = "https://example.com/updated", CollectionId = childCollectionId }, cancellationToken);

            await tags.RenameTagAsync(tagTwo, tagTwoRenamed, null, cancellationToken);

            // Allow some time for eventual consistency
            await Task.Delay(1000, cancellationToken);

            var tagList = await tags.ListTagsAsync(null, cancellationToken);
            Assert.Contains(tagList.Items, t => t.Id == tagTwoRenamed);

            var childCollections = await collections.ListChildCollectionsAsync(cancellationToken);
            Assert.Contains(childCollections.Items, c => c.Id == childCollectionId);

            // Perform explicit cleanup as part of the test flow to verify "clean state" behavior
            await raindropsTool.DeleteBookmarkAsync(firstRaindropId, cancellationToken);
            await raindropsTool.DeleteBookmarkAsync(secondRaindropId, cancellationToken);
            await collections.DeleteCollectionAsync(childCollectionId, cancellationToken);
            await collections.DeleteCollectionAsync(rootCollectionId, cancellationToken);

            await Task.Delay(1000, cancellationToken);

            var finalTags = await tags.ListTagsAsync(null, cancellationToken);
            Assert.DoesNotContain(finalTags.Items, t => t.Id == tagTwoRenamed);
        }
        finally
        {
            // Best effort cleanup in case of failure
            try { await raindropsTool.DeleteBookmarkAsync(firstRaindropId, cancellationToken); } catch { }
            try { await raindropsTool.DeleteBookmarkAsync(secondRaindropId, cancellationToken); } catch { }
            try { await collections.DeleteCollectionAsync(childCollectionId, cancellationToken); } catch { }
            try { await collections.DeleteCollectionAsync(rootCollectionId, cancellationToken); } catch { }
        }
    }
}
