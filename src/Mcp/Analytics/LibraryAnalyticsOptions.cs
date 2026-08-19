using System.ComponentModel.DataAnnotations;

namespace Mcp.Analytics;

public sealed class LibraryAnalyticsOptions
{
    public const int DefaultMaximumPages = 20;

    [Range(1, 10_000)]
    public int MaximumPages { get; set; } = DefaultMaximumPages;
}
