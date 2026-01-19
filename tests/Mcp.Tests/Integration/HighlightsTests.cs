using Mcp.Collections;
using Mcp.Raindrops;
using Mcp.Highlights;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class HighlightsTests : TestBase
{
    public HighlightsTests() : base(s =>
    {
        s.AddTransient<CollectionsTools>();
        s.AddTransient<RaindropsTools>();
        s.AddTransient<HighlightsTools>();
    }) { }

    [SkippableFact]
    public async Task Crud()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = cts.Token;

        var collections = Provider.GetRequiredService<CollectionsTools>();
        var raindropsService = Provider.GetRequiredService<RaindropsTools>();
        var highlights = Provider.GetRequiredService<HighlightsTools>();

        // Generate unique ID for this test run
        var uniqueId = Guid.NewGuid().ToString("N");

        int collectionId = -1;
        long raindropId = -1;

        try
        {
            // 1. Create Collection
            var collectionResponse = await collections.CreateCollectionAsync(new Collection
            {
                Title = $"Highlights Crud - Collection {uniqueId}"
            }, cancellationToken);
            collectionId = collectionResponse.Item.Id;

            // 2. Create Bookmark
            var raindropResponse = await raindropsService.CreateBookmarkAsync(new RaindropCreateRequest
            {
                CollectionId = collectionId,
                Link = $"https://example.com/hl/{uniqueId}",
                Title = $"Highlights Crud - Raindrop {uniqueId}",
                Note = "hl"
            }, cancellationToken);
            raindropId = raindropResponse.Item.Id;

            // 3. Create Highlight
            var newHighlight = await highlights.CreateHighlightAsync(raindropId, new HighlightCreateRequest
            {
                Text = $"Highlights Crud - New {uniqueId}",
                Note = "note"
            }, cancellationToken);

            string highlightId = newHighlight.Item.Highlights.Last().Id!;
            Assert.False(string.IsNullOrEmpty(highlightId), "Highlight ID should not be null or empty");

            // 4. Update Highlight
            await highlights.UpdateHighlightAsync(raindropId, new HighlightUpdateRequest
            {
                Id = highlightId,
                Text = $"Highlights Crud - Updated {uniqueId}",
                Note = "edited"
            }, cancellationToken);

            // 5. List All Highlights (Global)
            // Note: This might be huge if the account has many highlights, but we check if count > 0
            var listAll = await highlights.ListHighlightsAsync(page: 0, perPage: 1, cancellationToken: cancellationToken);
            Assert.True(listAll.Items.Count > 0);

            // 6. List Highlights By Collection
            var listByCollection = await highlights.ListHighlightsByCollectionAsync(collectionId, page: 0, perPage: 50, cancellationToken: cancellationToken);
            Assert.Contains(listByCollection.Items, h => h.Id == highlightId);

            // 7. Get Bookmark Highlights
            var retrieved = await highlights.GetBookmarkHighlightsAsync(raindropId, cancellationToken);
            Assert.Contains(retrieved.Item.Highlights, h => h.Id == highlightId);
            var specificHighlight = retrieved.Item.Highlights.First(h => h.Id == highlightId);
            Assert.Equal($"Highlights Crud - Updated {uniqueId}", specificHighlight.Text);
            Assert.Equal("edited", specificHighlight.Note);

            // 8. Delete Highlight
            await highlights.DeleteHighlightAsync(raindropId, highlightId, cancellationToken);

            // Verify deletion
            var postDelete = await highlights.GetBookmarkHighlightsAsync(raindropId, cancellationToken);
            Assert.DoesNotContain(postDelete.Item.Highlights, h => h.Id == highlightId);
        }
        finally
        {
            if (raindropId != -1)
            {
                await raindropsService.DeleteBookmarkAsync(raindropId, cancellationToken);
            }
            if (collectionId != -1)
            {
                await collections.DeleteCollectionAsync(collectionId, cancellationToken);
            }
        }
    }
}
