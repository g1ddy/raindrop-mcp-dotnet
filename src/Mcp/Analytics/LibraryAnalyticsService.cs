using Mcp.Collections;
using Mcp.Raindrops;
using Microsoft.Extensions.Options;

namespace Mcp.Analytics;

public sealed class LibraryAnalyticsService : ILibraryAnalyticsService
{
    private const int PageSize = 50;
    private readonly ICollectionsApi collectionsApi;
    private readonly IRaindropsApi raindropsApi;
    private readonly int maximumPages;

    public LibraryAnalyticsService(
        ICollectionsApi collectionsApi,
        IRaindropsApi raindropsApi,
        IOptions<LibraryAnalyticsOptions> options)
        : this(collectionsApi, raindropsApi, options.Value.MaximumPages)
    {
    }

    internal LibraryAnalyticsService(
        ICollectionsApi collectionsApi,
        IRaindropsApi raindropsApi,
        int maximumPages = LibraryAnalyticsOptions.DefaultMaximumPages)
    {
        this.collectionsApi = collectionsApi;
        this.raindropsApi = raindropsApi;
        this.maximumPages = maximumPages;
    }

    public async Task<LibraryAnalyticsReport> AnalyzeAsync(
        int collectionId,
        CancellationToken cancellationToken)
    {
        ValidateCollectionId(collectionId);

        var startedAt = DateTimeOffset.UtcNow;
        var diagnostics = new List<string>();

        var collections = await GetCollectionsAsync(collectionId, cancellationToken);
        var selectedCollections = SelectCollections(collectionId, collections);
        var (aggregate, pagesFetched, isComplete, terminationReason) =
            await GetBookmarksAsync(collectionId, diagnostics, cancellationToken);

        var collectionDistribution = BuildCollectionDistribution(
            collectionId,
            selectedCollections,
            aggregate.DirectCollectionCounts,
            aggregate.BookmarksAnalyzed,
            diagnostics);

        var domains = BuildDistribution(aggregate.DomainCounts, aggregate.BookmarksAnalyzed);
        var tags = BuildDistribution(aggregate.TagCounts, aggregate.BookmarksAnalyzed);

        var selectedCollectionIds = selectedCollections.Select(collection => collection.Id).ToHashSet();
        var rootCount = selectedCollections.Count(collection =>
            collection.Parent is null || !selectedCollectionIds.Contains(collection.Parent.Id));
        var childCount = selectedCollections.Count - rootCount;

        return new LibraryAnalyticsReport
        {
            Scope = new LibraryAnalyticsScope
            {
                CollectionId = collectionId,
                IncludesDescendants = collectionId > 0,
                PagesFetched = pagesFetched,
                IsComplete = isComplete,
                TerminationReason = terminationReason,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            },
            Summary = new LibraryAnalyticsSummary
            {
                BookmarksAnalyzed = aggregate.BookmarksAnalyzed,
                RootCollections = rootCount,
                ChildCollections = childCount,
                MaximumCollectionDepth = collectionDistribution.Count == 0
                    ? 0
                    : collectionDistribution.Max(collection => collection.Depth),
                UniqueDomains = domains.Count,
                UniqueTags = tags.Count,
                UntaggedBookmarks = aggregate.UntaggedBookmarks,
                BookmarksWithoutDomains = aggregate.BookmarksWithoutDomains,
                BookmarksWithoutExcerpts = aggregate.BookmarksWithoutExcerpts,
                FavoriteBookmarks = aggregate.FavoriteBookmarks,
                UnsortedBookmarks = aggregate.DirectCollectionCounts.GetValueOrDefault(-1)
            },
            Collections = collectionDistribution,
            Domains = domains,
            Tags = tags,
            Diagnostics = diagnostics
        };
    }

