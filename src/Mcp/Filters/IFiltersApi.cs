using Refit;

namespace Mcp.Filters;

public interface IFiltersApi
{
    [Get("/filters/{collectionId}")]
    Task<AvailableFilters> GetAsync(long collectionId,
        string? tagsSort = null,
        string? search = null);
}
