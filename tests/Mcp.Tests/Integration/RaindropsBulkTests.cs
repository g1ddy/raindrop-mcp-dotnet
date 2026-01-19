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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var cancellationToken = cts.Token;

        var tool = Provider.GetRequiredService<RaindropsTools>();

        // Generate unique ID for this test run to avoid collisions
        var uniqueId = Guid.NewGuid().ToString("N");

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
            var foundAll = false;
            const int pollAttempts = 15;
            const int pollIntervalMs = 2000;

            for (var i = 0; i < pollAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Search using unique ID
                var list = await tool.ListBookmarksAsync(0, uniqueId, null, null, null, null, cancellationToken);
                if (ids.All(id => list.Items.Any(r => r.Id == id)))
                {
                    foundAll = true;
                    break;
                }
                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            Assert.True(foundAll, "Not all created bookmarks were found in search results.");

            var update = new RaindropBulkUpdate
            {
                Ids = ids,
                Important = true,
                Tags = ["bulk-test"]
            };

            await tool.UpdateBookmarksAsync(0, update, null, null, cancellationToken);

            // Poll for updates to be reflected
            var updatesReflected = false;
            for (var i = 0; i < pollAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updated = await tool.ListBookmarksAsync(0, uniqueId, null, null, null, null, cancellationToken);
                var allUpdated = true;

                foreach (var id in ids)
                {
                    var item = updated.Items.FirstOrDefault(r => r.Id == id);
                    if (item == null || item.Important != true || item.Tags == null || !item.Tags.Contains("bulk-test"))
                    {
                        allUpdated = false;
                        break;
                    }
                }

                if (allUpdated)
                {
                    updatesReflected = true;
                    break;
                }

                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            Assert.True(updatesReflected, "Bulk updates were not reflected in search results.");
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
