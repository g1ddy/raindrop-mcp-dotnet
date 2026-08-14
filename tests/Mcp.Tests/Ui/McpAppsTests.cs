using System.Reflection;
using Mcp.Ui;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;

namespace RaindropMcp.Tests.Ui;

public class McpAppsTests
{
    [Fact]
    public void AppToolsDeclareTheirUiResourcesAndVisibility()
    {
        var hello = typeof(HelloUi).GetMethod(nameof(HelloUi.ShowRaindropHello))!
            .GetCustomAttribute<McpAppUiAttribute>();
        var visualize = typeof(UiTools).GetMethod(nameof(UiTools.VisualizeBookmarksAsync))!
            .GetCustomAttribute<McpAppUiAttribute>();
        var details = typeof(UiTools).GetMethod(nameof(UiTools.FetchBookmarkDetailsAsync))!
            .GetCustomAttribute<McpAppUiAttribute>();

        Assert.Equal("ui://raindrop/hello", hello?.ResourceUri);
        Assert.Equal(UiTools.ExplorerUri, visualize?.ResourceUri);
        Assert.Equal(UiTools.ExplorerUri, details?.ResourceUri);
        Assert.NotNull(details);
        Assert.NotNull(details.Visibility);
        Assert.Equal([McpUiToolVisibility.App], details.Visibility);
    }

    [Fact]
    public void UiResourcesUseTheMcpAppHtmlProfile()
    {
        var hello = HelloUi.GetHelloUi();
        var explorer = new UiResources().GetExplorerUi();

        Assert.Equal(McpApps.HtmlMimeType, hello.MimeType);
        Assert.Equal(McpApps.HtmlMimeType, explorer.MimeType);
        Assert.Equal(UiTools.ExplorerUri, explorer.Uri);
    }
}
