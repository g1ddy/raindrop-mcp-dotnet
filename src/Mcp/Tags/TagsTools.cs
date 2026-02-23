using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mcp.Common;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Tags;

[McpServerToolType]
public class TagsTools(ITagsApi api, RaindropCacheService cacheService, IOptions<RaindropOptions> options) : RaindropToolBase<ITagsApi>(api)
{
    private readonly RaindropCacheService _cacheService = cacheService;
    private readonly string _cacheKey = options.Value.ApiToken;

    private Task<ItemsResponse<TagInfo>> GetCachedTagsAsync(CancellationToken cancellationToken)
        => _cacheService.GetTagsAsync(_cacheKey, Api.ListAsync, cancellationToken);

    private async Task<bool> ConfirmActionAsync(McpServer server, string message, CancellationToken cancellationToken)
    {
        var confirmationRequest = new ElicitRequestParams
        {
            Message = message,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["confirm"] = new ElicitRequestParams.BooleanSchema { Description = "Confirm action" }
                }
            }
        };

        var confirmationResponse = await server.ElicitAsync(confirmationRequest, cancellationToken);

        return confirmationResponse.Action == "accept" &&
               confirmationResponse.Content != null &&
               confirmationResponse.Content.TryGetValue("confirm", out var value) &&
               value.ValueKind == JsonValueKind.True;
    }

    private async Task<SuccessResponse> ExecuteAndInvalidateAsync(Task<SuccessResponse> apiTask)
    {
        var response = await apiTask;
        if (response?.Result == true)
        {
            _cacheService.InvalidateTags(_cacheKey);
        }
        return response ?? new SuccessResponse(false);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true, Title = "List Tags"),
     Description("Retrieves all tags, optionally filtered by a collection.")]
    public Task<ItemsResponse<TagInfo>> ListTagsAsync(
        [Description("Optional collection ID to filter tags by.")] int? collectionId = null,
        CancellationToken cancellationToken = default)
        => collectionId is null
            ? GetCachedTagsAsync(cancellationToken)
            : Api.ListForCollectionAsync(collectionId.Value, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Rename Tag"),
     Description("Renames a tag across all bookmarks.")]
    public Task<SuccessResponse> RenameTagAsync(
        McpServer server,
        [Description("The current name of the tag to rename.")] string oldTag,
        [Description("The new name for the tag.")] string newTag,
        [Description("Collection ID if scoped")] int? collectionId = null,
        CancellationToken cancellationToken = default)
        => RenameTagsAsync(server, [oldTag], newTag, collectionId, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Rename Tags"),
     Description("Merges multiple tags into a single destination tag.")]
    public async Task<SuccessResponse> RenameTagsAsync(
        McpServer server,
        [Description("A collection of tag names to be merged.")] IEnumerable<string> tags,
        [Description("The name of the tag that the source tags will be merged into.")] string newTag,
        [Description("Collection ID if scoped")] int? collectionId = null,
        CancellationToken cancellationToken = default)
    {
        var tagsList = tags.ToList();
        const string BaseMessage = "Are you sure you want to merge these tags? This action cannot be undone.";

        string message = TagFormatter.FormatConfirmationMessage(tagsList, BaseMessage);
        message += $"\n\nThey will be merged into: \"{newTag}\"";

        if (!await ConfirmActionAsync(server, message, cancellationToken))
        {
            return new SuccessResponse(false);
        }

        var payload = new TagRenameRequest { Replace = newTag, Tags = tagsList };
        return await ExecuteAndInvalidateAsync(collectionId is null
            ? Api.UpdateAsync(payload, cancellationToken)
            : Api.UpdateForCollectionAsync(collectionId.Value, payload, cancellationToken));
    }

    [McpServerTool(Idempotent = true, Title = "Delete Tag", Destructive = true),
     Description("Removes a tag from all bookmarks.")]
    public Task<SuccessResponse> DeleteTagAsync(
        McpServer server,
        [Description("The name of the tag to remove.")] string tag,
        [Description("Collection ID if scoped")] int? collectionId = null,
        CancellationToken cancellationToken = default)
        => DeleteTagsAsync(server, [tag], collectionId, cancellationToken);

    [McpServerTool(Idempotent = true, Title = "Delete Tags", Destructive = true),
     Description("Removes one or more tags from all bookmarks.")]
    public async Task<SuccessResponse> DeleteTagsAsync(
        McpServer server,
        [Description("A collection of tag names to be removed.")] IEnumerable<string> tags,
        [Description("Collection ID if scoped")] int? collectionId = null,
        CancellationToken cancellationToken = default)
    {
        var tagsList = tags.ToList();
        const string BaseMessage = "Are you sure you want to delete these tags? This action cannot be undone.";

        string message = TagFormatter.FormatConfirmationMessage(tagsList, BaseMessage);

        if (!await ConfirmActionAsync(server, message, cancellationToken))
        {
            return new SuccessResponse(false);
        }

        var payload = new TagDeleteRequest { Tags = tagsList };
        return await ExecuteAndInvalidateAsync(collectionId is null
            ? Api.DeleteAsync(payload, cancellationToken)
            : Api.DeleteForCollectionAsync(collectionId.Value, payload, cancellationToken));
    }
}
