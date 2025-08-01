using Refit;
using Mcp.Common;

namespace Mcp.Tags;

public interface ITagsApi
{
    [Get("/tags")]
    Task<ItemsResponse<TagInfo>> ListAsync();

    [Get("/tags/{collectionId}")]
    Task<ItemsResponse<TagInfo>> ListForCollectionAsync(int collectionId);

    [Put("/tags")]
    Task<SuccessResponse> UpdateAsync([Body] TagRenameRequest payload);

    [Put("/tags/{collectionId}")]
    Task<SuccessResponse> UpdateForCollectionAsync(int collectionId, [Body] TagRenameRequest payload);

    [Delete("/tags")]
    Task<SuccessResponse> DeleteAsync([Body] TagDeleteRequest payload);

    [Delete("/tags/{collectionId}")]
    Task<SuccessResponse> DeleteForCollectionAsync(int collectionId, [Body] TagDeleteRequest payload);
}