    private async Task<List<Collection>> GetCollectionsAsync(
        int collectionId,
        CancellationToken cancellationToken)
    {
        if (collectionId is -1 or -99)
            return [];

        var rootTask = collectionsApi.ListAsync(cancellationToken);
        var childrenTask = collectionsApi.ListChildrenAsync(cancellationToken);
        await Task.WhenAll(rootTask, childrenTask);

        var rootResponse = await rootTask;
        var childrenResponse = await childrenTask;
        if (!rootResponse.Result || !childrenResponse.Result)
            throw new InvalidOperationException("Raindrop did not return the collection hierarchy.");

        return rootResponse.Items
            .Concat(childrenResponse.Items)
            .GroupBy(collection => collection.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static List<Collection> SelectCollections(
        int collectionId,
        List<Collection> collections)
    {
        if (collectionId == 0)
            return collections;

        if (collectionId is -1 or -99)
            return [];

        if (collections.All(collection => collection.Id != collectionId))
            throw new ArgumentOutOfRangeException(
                nameof(collectionId),
                collectionId,
                "The requested collection was not found.");

        var selectedIds = new HashSet<int> { collectionId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var collection in collections)
            {
                if (collection.Parent is not null &&
                    selectedIds.Contains(collection.Parent.Id) &&
                    selectedIds.Add(collection.Id))
                {
                    changed = true;
                }
            }
        }

        return collections
            .Where(collection => selectedIds.Contains(collection.Id))
            .ToList();
    }

    private async Task<(AnalyticsAccumulator Aggregate, int PagesFetched, bool IsComplete, string TerminationReason)>
        GetBookmarksAsync(
            int collectionId,
            List<string> diagnostics,
            CancellationToken cancellationToken)
    {
        var aggregate = new AnalyticsAccumulator();
        var page = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Mcp.Common.ItemsResponse<Raindrop> response;
            try
            {
                response = await raindropsApi.ListAsync(
                    collectionId,
                    search: null,
                    sort: "created",
                    page,
                    perPage: PageSize,
                    nested: collectionId > 0 ? true : null,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                diagnostics.Add($"Bookmark analysis stopped because Raindrop page {page} timed out.");
                return (aggregate, page, false, "api_error");
            }
            catch (Exception)
            {
                diagnostics.Add($"Bookmark analysis stopped because Raindrop page {page} could not be retrieved after retries.");
                return (aggregate, page, false, "api_error");
            }

            if (!response.Result)
            {
                diagnostics.Add($"Bookmark analysis stopped because Raindrop did not return bookmark page {page}.");
                return (aggregate, page, false, "api_error");
            }

            var newBookmarkCount = 0;
            foreach (var bookmark in response.Items)
            {
                if (aggregate.Add(bookmark))
                    newBookmarkCount++;
            }
            page++;

            if (response.Items.Count < PageSize)
                return (aggregate, page, true, "end_of_results");

            if (newBookmarkCount == 0)
            {
                diagnostics.Add($"Bookmark page {page - 1} repeated previously analyzed bookmark IDs.");
                return (aggregate, page, false, "repeated_page");
            }

            if (page >= maximumPages)
            {
                diagnostics.Add($"Bookmark analysis stopped at the configured safety limit of {maximumPages} pages.");
                return (aggregate, page, false, "safety_limit_reached");
            }
        }
    }

    private static IReadOnlyList<CollectionDistribution> BuildCollectionDistribution(
        int requestedCollectionId,
        List<Collection> collections,
        IReadOnlyDictionary<int, int> directCounts,
        int totalBookmarks,
        List<string> diagnostics)
    {
        if (requestedCollectionId is -1 or -99)
        {
            var title = requestedCollectionId == -1 ? "Unsorted" : "Trash";
            var count = directCounts.GetValueOrDefault(requestedCollectionId);
            return
            [
                new CollectionDistribution
                {
                    Id = requestedCollectionId,
                    Title = title,
                    Depth = 0,
                    DirectBookmarkCount = count,
                    DescendantBookmarkCount = 0,
                    SubtreeBookmarkCount = count,
                    Percentage = Percentage(count, totalBookmarks)
                }
            ];
        }

        var byId = collections.ToDictionary(collection => collection.Id);
        var depths = new Dictionary<int, int>();
        var subtreeCounts = new Dictionary<int, int>();

        int GetDepth(Collection collection, HashSet<int> path)
        {
            if (depths.TryGetValue(collection.Id, out var depth))
                return depth;

            if (!path.Add(collection.Id))
            {
                diagnostics.Add($"Collection hierarchy cycle detected at collection {collection.Id}.");
                return depths[collection.Id] = 0;
            }

            if (collection.Id == requestedCollectionId || collection.Parent is null)
                depth = 0;
            else if (!byId.TryGetValue(collection.Parent.Id, out var parent))
            {
                diagnostics.Add($"Collection {collection.Id} references missing parent {collection.Parent.Id}.");
                depth = 0;
            }
            else
                depth = GetDepth(parent, path) + 1;

            path.Remove(collection.Id);
            return depths[collection.Id] = depth;
        }

        int GetSubtreeCount(Collection collection, HashSet<int> path)
        {
            if (subtreeCounts.TryGetValue(collection.Id, out var count))
                return count;

            if (!path.Add(collection.Id))
                return directCounts.GetValueOrDefault(collection.Id);

            count = directCounts.GetValueOrDefault(collection.Id);
            foreach (var child in collections.Where(candidate => candidate.Parent?.Id == collection.Id))
                count += GetSubtreeCount(child, path);

            path.Remove(collection.Id);
            return subtreeCounts[collection.Id] = count;
        }

        var result = new List<CollectionDistribution>(collections.Count + 1);
        foreach (var collection in collections)
        {
            var directCount = directCounts.GetValueOrDefault(collection.Id);
            var subtreeCount = GetSubtreeCount(collection, []);
            result.Add(new CollectionDistribution
            {
                Id = collection.Id,
                Title = collection.Title ?? $"Collection {collection.Id}",
                ParentId = collection.Parent?.Id,
                Depth = GetDepth(collection, []),
                DirectBookmarkCount = directCount,
                DescendantBookmarkCount = subtreeCount - directCount,
                SubtreeBookmarkCount = subtreeCount,
                Percentage = Percentage(subtreeCount, totalBookmarks)
            });
        }

        if (requestedCollectionId == 0 && directCounts.GetValueOrDefault(-1) > 0)
        {
            var count = directCounts[-1];
            result.Add(new CollectionDistribution
            {
                Id = -1,
                Title = "Unsorted",
                Depth = 0,
                DirectBookmarkCount = count,
                DescendantBookmarkCount = 0,
                SubtreeBookmarkCount = count,
                Percentage = Percentage(count, totalBookmarks)
            });
        }

        foreach (var unknownCollectionId in directCounts.Keys.Where(id => id != -1 && !byId.ContainsKey(id)))
            diagnostics.Add($"Bookmarks reference unknown collection {unknownCollectionId}.");

        return result
            .OrderBy(collection => collection.Depth)
            .ThenBy(collection => collection.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(collection => collection.Id)
            .ToList();
    }

    private static IReadOnlyList<ValueDistribution> BuildDistribution(
        IReadOnlyDictionary<string, int> counts,
        int totalBookmarks) => counts
        .Select(pair => new ValueDistribution
        {
            Value = pair.Key,
            Count = pair.Value,
            Percentage = Percentage(pair.Value, totalBookmarks)
        })
        .OrderByDescending(distribution => distribution.Count)
        .ThenBy(distribution => distribution.Value, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static double Percentage(int count, int total) =>
        total == 0 ? 0 : Math.Round(count * 100d / total, 2);

    private static void ValidateCollectionId(int collectionId)
    {
        if (collectionId < 0 && collectionId is not -1 and not -99)
            throw new ArgumentOutOfRangeException(
                nameof(collectionId),
                collectionId,
                "Use 0 for all bookmarks, -1 for Unsorted, -99 for Trash, or a positive collection ID.");
    }

    private sealed class AnalyticsAccumulator
    {
        private readonly HashSet<long> _bookmarkIds = [];

        public Dictionary<int, int> DirectCollectionCounts { get; } = [];
        public Dictionary<string, int> DomainCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> TagCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int BookmarksAnalyzed => _bookmarkIds.Count;
        public int UntaggedBookmarks { get; private set; }
        public int BookmarksWithoutDomains { get; private set; }
        public int BookmarksWithoutExcerpts { get; private set; }
        public int FavoriteBookmarks { get; private set; }

        public bool Add(Raindrop bookmark)
        {
            if (!_bookmarkIds.Add(bookmark.Id))
                return false;

            Increment(DirectCollectionCounts, bookmark.Collection?.Id ?? -1);

            if (string.IsNullOrWhiteSpace(bookmark.Domain))
                BookmarksWithoutDomains++;
            else
                Increment(DomainCounts, bookmark.Domain.Trim());

            var tags = (bookmark.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tags.Count == 0)
                UntaggedBookmarks++;
            else
                foreach (var tag in tags)
                    Increment(TagCounts, tag);

            if (string.IsNullOrWhiteSpace(bookmark.Excerpt))
                BookmarksWithoutExcerpts++;

            if (bookmark.Important == true)
                FavoriteBookmarks++;

            return true;
        }

        private static void Increment<TKey>(Dictionary<TKey, int> counts, TKey key)
            where TKey : notnull => counts[key] = counts.GetValueOrDefault(key) + 1;
    }
}
