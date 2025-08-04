using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.Prompts;

/// <summary>
/// Consolidated prompts for common Raindrop.io workflows.
/// </summary>
[McpServerPromptType]
public static class RaindropPrompts
{
    [McpServerPrompt, Description("Plan categories for unsorted bookmarks and preview a reorganization table.")]
    public static string OrganizeUnsorted() =>
        """
        Analyze all unsorted bookmarks and suggest categories. Summarize how many belong in each category using RenderTable from the console chat plugin. Propose new collections and wait for confirmation before moving anything.
        """;

    [McpServerPrompt, Description("Display a visual tree of collections with bookmark counts.")]
    public static string ShowCollectionTree() =>
        """
        List all collections and their bookmark counts. Present the hierarchy using RenderTree and show a RenderChart of the largest collections.
        """;

    [McpServerPrompt, Description("Balance bookmarks across collections with split and merge suggestions.")]
    public static string BalanceCollections() =>
        """
        Examine collection sizes and identify any that are overly large or very small. Suggest splitting large collections or merging smaller ones. Use RenderChart to visualize the distribution before and after your proposal.
        """;

    [McpServerPrompt, Description("Find duplicate bookmarks across collections and show them in a table.")]
    public static string CleanupDuplicates() =>
        """
        Search all bookmarks for duplicate URLs. Group duplicates in a table showing their collections using RenderTable. Recommend which copies to keep or remove.
        """;

    [McpServerPrompt, Description("Standardize tags by merging similar ones and suggesting new tags.")]
    public static string StandardizeTags() =>
        """
        Audit existing tags for inconsistent capitalization, pluralization or synonyms. Propose a concise set of tags and display tag frequencies with RenderChart. After approval, outline how to merge or apply tags consistently.
        """;

    [McpServerPrompt, Description("Streamline collections by combining related topics.")]
    public static string StreamlineCollections() =>
        """
        Identify collections with overlapping themes and propose merges or restructuring. Illustrate the proposed hierarchy with RenderTree and summarize changes using RenderTable.
        """;

    [McpServerPrompt, Description("Archive outdated bookmarks in a structured way.")]
    public static string ArchiveOldBookmarks() =>
        """
        Locate bookmarks older than two years and check their links. Suggest archiving any stale content into a well organized Archive collection. Use RenderTable to show counts by category.
        """;

    [McpServerPrompt, Description("Improve descriptions and nesting for all collections.")]
    public static string EnhanceHierarchy() =>
        """
        Review all collection descriptions and nesting depth. Recommend clearer descriptions and ensure no branch is nested more than three levels deep. Present the optimized hierarchy with RenderTree.
        """;
}
