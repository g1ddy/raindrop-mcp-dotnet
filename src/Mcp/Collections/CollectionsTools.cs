using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Mcp.Common;
using Mcp.Raindrops;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Collections;

[McpServerToolType]
public class CollectionsTools(ICollectionsApi api, IRaindropsApi raindropsApi, RaindropCacheService cacheService) :
    RaindropToolBase<ICollectionsApi>(api)
{
    private static readonly char[] _separators = ['|', '\n'];
    private static readonly char[] _trimChars = ['-', '*', ' ', '\'', '"', '.'];
    private readonly IRaindropsApi _raindropsApi = raindropsApi;
    private readonly RaindropCacheService _cacheService = cacheService;
    private const int DefaultMaxTokens = 1000;

    private Task<ItemsResponse<Collection>> GetCachedCollectionsAsync(CancellationToken cancellationToken)
        => _cacheService.GetCollectionsAsync(Api.ListAsync, cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Collections"),
    Description("Retrieves all top-level (root) collections. Use this to understand your collection hierarchy before making structural changes.")]
    // The upstream API does not support pagination for this endpoint:
    // https://developer.raindrop.io/v1/collections/methods#get-root-collections
    public Task<ItemsResponse<Collection>> ListCollectionsAsync(CancellationToken cancellationToken) => GetCachedCollectionsAsync(cancellationToken);

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "Get Collection"),
    Description("Retrieves a single collection by its unique ID.")]
    public Task<ItemResponse<Collection>> GetCollectionAsync([Description("The ID of the collection to retrieve")] int id, CancellationToken cancellationToken)
        => Api.GetAsync(id, cancellationToken);

    [McpServerTool(Title = "Create Collection"),
    Description("Creates a new collection. To create a subcollection, include a parent object in the collection parameter.")]
    public async Task<ItemResponse<Collection>> CreateCollectionAsync([Description("The collection details to create")] Collection collection, CancellationToken cancellationToken)
    {
        var response = await Api.CreateAsync(collection, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateCollections();
        }
        return response;
    }

    [McpServerTool(Idempotent = true, Title = "Update Collection"),
    Description("Updates an existing collection.")]
    public async Task<ItemResponse<Collection>> UpdateCollectionAsync(
        [Description("ID of the collection to update")] int id,
        [Description("Updated collection data")] Collection collection, CancellationToken cancellationToken)
    {
        var response = await Api.UpdateAsync(id, collection, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateCollections();
        }
        return response;
    }

    [McpServerTool(Idempotent = true, Title = "Delete Collection"),
    Description("Removes a collection. Bookmarks within it are moved to the Trash.")]
    public async Task<SuccessResponse> DeleteCollectionAsync([Description("ID of the collection to delete")] int id, CancellationToken cancellationToken)
    {
        var response = await Api.DeleteAsync(id, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateCollections();
        }
        return response;
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "List Child Collections"),
    Description("Retrieves all nested (child) collections.")]
    public Task<ItemsResponse<Collection>> ListChildCollectionsAsync(CancellationToken cancellationToken) => Api.ListChildrenAsync(cancellationToken);

    // Pipe is used as a separator in the LLM response, so it must be removed from the input to prevent ambiguity.
    private static readonly SearchValues<char> _sanitizeChars = SearchValues.Create(['|', '\n', '\r', '\v', '\f', '\u0085', '\u2028', '\u2029']);

    // Optimized implementation using a single-pass buffer approach to minimize allocations.
    // Small strings use stack memory (stackalloc), while larger ones rent from the shared ArrayPool.
    // This reduces GC pressure compared to the previous two-pass or StringBuilder approaches.
    internal static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var span = value.AsSpan();
        if (!span.ContainsAny(_sanitizeChars))
        {
            return value;
        }

        int length = value.Length;
        char[]? rented = null;
        Span<char> buffer = length <= 512
            ? stackalloc char[length]
            : (rented = ArrayPool<char>.Shared.Rent(length));

        try
        {
            int newLength = 0;

            for (int i = 0; i < length; i++)
            {
                char c = span[i];

                if (c == '|')
                {
                    continue;
                }

                if (c == '\r')
                {
                    buffer[newLength++] = ' ';
                    if (i + 1 < length && span[i + 1] == '\n')
                    {
                        i++; // Skip \n
                    }
                }
                else if (c == '\n' || c == '\v' || c == '\f' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                {
                    buffer[newLength++] = ' ';
                }
                else
                {
                    buffer[newLength++] = c;
                }
            }

            return new string(buffer.Slice(0, newLength));
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    [McpServerTool(Idempotent = true, Title = "Merge Collections"),
    Description("Merge multiple collections into a destination collection. Requires both the target collection ID and an array of source collection IDs to merge.")]
    public async Task<SuccessResponse> MergeCollectionsAsync(
        [Description("Target collection ID where source collections will be merged")] int to,
        [Description("Collection IDs to merge")] HashSet<int> ids,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
            throw new ArgumentException("At least one source collection ID must be specified.", nameof(ids));

        if (ids.Contains(to))
            throw new ArgumentException("Destination collection cannot be merged into itself.", nameof(ids));

        var payload = new CollectionsMergeRequest { To = to, Ids = ids };
        var response = await Api.MergeAsync(payload, cancellationToken);
        if (response.Result)
        {
            _cacheService.InvalidateCollections();
        }
        return response;
    }

    [McpServerTool(Title = "Suggest Collection for Bookmark"),
     Description("Suggests the best three collections for a bookmark and moves it to the selected collection.")]
    public async Task<SuccessResponse> SuggestCollectionForBookmarkAsync(
        McpServer server,
        [Description("ID of the bookmark to move")] long bookmarkId,
        CancellationToken cancellationToken)
    {
        // 1. Get the bookmark and all collections concurrently
        var bookmarkTask = _raindropsApi.GetAsync(bookmarkId, cancellationToken);
        var collectionsTask = GetCachedCollectionsAsync(cancellationToken);

        await Task.WhenAll(bookmarkTask, collectionsTask);

        var bookmarkResponse = await bookmarkTask;
        if (bookmarkResponse?.Item == null)
        {
            return new SuccessResponse(false);
        }
        var bookmark = bookmarkResponse.Item;

        var collectionsResponse = await collectionsTask;
        if (collectionsResponse?.Items == null || collectionsResponse.Items.Count == 0)
        {
            return new SuccessResponse(false);
        }
        var collectionTitles = new Dictionary<string, Collection>(StringComparer.OrdinalIgnoreCase);
        var promptBuilder = new StringBuilder();

        var filteredCollections = collectionsResponse.Items
            .Where(c => !string.IsNullOrEmpty(c.Title))
            .Where(c => c.Parent == null) // Filter out child collections
            .OrderBy(c => c.Count)
            .Take(25);

        foreach (var c in filteredCollections)
        {
            // We've already filtered for null/empty title above
            var title = Sanitize(c.Title!);
            if (collectionTitles.TryAdd(title, c))
            {
                if (promptBuilder.Length > 0)
                {
                    promptBuilder.Append('\n');
                }
                promptBuilder.Append($"- {title}");
            }
        }

        if (collectionTitles.Count == 0)
        {
            return new SuccessResponse(false);
        }

        // 3. Use the LLM to get the top 3 suggestions
        var prompt = $"""
            Given the following bookmark:
            - Title: {Sanitize(bookmark.Title)}
            - URL: {Sanitize(bookmark.Link)}
            - Excerpt: {Sanitize(bookmark.Excerpt)}

            And the following list of collections:
            {promptBuilder}

            Please suggest the top 3 most relevant collections for this bookmark.
            Return ONLY a pipe-separated list of the collection titles, exactly as they appear in the list above.
            Do not include any explanations, numbering, or bullet points.
            Example: Title1 | Title2 | Title3
        """;

        var sampleRequest = new CreateMessageRequestParams
        {
            MaxTokens = DefaultMaxTokens,
            Messages =
            [
                new SamplingMessage
                {
                    Role = Role.User,
                    Content = [new TextContentBlock { Text = prompt }]
                }
            ]
        };

        var llmResponse = await server.SampleAsync(sampleRequest, cancellationToken);
        if (llmResponse?.Content?.FirstOrDefault() is not TextContentBlock textContent || string.IsNullOrWhiteSpace(textContent.Text))
        {
            return new SuccessResponse(false);
        }

        var suggestedTitles = textContent.Text.Split(_separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim(_trimChars))
            .Where(collectionTitles.ContainsKey)
            .Take(3)
            .ToList();

        if (suggestedTitles.Count == 0)
        {
            return new SuccessResponse(false);
        }

        // 4. Elicit choice from user
        var message = "Here are the top 3 suggested collections for your bookmark:\n" +
                      string.Join("\n", suggestedTitles.Select(st => $"- {st}"));
        message += "\n\nPlease select the collection you want to move the bookmark to.";

        var confirmationRequest = new ElicitRequestParams
        {
            Message = message,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["collectionName"] = new ElicitRequestParams.UntitledSingleSelectEnumSchema
                    {
                        Description = "Collection name",
                        Enum = suggestedTitles
                    }
                }
            }
        };

        var confirmationResponse = await server.ElicitAsync(confirmationRequest, cancellationToken);

        if (confirmationResponse.Action != "accept" ||
            confirmationResponse.Content?.TryGetValue("collectionName", out var value) != true ||
            value.ValueKind != JsonValueKind.String)
        {
            return new SuccessResponse(false);
        }

        // 5. Find the chosen collection
        var chosenCollectionName = value.GetString();
        if (chosenCollectionName == null || !collectionTitles.TryGetValue(chosenCollectionName, out var chosenCollection))
        {
            return new SuccessResponse(false);
        }

        // 6. Move the bookmark
        var updateRequest = new Raindrop { Collection = new IdRef { Id = chosenCollection.Id } };
        var updateResponse = await _raindropsApi.UpdateAsync(bookmarkId, updateRequest, cancellationToken);
        if (updateResponse.Result)
        {
            _cacheService.InvalidateCollections();
        }
        return new SuccessResponse(updateResponse.Result);
    }
}
