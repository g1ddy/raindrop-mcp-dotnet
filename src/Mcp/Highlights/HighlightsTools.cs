using System.ComponentModel;
using ModelContextProtocol.Server;
using Mcp.Common;

namespace Mcp.Highlights;

[McpServerToolType]
public class HighlightsTools(IHighlightsApi api) : RaindropToolBase<IHighlightsApi>(api)
{
    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "List Highlights"),
     Description("Retrieves all highlights across all bookmarks.")]
    public Task<ItemsResponse<Highlight>> ListHighlightsAsync(
        [Description("Page number starting from 0")] int? page = null,
        [Description("Items per page")] int? perPage = null,
        CancellationToken cancellationToken = default)
        => Api.ListAsync(page, perPage, cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "List Highlights By Collection"),
     Description("Retrieves highlights in a specific collection.")]
    public Task<ItemsResponse<Highlight>> ListHighlightsByCollectionAsync(
        [Description("Collection ID containing the bookmarks")] int collectionId,
        [Description("Page number starting from 0")] int? page = null,
        [Description("Items per page")] int? perPage = null,
        CancellationToken cancellationToken = default)
        => Api.ListByCollectionAsync(collectionId, page, perPage, cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "Get Bookmark Highlights"),
     Description("Retrieves all highlights for a specific bookmark.")]
    public Task<ItemResponse<RaindropHighlights>> GetBookmarkHighlightsAsync([
        Description("The unique identifier of the bookmark to retrieve highlights from.")] long raindropId,
        CancellationToken cancellationToken) => Api.GetAsync(raindropId, cancellationToken);

    [McpServerTool(Title = "Create Highlight"),
     Description("Adds a new highlight to a bookmark.")]
    public Task<ItemResponse<HighlightBulkUpdateRequest>> CreateHighlightAsync(
        [Description("The unique identifier of the bookmark to add the highlight to.")] long raindropId,
        [Description("The request object containing the details of the highlight to create.")] HighlightCreateRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new HighlightBulkUpdateRequest
        {
            Highlights = [new HighlightBulkUpdate { Text = request.Text, Color = request.Color, Note = request.Note }]
        };
        return Api.UpdateAsync(raindropId, payload, cancellationToken);
    }

    [McpServerTool(Idempotent = true, Title = "Update Highlight"),
     Description("Updates an existing highlight on a bookmark.")]
    public Task<ItemResponse<HighlightBulkUpdateRequest>> UpdateHighlightAsync(
        [Description("The unique identifier of the bookmark containing the highlight to update.")] long raindropId,
        [Description("The request object containing the updated details for the highlight.")] HighlightUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new HighlightBulkUpdateRequest
        {
            Highlights = [new HighlightBulkUpdate { Id = request.Id, Text = request.Text, Color = request.Color, Note = request.Note }]
        };
        return Api.UpdateAsync(raindropId, payload, cancellationToken);
    }

    [McpServerTool(Idempotent = true, Title = "Delete Highlight"),
     Description("Removes a highlight from a bookmark.")]
    public Task<ItemResponse<HighlightBulkUpdateRequest>> DeleteHighlightAsync(
        [Description("The unique identifier of the bookmark containing the highlight to remove.")] long raindropId,
        [Description("The unique identifier of the highlight to remove.")] string highlightId,
        CancellationToken cancellationToken)
    {
        var payload = new HighlightBulkUpdateRequest
        {
            Highlights = [new HighlightBulkUpdate { Id = highlightId, Text = string.Empty }]
        };
        return Api.UpdateAsync(raindropId, payload, cancellationToken);
    }
}
