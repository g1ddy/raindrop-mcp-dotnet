using Refit;
using Mcp.Common;

namespace Mcp.Collections;

public interface ICollectionsApi : ICommonApi<Collection, int>
{
    [Get("/collections")]
    Task<ItemsResponse<Collection>> ListAsync();

    [Get("/collection/{id}")]
    new Task<ItemResponse<Collection>> GetAsync(int id);

    [Post("/collection")]
    new Task<ItemResponse<Collection>> CreateAsync([Body] Collection collection);

    [Put("/collection/{id}")]
    new Task<ItemResponse<Collection>> UpdateAsync(int id, [Body] Collection collection);

    [Delete("/collection/{id}")]
    new Task<SuccessResponse> DeleteAsync(int id);

    [Get("/collections/childrens")]
    Task<ItemsResponse<Collection>> ListChildrenAsync();

    [Put("/collections/merge")]
    Task<SuccessResponse> MergeAsync([Body] CollectionsMergeRequest payload);
}
