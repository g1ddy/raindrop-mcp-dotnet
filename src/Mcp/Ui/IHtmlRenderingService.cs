using System.Collections.Generic;
using System.Threading.Tasks;
using Mcp.Raindrops;

namespace Mcp.Ui;

/// <summary>
/// Service for generating static HTML from Razor components.
/// </summary>
public interface IHtmlRenderingService
{
    /// <summary>
    /// Renders a list of bookmarks into a static HTML string using the Explorer Razor component.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to render.</param>
    /// <returns>The generated HTML string.</returns>
    Task<string> RenderBookmarksAsync(IEnumerable<Raindrop> bookmarks);
}
