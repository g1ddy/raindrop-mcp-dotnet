namespace Mcp.Ui;

/// <summary>
/// Cache service for storing generated UI HTML strings.
/// </summary>
public interface IUiCacheService
{
    /// <summary>
    /// Stores the HTML and returns a unique Guid key.
    /// </summary>
    string StoreHtml(string html);

    /// <summary>
    /// Retrieves the HTML by key. Returns null if not found.
    /// </summary>
    string? GetHtml(string key);
}
