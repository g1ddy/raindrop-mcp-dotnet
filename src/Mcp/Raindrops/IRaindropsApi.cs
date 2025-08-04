using Refit;
using Mcp.Common;

namespace Mcp.Raindrops;

public interface IRaindropsApi : ICommonApi<Raindrop, long>
{
    [Post("/raindrop")]
    new Task<ItemResponse<Raindrop>> CreateAsync([Body] Raindrop raindrop);

    [Get("/raindrop/{id}")]
    new Task<ItemResponse<Raindrop>> GetAsync(long id);

    [Put("/raindrop/{id}")]
    new Task<ItemResponse<Raindrop>> UpdateAsync(long id, [Body] Raindrop raindrop);

    [Delete("/raindrop/{id}")]
    new Task<SuccessResponse> DeleteAsync(long id);

    [Get("/raindrops/{collectionId}")]
    Task<ItemsResponse<Raindrop>> ListAsync(int collectionId,
        string? search = null,
        string? sort = null,
        int? page = null,
        [AliasAs("perpage")] int? perPage = null,
        bool? nested = null);

    [Post("/raindrops")]
    Task<ItemsResponse<Raindrop>> CreateManyAsync([Body] RaindropCreateManyRequest payload);

    [Put("/raindrops/{collectionId}")]
    Task<SuccessResponse> UpdateManyAsync(int collectionId, [Body] RaindropBulkUpdate update,
        bool? nested = null, string? search = null);
}
