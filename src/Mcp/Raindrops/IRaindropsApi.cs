using Refit;
using Mcp.Common;

namespace Mcp.Raindrops;

public interface IRaindropsApi : ICommonApi<Raindrop, long>
{
    [Post("/raindrop")]
    new Task<ItemResponse<Raindrop>> CreateAsync([Body] Raindrop raindrop, CancellationToken cancellationToken);

    [Get("/raindrop/{id}")]
    new Task<ItemResponse<Raindrop>> GetAsync(long id, CancellationToken cancellationToken);

    [Put("/raindrop/{id}")]
    new Task<ItemResponse<Raindrop>> UpdateAsync(long id, [Body] Raindrop raindrop, CancellationToken cancellationToken);

    [Delete("/raindrop/{id}")]
    new Task<SuccessResponse> DeleteAsync(long id, CancellationToken cancellationToken);

    [Get("/raindrops/{collectionId}")]
    Task<ItemsResponse<Raindrop>> ListAsync(int collectionId,
        string? search,
        string? sort,
        int? page,
        [AliasAs("perpage")] int? perPage,
        bool? nested,
        CancellationToken cancellationToken);

    [Post("/raindrops")]
    Task<ItemsResponse<Raindrop>> CreateManyAsync([Body] RaindropCreateManyRequest payload, CancellationToken cancellationToken);

    [Put("/raindrops/{collectionId}")]
    Task<SuccessResponse> UpdateManyAsync(int collectionId, [Body] RaindropBulkUpdate update,
        bool? nested,
        string? search,
        CancellationToken cancellationToken);
}
