using System.ComponentModel;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Mcp.Common;

namespace Mcp.Filters;

[McpServerToolType]
public class FiltersTools(IFiltersApi api, IRaindropCacheService cacheService, IOptions<RaindropOptions> options) : RaindropToolBase<IFiltersApi>(api)
{
    private readonly IRaindropCacheService _cacheService = cacheService;
    private readonly string _cacheKey = options.Value.ApiToken;

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get Available Filters"),
     Description("Retrieves available filters for a specific collection or all bookmarks.")]
    public Task<AvailableFilters> GetAvailableFiltersAsync(
        [Description("The ID of the collection to retrieve filters for. Use 0 for all collections.")] long collectionId,
        [Description("Sort tags by '-count' (descending by count, default) or '_id' (ascending by name).")] string? tagsSort = null,
        [Description(SearchSyntax.Description)] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (tagsSort is not null && tagsSort != "-count" && tagsSort != "_id")
            throw new ArgumentOutOfRangeException(nameof(tagsSort), tagsSort, "Valid values are '-count' or '_id'.");

        return _cacheService.GetAvailableFiltersAsync(
            _cacheKey,
            collectionId,
            tagsSort,
            search,
            ct => Api.GetAsync(collectionId, tagsSort, search, ct),
            cancellationToken);
    }
}
