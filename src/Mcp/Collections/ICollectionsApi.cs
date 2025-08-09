using Refit;
using Mcp.Common;

namespace Mcp.Collections;

public interface ICollectionsApi : ICommonApi<Collection, int>
{
    [Get("/collections")]
    Task<ItemsResponse<Collection>> ListAsync(CancellationToken cancellationToken);

    [Get("/collection/{id}")]
    new Task<ItemResponse<Collection>> GetAsync(int id, CancellationToken cancellationToken);

    [Post("/collection")]
    new Task<ItemResponse<Collection>> CreateAsync([Body] Collection collection, CancellationToken cancellationToken);

    [Put("/collection/{id}")]
    new Task<ItemResponse<Collection>> UpdateAsync(int id, [Body] Collection collection, CancellationToken cancellationToken);

    [Delete("/collection/{id}")]
    new Task<SuccessResponse> DeleteAsync(int id, CancellationToken cancellationToken);

    [Get("/collections/childrens")]
    Task<ItemsResponse<Collection>> ListChildrenAsync(CancellationToken cancellationToken);

    [Put("/collections/merge")]
    Task<SuccessResponse> MergeAsync([Body] CollectionsMergeRequest payload, CancellationToken cancellationToken);
}
