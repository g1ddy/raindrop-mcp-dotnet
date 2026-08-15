using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Mcp.Common;
using Mcp.Raindrops;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Extensions.Apps;

namespace Mcp.Ui;

[McpServerToolType]
public class UiTools
{
    public const string ExplorerUri = "ui://raindrop/explorer";
    private readonly IRaindropsApi _raindropsApi;
    private static readonly HashSet<string> ValidSortOptions = new(
        new[] { "created", "-created", "title", "-title", "domain", "-domain", "sort", "score" }
    );

    public UiTools(IRaindropsApi raindropsApi)
    {
        _raindropsApi = raindropsApi;
    }

    [McpServerTool(Name = "visualize_bookmarks", Title = "Visualize Bookmarks"), Description("Searches for bookmarks and displays them in a visual, read-only UI grid for the human user. Use this instead of list_bookmarks when the user wants to visually explore their bookmarks.")]
    [McpAppUi(ResourceUri = ExplorerUri)]
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

        var items = new List<ExplorerBookmarkSummary>(bookmarks.Count);
        foreach (var bookmark in bookmarks)
        {
            items.Add(new ExplorerBookmarkSummary
            {
                Id = bookmark.Id,
                Title = bookmark.Title,
                Link = bookmark.Link,
                Domain = bookmark.Domain,
                Excerpt = bookmark.Excerpt,
                Tags = bookmark.Tags
            });
        }

        var result = new ExplorerResult
        {
            CollectionId = collectionId,
            Search = search,
            Sort = sort,
            Page = page,
            PerPage = perPage,
            Nested = nested,
            Count = items.Count,
            Items = items
        };

        var serializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"Opening the Visual Bookmark Explorer...\nFound {items.Count} bookmarks." }
            },
            StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(result, serializerOptions)
        };
    }

    [McpServerTool(Name = "fetch_bookmark_details", Title = "Fetch Bookmark Details", ReadOnly = true, Destructive = false, Idempotent = true), Description("Fetches extended metadata for a specific bookmark. Used by the UI.")]
    [McpAppUi(ResourceUri = ExplorerUri, Visibility = [McpUiToolVisibility.App])]
    public async Task<CallToolResult> FetchBookmarkDetailsAsync(
        [Description("ID of the bookmark to retrieve")] long bookmarkId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _raindropsApi.GetAsync(bookmarkId, cancellationToken);
            var rawData = response.Item;

            var details = new ExplorerBookmarkDetails
            {
                Id = rawData.Id,
                Title = rawData.Title,
                Link = rawData.Link,
                Domain = rawData.Domain,
                Excerpt = rawData.Excerpt,
                Note = rawData.Note,
                Tags = rawData.Tags,
                Created = rawData.Created
            };

            var serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Fetched details for bookmark {bookmarkId}." }
                },
                StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(details, serializerOptions)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            var serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            return new CallToolResult
            {
                IsError = true,
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Failed to fetch details for bookmark {bookmarkId}." }
                },
                StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(new ExplorerErrorResult { Error = "Failed to fetch bookmark details." }, serializerOptions)
            };
        }
    }
}
