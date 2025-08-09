using Refit;
using Mcp.Common;

namespace Mcp.Tags;

public interface ITagsApi
{
    [Get("/tags")]
    Task<ItemsResponse<TagInfo>> ListAsync(CancellationToken cancellationToken);

    [Get("/tags/{collectionId}")]
    Task<ItemsResponse<TagInfo>> ListForCollectionAsync(int collectionId, CancellationToken cancellationToken);

    [Put("/tags")]
    Task<SuccessResponse> UpdateAsync([Body] TagRenameRequest payload, CancellationToken cancellationToken);

    [Put("/tags/{collectionId}")]
    Task<SuccessResponse> UpdateForCollectionAsync(int collectionId, [Body] TagRenameRequest payload, CancellationToken cancellationToken);

    [Delete("/tags")]
    Task<SuccessResponse> DeleteAsync([Body] TagDeleteRequest payload, CancellationToken cancellationToken);

    [Delete("/tags/{collectionId}")]
    Task<SuccessResponse> DeleteForCollectionAsync(int collectionId, [Body] TagDeleteRequest payload, CancellationToken cancellationToken);
}
