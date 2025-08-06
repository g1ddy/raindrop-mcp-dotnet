namespace Mcp.Common;

/// <summary>
/// Centralized description of the Raindrop search syntax.
/// </summary>
public static class SearchSyntax
{
    /// <summary>
    /// Explanation of supported search operators for filtering bookmarks and filters.
    /// </summary>
    public const string Description = """
        A search string using Raindrop's advanced syntax. Useful operators:
        * "exact phrase" matches the phrase.
        * -word or -#tag excludes a word or tag.
        * #tag or #"multi word" filters by tag.
        * match:OR finds items with either term.
        * created:>YYYY-MM-DD or lastUpdate:YYYY-MM-DD filter by date.
        * title:word, excerpt:word, note:word, link:word search within fields.
        * type:article/video/image/document/audio limit by type.
        * ❤️ favorites, file:true, notag:true, cache.status:ready, reminder:true.
    """;
}
