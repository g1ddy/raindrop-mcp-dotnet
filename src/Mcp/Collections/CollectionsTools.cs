using System.ComponentModel;
using System.Text.Json;
using Mcp.Common;
using Mcp.Raindrops;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Collections;

[McpServerToolType]
public class CollectionsTools(ICollectionsApi api, IRaindropsApi raindropsApi) :
    RaindropToolBase<ICollectionsApi>(api)
{
    private readonly IRaindropsApi _raindropsApi = raindropsApi;
    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Collections"),
    Description("Retrieves all top-level (root) collections. Use this to understand your collection hierarchy before making structural changes.")]
    public Task<ItemsResponse<Collection>> ListCollectionsAsync(CancellationToken cancellationToken) => Api.ListAsync(cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "Get Collection"),
    Description("Retrieves a single collection by its unique ID.")]
    public Task<ItemResponse<Collection>> GetCollectionAsync([Description("The ID of the collection to retrieve")] int id, CancellationToken cancellationToken)
        => Api.GetAsync(id, cancellationToken);

    [McpServerTool(Title = "Create Collection"),
    Description("Creates a new collection. To create a subcollection, include a parent object in the collection parameter.")]
    public Task<ItemResponse<Collection>> CreateCollectionAsync([Description("The collection details to create")] Collection collection, CancellationToken cancellationToken)
        => Api.CreateAsync(collection, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Update Collection"),
    Description("Updates an existing collection.")]
    public Task<ItemResponse<Collection>> UpdateCollectionAsync(
        [Description("ID of the collection to update")] int id,
        [Description("Updated collection data")] Collection collection, CancellationToken cancellationToken)
        => Api.UpdateAsync(id, collection, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Delete Collection"),
    Description("Removes a collection. Bookmarks within it are moved to the Trash.")]
    public Task<SuccessResponse> DeleteCollectionAsync([Description("ID of the collection to delete")] int id, CancellationToken cancellationToken)
        => Api.DeleteAsync(id, cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Child Collections"),
    Description("Retrieves all nested (child) collections.")]
    public Task<ItemsResponse<Collection>> ListChildCollectionsAsync(CancellationToken cancellationToken) => Api.ListChildrenAsync(cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Merge Collections"),
    Description("Merge multiple collections into a destination collection. Requires both the target collection ID and an array of source collection IDs to merge.")]
    public Task<SuccessResponse> MergeCollectionsAsync(
        [Description("Target collection ID where source collections will be merged")] int to,
        [Description("Collection IDs to merge")] List<int> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
            throw new ArgumentException("At least one source collection ID must be specified.", nameof(ids));

        if (ids.Contains(to))
            throw new ArgumentException("Destination collection cannot be merged into itself.", nameof(ids));

        var payload = new CollectionsMergeRequest { To = to, Ids = ids };
        return Api.MergeAsync(payload, cancellationToken);
    }

    [McpServerTool(Title = "Suggest Collection for Bookmark"),
     Description("Suggests the best three collections for a bookmark and moves it to the selected collection.")]
    public async Task<SuccessResponse> SuggestCollectionForBookmarkAsync(
        [Description("ID of the bookmark to move")] long bookmarkId,
        IMcpServer server,
        CancellationToken token)
    {
        // 1. Get all collections
        var collectionsResponse = await Api.ListAsync(token);
        if (collectionsResponse?.Items == null || collectionsResponse.Items.Count == 0)
        {
            return new SuccessResponse(false);
        }

        // 2. Take the top 3 as suggestions
        var suggestions = collectionsResponse.Items.Take(3).ToList();
        if (suggestions.Count == 0)
        {
            return new SuccessResponse(false);
        }

        // 3. Elicit choice from user
        var message = "Here are the top 3 suggested collections for your bookmark:\\n" +
                      string.Join("\\n", suggestions.Select(c => $"- {c.Title}"));
        message += "\\n\\nPlease type the name of the collection you want to move the bookmark to.";

        var confirmationRequest = new ElicitRequestParams
        {
            Message = message,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["collectionName"] = new ElicitRequestParams.StringSchema { Description = "Collection name" }
                }
            }
        };

        var confirmationResponse = await server.ElicitAsync(confirmationRequest, token);

        if (confirmationResponse.Action != "accept" ||
            confirmationResponse.Content?.TryGetValue("collectionName", out var value) != true ||
            value.ValueKind != JsonValueKind.String)
        {
            return new SuccessResponse(false);
        }

        // 4. Find the chosen collection
        var chosenCollectionName = value.GetString();
        var chosenCollection = suggestions.FirstOrDefault(c => string.Equals(c.Title, chosenCollectionName, StringComparison.OrdinalIgnoreCase));

        if (chosenCollection == null)
        {
            return new SuccessResponse(false);
        }

        // 5. Move the bookmark
        var updateRequest = new Raindrop { Collection = new IdRef { Id = chosenCollection.Id } };
        var updateResponse = await _raindropsApi.UpdateAsync(bookmarkId, updateRequest, token);
        return new SuccessResponse(updateResponse.Result);
    }
}
