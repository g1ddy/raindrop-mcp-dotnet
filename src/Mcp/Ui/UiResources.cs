using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.Ui;

[McpServerResourceType]
public class UiResources
{
    private readonly string _explorerHtml;

    public UiResources()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RaindropMcp.Ui.Artifacts.Explorer.html");
        if (stream == null)
        {
            _explorerHtml = "<html><body><h2>Error</h2><p>Explorer UI artifact not found.</p></body></html>";
        }
        else
        {
            using var reader = new StreamReader(stream);
            _explorerHtml = reader.ReadToEnd();
        }
    }

    [McpServerResource(UriTemplate = UiTools.ExplorerUri, Name = "Bookmark Explorer UI", MimeType = McpApps.HtmlMimeType)]
    [McpAppUi]
    [Description("Returns the static HTML Explorer application.")]
    public TextResourceContents GetExplorerUi()
    {
        return new TextResourceContents
        {
            Uri = UiTools.ExplorerUri,
            MimeType = McpApps.HtmlMimeType,
            Text = _explorerHtml
        };
    }
}
