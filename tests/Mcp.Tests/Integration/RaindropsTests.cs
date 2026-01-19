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

        // Create
        var createResponse = await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            CollectionId = null,
            Link = "https://example.com",
            Title = "Raindrops Crud - Create",
            Note = "note"
        }, cancellationToken);

        long raindropId = createResponse.Item.Id;

        try
        {
            // Update single
            await raindropsTool.UpdateBookmarkAsync(raindropId, new RaindropUpdateRequest
            {
                Title = "Raindrops Crud - Updated"
            }, cancellationToken);

            // Bulk update (UpdateBookmarksAsync signature: collectionId, update, nested, search, cancellationToken)
            await raindropsTool.UpdateBookmarksAsync(0, new RaindropBulkUpdate
            {
                Ids = [raindropId],
                Important = true
            }, null, null, cancellationToken);

            // Add a delay before searching to allow indexing/propagation
            await Task.Delay(5000, cancellationToken);

            // List/Search (ListBookmarksAsync signature: collectionId, search, sort, page, perPage, nested, cancellationToken)
            var search = await raindropsTool.ListBookmarksAsync(0, "example", null, null, null, null, cancellationToken);
            Assert.Contains(search.Items, r => r.Id == raindropId);

            // Get
            var retrieved = await raindropsTool.GetBookmarkAsync(raindropId, cancellationToken);
            Assert.Equal("Raindrops Crud - Updated", retrieved.Item.Title);
            Assert.True(retrieved.Item.Important, "Bookmark should be marked important from bulk update");
        }
        finally
        {
            // Delete
            await raindropsTool.DeleteBookmarkAsync(raindropId, cancellationToken);
        }
    }
}
