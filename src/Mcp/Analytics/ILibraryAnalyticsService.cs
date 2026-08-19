namespace Mcp.Analytics;

public interface ILibraryAnalyticsService
{
    Task<LibraryAnalyticsReport> AnalyzeAsync(int collectionId, CancellationToken cancellationToken);
}
