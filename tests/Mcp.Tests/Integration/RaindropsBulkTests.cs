using Mcp.Raindrops;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class RaindropsBulkTests : TestBase
{
    public RaindropsBulkTests() : base(s => s.AddTransient<RaindropsTools>()) { }

    [SkippableFact]
    public async Task BulkEndpoints()
    {
        InitializeVcr();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;

        var tool = Provider.GetRequiredService<RaindropsTools>();

        // Generate unique ID for this test run to avoid collisions
        var uniqueId = CurrentTestId;

        var items = new List<Raindrop>
        {
            new Raindrop { Link = $"https://example.com/bulk1/{uniqueId}", Title = $"Bulk Endpoints One {uniqueId}" },
            new Raindrop { Link = $"https://example.com/bulk2/{uniqueId}", Title = $"Bulk Endpoints Two {uniqueId}" }
        };

        var created = await tool.CreateBookmarksAsync(0, items, cancellationToken);
        var ids = created.Items.Select(r => r.Id).ToList();

        try
        {
            // Poll for the bookmarks to be indexed and appear in search results.
            await PollUntilAsync(async () =>
            {
                var list = await tool.ListBookmarksAsync(0, uniqueId, null, null, null, null, cancellationToken);
                return ids.All(id => list.Items.Any(r => r.Id == id));
            }, "Not all created bookmarks were found in search results.", cancellationToken, 15, 2000);

            var update = new RaindropBulkUpdate
            {
                Ids = ids,
                Important = true,
                Tags = ["bulk-test"]
            };

            await tool.UpdateBookmarksAsync(0, update, null, null, cancellationToken);

            // Poll for updates to be reflected
            await PollUntilAsync(async () =>
            {
                var updated = await tool.ListBookmarksAsync(0, uniqueId, null, null, null, null, cancellationToken);
                return ids.All(id =>
                {
                    var item = updated.Items.FirstOrDefault(r => r.Id == id);
                    return item is { Important: true } && item.Tags?.Contains("bulk-test") == true;
                });
            }, "Bulk updates were not reflected in search results.", cancellationToken, 15, 2000);
        }
        finally
        {
            foreach (var id in ids)
            {
                try
                {
                    await tool.DeleteBookmarkAsync(id, cancellationToken);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
