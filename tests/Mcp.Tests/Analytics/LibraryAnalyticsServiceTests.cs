using Mcp.Analytics;
using Mcp.Collections;
using Mcp.Common;
using Mcp.Raindrops;
using Moq;

namespace RaindropMcp.Tests.Analytics;

public class LibraryAnalyticsServiceTests
{
    [Fact]
    public async Task AnalyzeAllBuildsHierarchyAndAggregatesEveryPage()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);

        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true,
            [
                new Collection { Id = 1, Title = "Development" }
            ]));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true,
            [
                new Collection { Id = 2, Title = ".NET", Parent = new IdRef { Id = 1 } }
            ]));

        var firstPage = Enumerable.Range(1, 50)
            .Select(id => Bookmark(id, id <= 30 ? 1 : 2, "example.com", ["dotnet"]))
            .ToList();
        var secondPage = new[]
        {
            Bookmark(51, -1, "other.example", ["research", "dotnet"], important: true)
        };

        SetupPage(raindropsApi, 0, 0, null, firstPage);
        SetupPage(raindropsApi, 0, 1, null, secondPage);

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(0, CancellationToken.None);

        Assert.True(report.Scope.IsComplete);
        Assert.Equal(2, report.Scope.PagesFetched);
        Assert.Equal(51, report.Summary.BookmarksAnalyzed);
        Assert.Equal(1, report.Summary.RootCollections);
        Assert.Equal(1, report.Summary.ChildCollections);
        Assert.Equal(1, report.Summary.UnsortedBookmarks);
        Assert.Equal(1, report.Summary.FavoriteBookmarks);

        var root = Assert.Single(report.Collections, collection => collection.Id == 1);
        Assert.Equal(30, root.DirectBookmarkCount);
        Assert.Equal(20, root.DescendantBookmarkCount);
        Assert.Equal(50, root.SubtreeBookmarkCount);

        var child = Assert.Single(report.Collections, collection => collection.Id == 2);
        Assert.Equal(1, child.Depth);
        Assert.Equal(20, child.DirectBookmarkCount);

        var unsorted = Assert.Single(report.Collections, collection => collection.Id == -1);
        Assert.Equal(1, unsorted.DirectBookmarkCount);

        Assert.Equal(50, Assert.Single(report.Domains, domain => domain.Value == "example.com").Count);
        Assert.Equal(51, Assert.Single(report.Tags, tag => tag.Value == "dotnet").Count);
        Assert.Equal(1, Assert.Single(report.Tags, tag => tag.Value == "research").Count);
    }

    [Fact]
    public async Task AnalyzeCollectionIncludesDescendantsAndRequestsNestedBookmarks()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);

        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true,
            [
                new Collection { Id = 1, Title = "Selected" },
                new Collection { Id = 9, Title = "Other" }
            ]));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true,
            [
                new Collection { Id = 2, Title = "Child", Parent = new IdRef { Id = 1 } },
                new Collection { Id = 10, Title = "Other child", Parent = new IdRef { Id = 9 } }
            ]));

        SetupPage(raindropsApi, 1, 0, true,
        [
            Bookmark(1, 1, "one.example", []),
            Bookmark(2, 2, "two.example", [])
        ]);

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(1, CancellationToken.None);

        Assert.True(report.Scope.IncludesDescendants);
        Assert.Equal([1, 2], report.Collections.Select(collection => collection.Id).Order().ToArray());
        Assert.DoesNotContain(report.Collections, collection => collection.Id is 9 or 10);
        Assert.Equal(2, Assert.Single(report.Collections, collection => collection.Id == 1).SubtreeBookmarkCount);
    }

    [Fact]
    public async Task AnalyzeSystemCollectionDoesNotFetchUserCollections()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);
        SetupPage(raindropsApi, -99, 0, null,
        [
            Bookmark(1, -99, "deleted.example", [])
        ]);

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(-99, CancellationToken.None);

        var trash = Assert.Single(report.Collections);
        Assert.Equal(-99, trash.Id);
        Assert.Equal("Trash", trash.Title);
        Assert.Equal(1, trash.DirectBookmarkCount);
        collectionsApi.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnalyzeRejectsUnsupportedNegativeCollectionId()
    {
        var service = new LibraryAnalyticsService(
            Mock.Of<ICollectionsApi>(),
            Mock.Of<IRaindropsApi>());

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.AnalyzeAsync(-2, CancellationToken.None));

        Assert.Equal("collectionId", exception.ParamName);
    }

    [Fact]
    public async Task AnalyzeStopsRepeatedFullPageAndMarksReportPartial()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);
        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));

        var page = Enumerable.Range(1, 50)
            .Select(id => Bookmark(id, -1, "example.com", []))
            .ToList();
        SetupPage(raindropsApi, 0, 0, null, page);
        SetupPage(raindropsApi, 0, 1, null, page);

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(0, CancellationToken.None);

        Assert.False(report.Scope.IsComplete);
        Assert.Equal("repeated_page", report.Scope.TerminationReason);
        Assert.Equal(50, report.Summary.BookmarksAnalyzed);
        Assert.Contains(report.Diagnostics, diagnostic => diagnostic.Contains("repeated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeReturnsPartialReportWhenLaterPageFails()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);
        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));

        SetupPage(raindropsApi, 0, 0, null, Enumerable.Range(1, 50)
            .Select(id => Bookmark(id, -1, "example.com", []))
            .ToList());
        raindropsApi.Setup(api => api.ListAsync(
                0, null, "created", 1, 50, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("transient failure after retries"));

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(0, CancellationToken.None);

        Assert.False(report.Scope.IsComplete);
        Assert.Equal("api_error", report.Scope.TerminationReason);
        Assert.Equal(1, report.Scope.PagesFetched);
        Assert.Equal(50, report.Summary.BookmarksAnalyzed);
        Assert.Contains(report.Diagnostics, diagnostic => diagnostic.Contains("after retries", StringComparison.Ordinal));
        raindropsApi.Verify(api => api.ListAsync(
            0, null, "created", It.IsAny<int>(), 50, null, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AnalyzeContinuesBeyondFormerPageLimitUntilEndOfResults()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);
        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));

        for (var page = 0; page < 21; page++)
        {
            var firstId = page * 50 + 1;
            SetupPage(raindropsApi, 0, page, null, Enumerable.Range(firstId, 50)
                .Select(id => Bookmark(id, -1, "example.com", []))
                .ToList());
        }
        SetupPage(raindropsApi, 0, 21, null, []);

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object);

        var report = await service.AnalyzeAsync(0, CancellationToken.None);

        Assert.True(report.Scope.IsComplete);
        Assert.Equal("end_of_results", report.Scope.TerminationReason);
        Assert.Equal(22, report.Scope.PagesFetched);
        Assert.Equal(1_050, report.Summary.BookmarksAnalyzed);
    }

    [Fact]
    public async Task AnalyzeStopsAtConfiguredPageLimitAndMarksReportPartial()
    {
        var collectionsApi = new Mock<ICollectionsApi>(MockBehavior.Strict);
        var raindropsApi = new Mock<IRaindropsApi>(MockBehavior.Strict);
        collectionsApi.Setup(api => api.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));
        collectionsApi.Setup(api => api.ListChildrenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResponse<Collection>(true, []));
        SetupPage(raindropsApi, 0, 0, null, Enumerable.Range(1, 50)
            .Select(id => Bookmark(id, -1, "example.com", []))
            .ToList());
        SetupPage(raindropsApi, 0, 1, null, Enumerable.Range(51, 50)
            .Select(id => Bookmark(id, -1, "example.com", []))
            .ToList());

        var service = new LibraryAnalyticsService(collectionsApi.Object, raindropsApi.Object, maximumPages: 2);

        var report = await service.AnalyzeAsync(0, CancellationToken.None);

        Assert.False(report.Scope.IsComplete);
        Assert.Equal("safety_limit_reached", report.Scope.TerminationReason);
        Assert.Equal(2, report.Scope.PagesFetched);
        Assert.Equal(100, report.Summary.BookmarksAnalyzed);
        Assert.Contains(report.Diagnostics, diagnostic => diagnostic.Contains("2 pages", StringComparison.Ordinal));
    }

    private static Raindrop Bookmark(
        int id,
        int collectionId,
        string? domain,
        IReadOnlyList<string> tags,
        bool important = false) => new()
        {
            Id = id,
            Link = $"https://{domain ?? "example.com"}/{id}",
            Domain = domain,
            Collection = new IdRef { Id = collectionId },
            Tags = tags,
            Excerpt = $"Bookmark {id}",
            Important = important
        };

    private static void SetupPage(
        Mock<IRaindropsApi> api,
        int collectionId,
        int page,
        bool? nested,
        IReadOnlyList<Raindrop> bookmarks) => api
        .Setup(client => client.ListAsync(
            collectionId,
            null,
            "created",
            page,
            50,
            nested,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ItemsResponse<Raindrop>(true, bookmarks));
}
