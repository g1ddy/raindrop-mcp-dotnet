using System.Reflection;
using Mcp.Analytics;
using ModelContextProtocol.Server;
using Moq;

namespace RaindropMcp.Tests.Analytics;

public class LibraryAnalyticsToolsTests
{
    [Fact]
    public async Task AnalyzeLibraryReturnsTextAndStructuredReport()
    {
        var report = Report();
        var service = new Mock<ILibraryAnalyticsService>();
        service.Setup(analyzer => analyzer.AnalyzeAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        var tools = new LibraryAnalyticsTools(service.Object);

        var result = await tools.AnalyzeLibraryAsync(cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        Assert.NotEmpty(result.Content);
        Assert.Equal(3, result.StructuredContent?.GetProperty("summary").GetProperty("bookmarksAnalyzed").GetInt32());
    }

    [Fact]
    public void AnalyzeLibraryDeclaresReadOnlyToolMetadata()
    {
        var attribute = typeof(LibraryAnalyticsTools)
            .GetMethod(nameof(LibraryAnalyticsTools.AnalyzeLibraryAsync))!
            .GetCustomAttribute<McpServerToolAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("analyze_library", attribute.Name);
        Assert.True(attribute.ReadOnly);
        Assert.False(attribute.Destructive);
        Assert.True(attribute.Idempotent);
    }

    private static LibraryAnalyticsReport Report() => new()
    {
        Scope = new LibraryAnalyticsScope
        {
            CollectionId = 0,
            IncludesDescendants = false,
            PagesFetched = 1,
            IsComplete = true,
            TerminationReason = "end_of_results",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow
        },
        Summary = new LibraryAnalyticsSummary
        {
            BookmarksAnalyzed = 3,
            RootCollections = 1,
            ChildCollections = 0,
            MaximumCollectionDepth = 0,
            UniqueDomains = 1,
            UniqueTags = 1,
            UntaggedBookmarks = 0,
            BookmarksWithoutDomains = 0,
            BookmarksWithoutExcerpts = 0,
            FavoriteBookmarks = 0,
            UnsortedBookmarks = 0
        },
        Collections = [],
        Domains = [],
        Tags = [],
        Diagnostics = []
    };
}
