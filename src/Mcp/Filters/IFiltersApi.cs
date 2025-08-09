using Refit;

namespace Mcp.Filters;

public interface IFiltersApi
{
    [Get("/filters/{collectionId}")]
    Task<AvailableFilters> GetAsync(long collectionId,
        string? tagsSort,
        string? search,
        CancellationToken cancellationToken);
}
