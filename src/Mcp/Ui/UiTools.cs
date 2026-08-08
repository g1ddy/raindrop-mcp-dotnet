using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Common;
using Mcp.Raindrops;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Ui;

[McpServerToolType]
public class UiTools
{
    private readonly IRaindropsApi _raindropsApi;
    private readonly IHtmlRenderingService _htmlRenderingService;
    private readonly IUiCacheService _uiCacheService;
    private static readonly HashSet<string> ValidSortOptions = new(
        new[] { "created", "-created", "title", "-title", "domain", "-domain", "sort", "score" }
    );

    public UiTools(IRaindropsApi raindropsApi, IHtmlRenderingService htmlRenderingService, IUiCacheService uiCacheService)
    {
        _raindropsApi = raindropsApi;
        _htmlRenderingService = htmlRenderingService;
        _uiCacheService = uiCacheService;
    }

    [McpServerTool(Name = "visualize_bookmarks", Title = "Visualize Bookmarks"), Description("Searches for bookmarks and displays them in a visual, read-only UI grid for the human user. Use this instead of list_bookmarks when the user wants to visually explore their bookmarks.")]
    public async Task<CallToolResult> VisualizeBookmarksAsync(
        [Description("The ID of the collection to retrieve bookmarks from. Use 0 for all, -1 for unsorted, -99 for trash.")] int collectionId,
        [Description(SearchSyntax.Description)] string? search = null,
        [Description("Sorting order: '-created' (newest, default), 'created', 'score' (relevance when searching), 'sort', 'title', '-title', 'domain', '-domain'.")] string? sort = null,
        [Description("Page index starting from 0.")] int? page = null,
        [Description("How many raindrops per page, up to 50.")] int? perPage = null,
        [Description("Include bookmarks from nested collections (true/false).")] bool? nested = null,
        CancellationToken cancellationToken = default)
    {
        if (page is < 0)
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page number cannot be negative.");

        if (perPage is > 50 or < 1)
            throw new ArgumentOutOfRangeException(nameof(perPage), perPage, "Number of items per page must be between 1 and 50.");

        if (sort is not null)
        {
            if (!ValidSortOptions.Contains(sort))
                throw new ArgumentOutOfRangeException(nameof(sort), sort, $"Valid values are '{string.Join("', '", ValidSortOptions)}'.");

            if (sort == "score" && string.IsNullOrWhiteSpace(search))
                throw new ArgumentException("Sort 'score' is only allowed when using a search query.", nameof(sort));
        }

        var response = await _raindropsApi.ListAsync(collectionId, search, sort, page, perPage, nested, cancellationToken);

        var bookmarks = response.Items ?? Array.Empty<Raindrop>();

        var html = await _htmlRenderingService.RenderBookmarksAsync(bookmarks);
        var guid = _uiCacheService.StoreHtml(html);

        var uri = $"ui://raindrop/explorer/{guid}";

        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Opening the Visual Bookmark Explorer...\nFound {bookmarks.Count} bookmarks." }
            },
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject { ["ResourceUri"] = uri }
            }
        };
    }

    [McpServerTool(Name = "fetch_bookmark_details", Title = "Fetch Bookmark Details"), Description("Fetches extended metadata for a specific bookmark. Used by the UI.")]
    public async Task<CallToolResult> FetchBookmarkDetailsAsync(
        [Description("ID of the bookmark to retrieve")] int bookmarkId,
        CancellationToken cancellationToken = default)
    {
        var rawData = await _raindropsApi.GetAsync(bookmarkId, cancellationToken);
        var jsonText = System.Text.Json.JsonSerializer.Serialize(rawData);
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = jsonText }
            }
        };
    }
}
