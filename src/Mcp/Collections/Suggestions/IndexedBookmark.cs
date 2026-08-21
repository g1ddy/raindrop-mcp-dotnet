namespace Mcp.Collections.Suggestions;

internal sealed record IndexedBookmark(
    long Id,
    int CollectionId,
    IReadOnlyDictionary<string, int> Terms,
    IReadOnlySet<string> Tags,
    string? Domain,
    string CanonicalString = "");
