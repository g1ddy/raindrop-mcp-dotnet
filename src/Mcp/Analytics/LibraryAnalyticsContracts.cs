namespace Mcp.Analytics;

public sealed record LibraryAnalyticsReport
{
    public required LibraryAnalyticsScope Scope { get; init; }
    public required LibraryAnalyticsSummary Summary { get; init; }
    public required IReadOnlyList<CollectionDistribution> Collections { get; init; }
    public required IReadOnlyList<ValueDistribution> Domains { get; init; }
    public required IReadOnlyList<ValueDistribution> Tags { get; init; }
    public required IReadOnlyList<string> Diagnostics { get; init; }
}

public sealed record LibraryAnalyticsScope
{
    public required int CollectionId { get; init; }
    public required bool IncludesDescendants { get; init; }
    public required int PagesFetched { get; init; }
    public required bool IsComplete { get; init; }
    public required string TerminationReason { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}

public sealed record LibraryAnalyticsSummary
{
    public required int BookmarksAnalyzed { get; init; }
    public required int RootCollections { get; init; }
    public required int ChildCollections { get; init; }
    public required int MaximumCollectionDepth { get; init; }
    public required int UniqueDomains { get; init; }
    public required int UniqueTags { get; init; }
    public required int UntaggedBookmarks { get; init; }
    public required int BookmarksWithoutDomains { get; init; }
    public required int BookmarksWithoutExcerpts { get; init; }
    public required int FavoriteBookmarks { get; init; }
    public required int UnsortedBookmarks { get; init; }
}

public sealed record CollectionDistribution
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public int? ParentId { get; init; }
    public required int Depth { get; init; }
    public required int DirectBookmarkCount { get; init; }
    public required int DescendantBookmarkCount { get; init; }
    public required int SubtreeBookmarkCount { get; init; }
    public required double Percentage { get; init; }
}

public sealed record ValueDistribution
{
    public required string Value { get; init; }
    public required int Count { get; init; }
    public required double Percentage { get; init; }
}
