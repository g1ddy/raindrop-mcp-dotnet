using Refit;
using Mcp.Common;

namespace Mcp.Highlights;

public interface IHighlightsApi
{
    [Get("/highlights")]
    Task<ItemsResponse<Highlight>> ListAsync(
        int? page,
        int? perPage,
        CancellationToken cancellationToken);

    [Get("/highlights/{collectionId}")]
    Task<ItemsResponse<Highlight>> ListByCollectionAsync(int collectionId,
        int? page,
        int? perPage,
        CancellationToken cancellationToken);

    [Get("/raindrop/{id}")]
    Task<ItemResponse<RaindropHighlights>> GetAsync(long id, CancellationToken cancellationToken);

    [Put("/raindrop/{id}")]
    Task<ItemResponse<HighlightBulkUpdateRequest>> UpdateAsync(long id, [Body] HighlightBulkUpdateRequest payload, CancellationToken cancellationToken);
}
