using System.ComponentModel;

using ModelContextProtocol.Server;

using Mcp.Common;

namespace Mcp.Collections;

[McpServerToolType]
public class CollectionsTools(ICollectionsApi api) :
    RaindropToolBase<ICollectionsApi>(api)
{
    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Collections"),
    Description("Retrieves all top-level (root) collections. Use this to understand your collection hierarchy before making structural changes.")]
    public Task<ItemsResponse<Collection>> ListCollectionsAsync() => Api.ListAsync();

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "Get Collection"),
    Description("Retrieves a single collection by its unique ID.")]
    public Task<ItemResponse<Collection>> GetCollectionAsync([Description("The ID of the collection to retrieve")] int id)
        => Api.GetAsync(id);

    [McpServerTool(Title = "Create Collection"),
    Description("Creates a new collection. To create a subcollection, include a parent object in the collection parameter.")]
    public Task<ItemResponse<Collection>> CreateCollectionAsync([Description("The collection details to create")] Collection collection)
        => Api.CreateAsync(collection);

    [McpServerTool(Idempotent = true, Title = "Update Collection"),
    Description("Updates an existing collection.")]
    public Task<ItemResponse<Collection>> UpdateCollectionAsync(
        [Description("ID of the collection to update")] int id,
        [Description("Updated collection data")] Collection collection)
        => Api.UpdateAsync(id, collection);

    [McpServerTool(Idempotent = true, Title = "Delete Collection"),
    Description("Removes a collection. Bookmarks within it are moved to the Trash.")]
    public Task<SuccessResponse> DeleteCollectionAsync([Description("ID of the collection to delete")] int id)
        => Api.DeleteAsync(id);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Child Collections"),
    Description("Retrieves all nested (child) collections.")]
    public Task<ItemsResponse<Collection>> ListChildCollectionsAsync() => Api.ListChildrenAsync();

    [McpServerTool(Idempotent = true, Title = "Merge Collections"),
    Description("Merge multiple collections into a destination collection. Requires both the target collection ID and an array of source collection IDs to merge.")]
    public Task<SuccessResponse> MergeCollectionsAsync(
        [Description("Target collection ID where source collections will be merged")] int to,
        [Description("Collection IDs to merge")] List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
            throw new ArgumentException("At least one source collection ID must be specified.", nameof(ids));

        if (ids.Contains(to))
            throw new ArgumentException("Destination collection cannot be merged into itself.", nameof(ids));

        var payload = new CollectionsMergeRequest { To = to, Ids = ids };
        return Api.MergeAsync(payload);
    }
}
