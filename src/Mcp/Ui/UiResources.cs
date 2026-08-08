using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Ui;

[McpServerResourceType]
public class UiResources
{
    private readonly IUiCacheService _uiCacheService;

    public UiResources(IUiCacheService uiCacheService)
    {
        _uiCacheService = uiCacheService;
    }

    [McpServerResource(UriTemplate = "ui://raindrop/explorer/{id}", Name = "Bookmark Explorer UI", MimeType = "text/html")]
    [Description("Returns the dynamically generated HTML grid of bookmarks for a specific visualization request.")]
    public TextResourceContents GetExplorerUi(string id)
    {
        var html = _uiCacheService.GetHtml(id);

        if (html == null)
        {
            return new TextResourceContents
            {
                Uri = $"ui://raindrop/explorer/{id}",
                MimeType = "text/html",
                Text = "<html><body><h2>Error</h2><p>UI visualization not found or expired.</p></body></html>"
            };
        }

        return new TextResourceContents
        {
            Uri = $"ui://raindrop/explorer/{id}",
            MimeType = "text/html",
            Text = html
        };
    }
}
