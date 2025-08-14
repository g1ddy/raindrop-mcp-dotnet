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
            Analyze all unsorted bookmarks and suggest categories.
            Summarize how many belong in each category using unicode borders to render the table.
            Propose new collections and wait for confirmation before moving anything.
        """;

    [McpServerPrompt, Description("Display a visual tree of collections with bookmark counts.")]
    public static string ShowCollectionTree() =>
        """
            List all collections and their bookmark counts.
            Present the hierarchy using unicode box-drawing character to render the tree.
        """;

    [McpServerPrompt, Description("Balance bookmarks across collections with split and merge suggestions.")]
    public static string BalanceCollections() =>
        """
            Examine collection sizes and identify any that are overly large or very small.
            Suggest splitting large collections or merging smaller ones.
            Use ANSI escape sequences to control aspects like color to visualize the distribution in chart form before and after your proposal.
        """;

    [McpServerPrompt, Description("Find duplicate bookmarks across collections and show them in a table.")]
    public static string CleanupDuplicates() =>
        """
            Search all bookmarks for duplicate URLs.
            Group duplicates in a table showing their collections using unicode borders to render the table.
            Recommend which copies to keep or remove.
        """;

    [McpServerPrompt, Description("Standardize tags by merging similar ones and suggesting new tags.")]
    public static string StandardizeTags() =>
        """
            Audit existing tags for inconsistent capitalization, pluralization or synonyms.
            Propose a concise set of tags and display tag frequencies with ANSI escape sequences to control aspects like color to visualize the distribution in chart form.
            After approval, outline how to merge or apply tags consistently.
        """;

    [McpServerPrompt, Description("Streamline collections by combining related topics.")]
    public static string StreamlineCollections() =>
        """
            Identify collections with overlapping themes and propose merges or restructuring.
            Illustrate the proposed hierarchy using unicode box-drawing character to render the tree and summarize changes using unicode borders to render the table.
        """;

    [McpServerPrompt, Description("Archive outdated bookmarks in a structured way.")]
    public static string ArchiveOldBookmarks() =>
        """
            Locate bookmarks older than two years and check their links.
            Suggest archiving any stale content into a well organized Archive collection.
            Use unicode borders to render the table to show counts by category.
        """;

    [McpServerPrompt, Description("Improve descriptions and nesting for all collections.")]
    public static string EnhanceHierarchy() =>
        """
            Review all collection descriptions and nesting depth.
            Recommend clearer descriptions and ensure no branch is nested more than three levels deep.
            Present the optimized hierarchy using unicode box-drawing character to render the tree.
        """;

    [McpServerPrompt, Description("Remove a tag from all bookmarks.")]
    public static string RemoveTagFromAllBookmarks() =>
        """
            Find all bookmarks with the tag '#obsolete' and remove it.
            This is a destructive action, so you must ask for confirmation before proceeding.
        """;

    [McpServerPrompt, Description("Suggest a collection for a bookmark.")]
    public static string SuggestCollection() =>
        """
            Given a bookmark, find the best three collections to put it in.
            Ask the user to select a collection and then move the bookmark to the selected collection.
        """;
}
