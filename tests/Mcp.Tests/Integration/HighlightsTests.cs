using Mcp.Highlights;
using Mcp.Raindrops;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mcp.Tests.Integration;

[Trait("Category", "Integration")]
public class HighlightsTests : TestBase
{
    public HighlightsTests() : base(s => {
        s.AddTransient<RaindropsTools>();
        s.AddTransient<HighlightsTools>();
    }) { }

    [SkippableFact]
    public async Task Crud()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = cts.Token;

        var raindropsTool = Provider.GetRequiredService<RaindropsTools>();
        var highlightsTool = Provider.GetRequiredService<HighlightsTools>();

        // Generate unique ID for this test run to avoid collisions
        var uniqueId = Guid.NewGuid().ToString("N");

        // Create a bookmark to add a highlight to
        var createResponse = await raindropsTool.CreateBookmarkAsync(new RaindropCreateRequest
        {
            Link = $"https://example.com/{uniqueId}",
            Title = $"Highlights Crud - Create {uniqueId}",
        }, cancellationToken);

        long raindropId = createResponse.Item.Id;

        try
        {
            // Create a highlight
            var highlightCreateResponse = await highlightsTool.CreateHighlightAsync(new HighlightCreateRequest
            {
                RaindropRef = raindropId,
                Text = "This is a highlighted text.",
                Note = "This is a note."
            }, cancellationToken);

            // Get highlights for the bookmark
            var highlights = await highlightsTool.GetHighlightsAsync(raindropId, cancellationToken);
            var highlight = highlights.Items.FirstOrDefault();

            Assert.NotNull(highlight);
            Assert.NotNull(highlight.Created);
            Assert.True(highlight.Created.Value > DateTime.MinValue);
            Assert.NotNull(highlight.LastUpdate);
            Assert.True(highlight.LastUpdate.Value > DateTime.MinValue);
        }
        finally
        {
            // Delete the bookmark
            await raindropsTool.DeleteBookmarkAsync(raindropId, cancellationToken);
        }
    }
}
