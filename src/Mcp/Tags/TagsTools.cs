using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using ModelContextProtocol.Server;
using Mcp.Common;

namespace Mcp.Tags;

[McpServerToolType]
public class TagsTools(ITagsApi api) : RaindropToolBase<ITagsApi>(api)
{
    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
        Title = "List Tags"),
     Description("Retrieves all tags, optionally filtered by a collection.")]
    public Task<ItemsResponse<TagInfo>> ListTagsAsync([Description("Optional collection ID to filter tags by.")] int? collectionId = null)
        => collectionId is null
            ? Api.ListAsync()
            : Api.ListForCollectionAsync(collectionId.Value);

    [McpServerTool(Idempotent = true, Title = "Rename Tag"),
     Description("Renames a tag across all bookmarks.")]
    public Task<SuccessResponse> RenameTagAsync(
        [Description("The current name of the tag to rename.")] string oldTag,
        [Description("The new name for the tag.")] string newTag,
        [Description("Collection ID if scoped")] int? collectionId = null)
        => RenameTagsAsync([oldTag], newTag, collectionId);

    [McpServerTool(Idempotent = true, Title = "Rename Tags"),
     Description("Merges multiple tags into a single destination tag.")]
    public Task<SuccessResponse> RenameTagsAsync(
        [Description("A collection of tag names to be merged.")] IEnumerable<string> tags,
        [Description("The name of the tag that the source tags will be merged into.")] string newTag,
        [Description("Collection ID if scoped")] int? collectionId = null)
    {
        var payload = new TagRenameRequest { Replace = newTag, Tags = tags.ToList() };
        return collectionId is null
            ? Api.UpdateAsync(payload)
            : Api.UpdateForCollectionAsync(collectionId.Value, payload);
    }

    [McpServerTool(Idempotent = true, Title = "Delete Tag"),
     Description("Removes a tag from all bookmarks.")]
    public Task<SuccessResponse> DeleteTagAsync(
        [Description("The name of the tag to remove.")] string tag,
        [Description("Collection ID if scoped")] int? collectionId = null)
        => DeleteTagsAsync([tag], collectionId);

    [McpServerTool(Idempotent = true, Title = "Delete Tags"),
     Description("Removes one or more tags from all bookmarks.")]
    public Task<SuccessResponse> DeleteTagsAsync(
        [Description("A collection of tag names to be removed.")] IEnumerable<string> tags,
        [Description("Collection ID if scoped")] int? collectionId = null)
    {
        var payload = new TagDeleteRequest { Tags = tags.ToList() };
        return collectionId is null
            ? Api.DeleteAsync(payload)
            : Api.DeleteForCollectionAsync(collectionId.Value, payload);
    }
}
