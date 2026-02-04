using System.ComponentModel;
using System.Text;
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
    private static readonly char[] _separators = ['|', '\n'];
    private static readonly char[] _trimChars = ['-', '*', ' ', '\'', '"', '.'];
    private readonly IRaindropsApi _raindropsApi = raindropsApi;
    private const int DefaultMaxTokens = 1000;

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

    // Pipe is used as a separator in the LLM response, so it must be removed from the input to prevent ambiguity.
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int length = value.Length;
        bool needsSanitization = false;

        // Pass 0: Check if sanitization is needed
        for (int i = 0; i < length; i++)
        {
            char c = value[i];
            // Check for pipe or any newline character
            if (c == '|' || c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                needsSanitization = true;
                break;
            }
        }

        if (!needsSanitization)
        {
            return value;
        }

        // Pass 1: Calculate new length
        int newLength = 0;

        for (int i = 0; i < length; i++)
        {
            char c = value[i];

            if (c == '|')
            {
                continue;
            }

            if (c == '\r')
            {
                newLength++; // Replaced by space
                if (i + 1 < length && value[i + 1] == '\n')
                {
                    i++; // Skip \n
                }
            }
            else if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
            {
                newLength++; // Replaced by space
            }
            else
            {
                newLength++;
            }
        }

        // Pass 2: Create string
        return string.Create(newLength, value, (span, state) =>
        {
            var input = state.AsSpan();
            int inputLen = input.Length;
            int idx = 0;

            for (int i = 0; i < inputLen; i++)
            {
                char c = input[i];

                if (c == '|')
                {
                    continue;
                }

                if (c == '\r')
                {
                    span[idx++] = ' ';
                    if (i + 1 < inputLen && input[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else if (c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
                {
                    span[idx++] = ' ';
                }
                else
                {
                    span[idx++] = c;
                }
            }
        });
    }

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
        McpServer server,
        [Description("ID of the bookmark to move")] long bookmarkId,
        CancellationToken cancellationToken)
    {
        // 1. Get the bookmark and all collections concurrently
        var bookmarkTask = _raindropsApi.GetAsync(bookmarkId, cancellationToken);
        var collectionsTask = Api.ListAsync(cancellationToken);

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

        foreach (var c in collectionsResponse.Items)
        {
            if (!string.IsNullOrEmpty(c.Title))
            {
                var title = Sanitize(c.Title);
                if (collectionTitles.TryAdd(title, c))
                {
                    if (promptBuilder.Length > 0)
                    {
                        promptBuilder.Append('\n');
                    }
                    promptBuilder.Append($"- {title}");
                }
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
        return new SuccessResponse(updateResponse.Result);
    }
}
