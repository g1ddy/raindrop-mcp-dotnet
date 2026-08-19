using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Analytics;

[McpServerToolType]
public sealed class LibraryAnalyticsTools(ILibraryAnalyticsService analyticsService)
{
    public const string ToolName = "analyze_library";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(
        Name = ToolName,
        Title = "Analyze Bookmark Library",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true)]
    [Description("Analyzes the complete Raindrop library or a selected collection, including its subcollections, and returns collection, domain, tag, and organization metrics. Omit collectionId or use 0 for all non-trash bookmarks, -1 for Unsorted, -99 for Trash, or a positive collection ID.")]
    public async Task<CallToolResult> AnalyzeLibraryAsync(
        [Description("Collection scope. Defaults to 0 for all non-trash bookmarks; use -1 for Unsorted, -99 for Trash, or a positive collection ID.")] int collectionId = 0,
        CancellationToken cancellationToken = default)
    {
        var report = await analyticsService.AnalyzeAsync(collectionId, cancellationToken);
        var completeness = report.Scope.IsComplete ? "complete" : $"partial ({report.Scope.TerminationReason})";

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = $"Library analysis is {completeness}. Analyzed {report.Summary.BookmarksAnalyzed} bookmarks across {report.Collections.Count} collection entries, {report.Summary.UniqueDomains} domains, and {report.Summary.UniqueTags} tags."
                }
            ],
            StructuredContent = JsonSerializer.SerializeToElement(report, SerializerOptions)
        };
    }
}
