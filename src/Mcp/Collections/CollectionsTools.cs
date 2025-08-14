using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ModelContextProtocol.Protocol;
using Mcp.Raindrops;

using ModelContextProtocol.Server;

using Mcp.Common;

namespace Mcp.Collections;

[McpServerToolType]
public class CollectionsTools(ICollectionsApi api, IRaindropsApi raindropsApi) :
    RaindropToolBase<ICollectionsApi>(api)
{
    private readonly IRaindropsApi _raindropsApi = raindropsApi;
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

    [McpServerTool(Title = "Suggest Collection for Bookmark"),
     Description("Suggests the best three collections for a bookmark and moves it to the selected collection.")]
    public async Task<SuccessResponse> SuggestCollectionForBookmarkAsync(
        [Description("ID of the bookmark to move")] long bookmarkId,
        IMcpServer server,
        CancellationToken token)
    {
        // 1. Get the bookmark
        var bookmarkResponse = await _raindropsApi.GetAsync(bookmarkId);
        if (bookmarkResponse?.Item == null)
        {
            return new SuccessResponse(false);
        }
        var bookmark = bookmarkResponse.Item;

        // 2. Get all collections
        var collectionsResponse = await Api.ListAsync();
        if (collectionsResponse?.Items == null || collectionsResponse.Items.Count == 0)
        {
            return new SuccessResponse(false);
        }
        var allCollections = collectionsResponse.Items;

        // 3. Use the LLM to get the top 3 suggestions
        var prompt = $"""
        Given the following bookmark:
        - Title: {bookmark.Title}
        - URL: {bookmark.Link}
        - Excerpt: {bookmark.Excerpt}

        And the following list of collections:
        {string.Join("\n", allCollections.Select(c => $"- {c.Title}"))}

        Please suggest the top 3 most relevant collections for this bookmark.
        Return a comma-separated list of the collection titles.
        """;

        var sampleRequest = new CreateMessageRequestParams
        {
            Messages =
            [
                new SamplingMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = prompt }
                }
            ]
        };

        var llmResponse = await server.SampleAsync(sampleRequest, token);
        if (llmResponse?.Content is not TextContentBlock textContent || string.IsNullOrWhiteSpace(textContent.Text))
        {
            return new SuccessResponse(false);
        }

        var suggestedTitles = textContent.Text.Split(',').Select(t => t.Trim()).ToList();
        var suggestions = allCollections
            .Where(c => suggestedTitles.Contains(c.Title, StringComparer.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (suggestions.Count == 0)
        {
            return new SuccessResponse(false);
        }

        // 4. Elicit choice from user
        var message = "Here are the top 3 suggested collections for your bookmark:\n" +
                      string.Join("\n", suggestions.Select(c => $"- {c.Title}"));
        message += "\n\nPlease select the collection you want to move the bookmark to.";

        var confirmationRequest = new ElicitRequestParams
        {
            Message = message,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["collectionName"] = new ElicitRequestParams.EnumSchema
                    {
                        Description = "Collection name",
                        Enum = suggestions.Select(c => c.Title).ToList()
                    }
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

        // 5. Find the chosen collection
        var chosenCollectionName = value.GetString();
        var chosenCollection = suggestions.FirstOrDefault(c => string.Equals(c.Title, chosenCollectionName, StringComparison.OrdinalIgnoreCase));

        if (chosenCollection == null)
        {
            return new SuccessResponse(false);
        }

        // 6. Move the bookmark
        var updateRequest = new Raindrop { Collection = new IdRef { Id = chosenCollection.Id } };
        var updateResponse = await _raindropsApi.UpdateAsync(bookmarkId, updateRequest);
        return new SuccessResponse(updateResponse.Result);
    }
}
