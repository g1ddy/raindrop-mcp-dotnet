using System.ComponentModel;
using ModelContextProtocol.Server;
using Mcp.Common;

namespace Mcp.Raindrops;

[McpServerToolType]
public class RaindropsTools(IRaindropsApi api) :
    RaindropToolBase<IRaindropsApi>(api)
{
    private static readonly HashSet<string> ValidSortOptions = new(
        new[] { "created", "-created", "title", "-title", "domain", "-domain", "sort", "score" }
    );

    [McpServerTool(Title = "Create Bookmark"),
         Description("Creates a new bookmark.")]
    public Task<ItemResponse<Raindrop>> CreateBookmarkAsync(
            [Description("Bookmark creation details")] RaindropCreateRequest request, CancellationToken cancellationToken)
    {
        var payload = request.ToRaindrop();
        return Api.CreateAsync(payload, cancellationToken);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
            Title = "Get Bookmark"),
         Description("Retrieves a single bookmark by its unique ID.")]
    public Task<ItemResponse<Raindrop>> GetBookmarkAsync([
            Description("ID of the bookmark to retrieve")] long id, CancellationToken cancellationToken)
            => Api.GetAsync(id, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Update Bookmark"),
         Description("Updates an existing bookmark.")]
    public Task<ItemResponse<Raindrop>> UpdateBookmarkAsync(
            [Description("ID of the bookmark to update")] long id,
            [Description("Updated bookmark data")] RaindropUpdateRequest request, CancellationToken cancellationToken)
    {
        var payload = request.ToRaindrop();
        return Api.UpdateAsync(id, payload, cancellationToken);
    }

    [McpServerTool(Idempotent = true, Title = "Delete Bookmark"),
         Description("Moves a bookmark to the Trash.")]
    public Task<SuccessResponse> DeleteBookmarkAsync([
            Description("ID of the bookmark to delete")] long id, CancellationToken cancellationToken)
            => Api.DeleteAsync(id, cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
            Title = "List Bookmarks"),
         Description("Retrieves a list of bookmarks from a specific collection. For large collections, use pagination with perPage=50 to retrieve all bookmarks.")]
    public Task<ItemsResponse<Raindrop>> ListBookmarksAsync(
            [Description("The ID of the collection to retrieve bookmarks from. Use 0 for all, -1 for unsorted, -99 for trash.")] int collectionId,
            [Description(SearchSyntax.Description)] string? search = null,
            [Description("Sorting order: '-created' (newest, default), 'created', 'score' (relevance when searching), 'sort', 'title', '-title', 'domain', '-domain'.")] string? sort = null,
            [Description("Page index starting from 0.")] int? page = null,
            [Description("How many raindrops per page, up to 50.")] int? perPage = null,
            [Description("Include bookmarks from nested collections (true/false).")] bool? nested = null,
            CancellationToken cancellationToken = default)
    {
        if (page is < 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page number cannot be negative.");

        if (perPage is > 50 or < 1)
            throw new ArgumentOutOfRangeException(nameof(perPage), "Number of items per page must be between 1 and 50.");

        if (sort is not null)
        {
            if (!ValidSortOptions.Contains(sort))
                throw new ArgumentOutOfRangeException(nameof(sort), $"Valid values are '{string.Join("', '", ValidSortOptions)}'.");

            if (sort == "score" && string.IsNullOrWhiteSpace(search))
                throw new ArgumentException("Sort 'score' is only allowed when using a search query.", nameof(sort));
        }

        return Api.ListAsync(collectionId, search, sort, page, perPage, nested, cancellationToken);
    }

    [McpServerTool(Title = "Create Bookmarks"),
         Description("Creates multiple bookmarks in a single request.")]
    public Task<ItemsResponse<Raindrop>> CreateBookmarksAsync(
            [Description("Collection ID for the new bookmarks")] int collectionId,
            [Description("A collection of bookmark details to create.")] IEnumerable<Raindrop> raindrops,
            CancellationToken cancellationToken = default)
    {
        var payload = new RaindropCreateManyRequest { CollectionId = collectionId, Items = raindrops.ToList() };
        return Api.CreateManyAsync(payload, cancellationToken);
    }

    [McpServerTool(Idempotent = true, Title = "Update Bookmarks"),
     Description("Bulk update bookmarks in a collection. For precise targeting, use the ids parameter in the update object.")]
    public Task<SuccessResponse> UpdateBookmarksAsync(
        [Description("Collection to update")] int collectionId,
        [Description("Update operations to apply")] RaindropBulkUpdate update,
        [Description("Apply to nested collections")] bool? nested = null,
        [Description("Optional search filter. Use cautiously as it may affect more bookmarks than intended.")] string? search = null,
        CancellationToken cancellationToken = default)
        => Api.UpdateManyAsync(collectionId, update, nested, search, cancellationToken);
}
