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

            var highlight = await highlights.CreateHighlightAsync(firstRaindropId.Value, new HighlightCreateRequest { Text = "Integration Highlight", Note = "int" }, cancellationToken);
            string highlightId = highlight.Item.Highlights.Last().Id!;
            Assert.False(string.IsNullOrEmpty(highlightId), "Highlight ID should not be null or empty");

            await raindropsTool.UpdateBookmarkAsync(secondRaindropId.Value, new RaindropUpdateRequest { Link = "https://example.com/updated", CollectionId = childCollectionId }, cancellationToken);

            await tags.RenameTagAsync(tagTwo, tagTwoRenamed, null, cancellationToken);

            // Poll for tag rename consistency
            await PollUntilAsync(async () =>
            {
                var t = await tags.ListTagsAsync(null, cancellationToken);
                return t.Items.Any(tag => tag.Id == tagTwoRenamed);
            }, cancellationToken, "Tag rename propagation");

            var tagList = await tags.ListTagsAsync(null, cancellationToken);
            Assert.Contains(tagList.Items, t => t.Id == tagTwoRenamed);

            var childCollections = await collections.ListChildCollectionsAsync(cancellationToken);
            Assert.Contains(childCollections.Items, c => c.Id == childCollectionId);

            // Perform explicit cleanup as part of the test flow to verify "clean state" behavior
            if (firstRaindropId.HasValue) await raindropsTool.DeleteBookmarkAsync(firstRaindropId.Value, cancellationToken);
            if (secondRaindropId.HasValue) await raindropsTool.DeleteBookmarkAsync(secondRaindropId.Value, cancellationToken);
            if (childCollectionId.HasValue) await collections.DeleteCollectionAsync(childCollectionId.Value, cancellationToken);
            if (rootCollectionId.HasValue) await collections.DeleteCollectionAsync(rootCollectionId.Value, cancellationToken);

            // Poll for tag deletion consistency
            await PollUntilAsync(async () =>
            {
                var t = await tags.ListTagsAsync(null, cancellationToken);
                return !t.Items.Any(tag => tag.Id == tagTwoRenamed);
            }, cancellationToken, "Tag cleanup propagation");

            var finalTags = await tags.ListTagsAsync(null, cancellationToken);
            Assert.DoesNotContain(finalTags.Items, t => t.Id == tagTwoRenamed);
        }
        finally
        {
            // Best effort cleanup in case of failure
            if (firstRaindropId.HasValue) try { await raindropsTool.DeleteBookmarkAsync(firstRaindropId.Value, cancellationToken); } catch { }
            if (secondRaindropId.HasValue) try { await raindropsTool.DeleteBookmarkAsync(secondRaindropId.Value, cancellationToken); } catch { }
            if (childCollectionId.HasValue) try { await collections.DeleteCollectionAsync(childCollectionId.Value, cancellationToken); } catch { }
            if (rootCollectionId.HasValue) try { await collections.DeleteCollectionAsync(rootCollectionId.Value, cancellationToken); } catch { }
        }
    }

    private async Task PollUntilAsync(Func<Task<bool>> predicate, CancellationToken cancellationToken, string description)
    {
        const int maxAttempts = 15;
        const int intervalMs = 2000;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (await predicate()) return;
            await Task.Delay(intervalMs, cancellationToken);
        }

        // Final check or let the subsequent assertion fail with a better message
    }
}
