using Refit;
using Mcp.Common;

namespace Mcp.Highlights;

public interface IHighlightsApi
{
    [Get("/highlights")]
    Task<ItemsResponse<Highlight>> ListAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    [Get("/highlights/{collectionId}")]
    Task<ItemsResponse<Highlight>> ListByCollectionAsync(int collectionId,
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    [Get("/raindrop/{id}")]
    Task<ItemResponse<RaindropHighlights>> GetAsync(long id, CancellationToken cancellationToken);

    [Put("/raindrop/{id}")]
    Task<ItemResponse<HighlightBulkUpdateRequest>> UpdateAsync(long id, [Body] HighlightBulkUpdateRequest payload, CancellationToken cancellationToken);
}
