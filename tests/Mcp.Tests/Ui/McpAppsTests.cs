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

    [Fact]
    public void ExplorerArtifactUsesSafeMcpAppLifecycleWithoutGlobalExposure()
    {
        var explorer = new UiResources().GetExplorerUi();

        Assert.Contains("ontoolresult", explorer.Text, StringComparison.Ordinal);
        Assert.Contains("ontoolcancelled", explorer.Text, StringComparison.Ordinal);
        Assert.Contains("Loading bookmark details", explorer.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("window.mcpApp", explorer.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("window.loadDetails", explorer.Text, StringComparison.Ordinal);
        Assert.True(
            explorer.Text.IndexOf("ontoolresult", StringComparison.Ordinal) <
            explorer.Text.LastIndexOf(".connect()", StringComparison.Ordinal),
            "One-shot handlers must be registered before the MCP App connects.");
    }

    [Fact]
    public void ExplorerResourceIsImmutableAcrossReads()
    {
        var resources = new UiResources();

        var first = resources.GetExplorerUi();
        var second = resources.GetExplorerUi();

        Assert.Equal(first.Uri, second.Uri);
        Assert.Equal(first.MimeType, second.MimeType);
        Assert.Equal(first.Text, second.Text);
    }
}
