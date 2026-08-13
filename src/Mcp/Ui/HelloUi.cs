using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.Ui;

/// <summary>
/// Provides a static Hello World UI and a tool to trigger it for the client.
/// </summary>
[McpServerResourceType]
[McpServerToolType]
public class HelloUi
{
    private const string HelloUri = "ui://raindrop/hello";

    [McpServerResource(UriTemplate = HelloUri, Name = "Hello World UI", MimeType = McpApps.HtmlMimeType)]
    [Description("Returns a basic Hello World HTML string")]
    public static TextResourceContents GetHelloUi()
    {
        var html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <style>body { font-family: sans-serif; padding: 20px; text-align: center; }</style>
        </head>
        <body>
            <h2>Raindrop MCP App Dashboard</h2>
            <p>Status: Successfully connected to Claude Code!</p>
        </body>
        </html>";

        return new TextResourceContents
        {
            Uri = HelloUri,
            MimeType = McpApps.HtmlMimeType,
            Text = html
        };
    }

    [McpServerTool(Name = "show_raindrop_hello")]
    [McpAppUi(ResourceUri = HelloUri)]
    [Description("Displays the initial test UI view inside the chat window.")]
    public static CallToolResult ShowRaindropHello()
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Opening the Raindrop test UI dashboard..." }]
        };
    }
}
