using Mcp.Raindrops;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class RaindropsTests : TestBase
{
    public RaindropsTests() : base(s => s.AddTransient<RaindropsTools>()) { }

    [SkippableFact]
    public async Task Crud()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = cts.Token;

        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();

        // Generate unique ID for this test run to avoid collisions
        var uniqueId = Guid.NewGuid().ToString("N");

        // Create
        var createResponse = await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            CollectionId = null,
            Link = $"https://example.com/{uniqueId}",
            Title = $"Raindrops Crud - Create {uniqueId}",
            Note = "note",
            Excerpt = "Test Excerpt",
            Tags = ["tag1", "tag2"],
            Important = false
        }, cancellationToken);

        long raindropId = createResponse.Item.Id;

        try
        {
            // Update single
            await raindropsTool.UpdateBookmarkAsync(raindropId, new RaindropUpdateRequest
            {
                Title = $"Raindrops Crud - Updated {uniqueId}",
                Note = "updated note",
                Excerpt = "Updated Excerpt",
                Tags = ["tag3"]
            }, cancellationToken);

            // Bulk update (UpdateBookmarksAsync signature: collectionId, update, nested, search, cancellationToken)
            await raindropsTool.UpdateBookmarksAsync(0, new RaindropBulkUpdate
            {
                Ids = [raindropId],
                Important = true
            }, null, null, cancellationToken);

            // Poll for the bookmark to be indexed and appear in search results.
            var foundInSearch = false;
            const int pollAttempts = 15;
            const int pollIntervalMs = 2000;

            for (var i = 0; i < pollAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Search using unique ID
                var search = await raindropsTool.ListBookmarksAsync(0, uniqueId, null, null, null, null, cancellationToken);
                if (search.Items.Any(r => r.Id == raindropId))
                {
                    foundInSearch = true;
                    break;
                }
                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            Assert.True(foundInSearch, $"Bookmark {raindropId} did not appear in search results after {pollAttempts} attempts.");

            // Get
            var retrieved = await raindropsTool.GetBookmarkAsync(raindropId, cancellationToken);
            Assert.Equal($"Raindrops Crud - Updated {uniqueId}", retrieved.Item.Title);
            Assert.Equal("updated note", retrieved.Item.Note);
            Assert.Equal("Updated Excerpt", retrieved.Item.Excerpt);
            Assert.NotNull(retrieved.Item.Tags);
            Assert.Contains("tag3", retrieved.Item.Tags);
            Assert.Single(retrieved.Item.Tags);
            Assert.True(retrieved.Item.Important, "Bookmark should be marked important from bulk update");
        }
        finally
        {
            // Delete
            await raindropsTool.DeleteBookmarkAsync(raindropId, cancellationToken);
        }
    }
}
