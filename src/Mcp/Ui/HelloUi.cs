using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace Mcp.Ui;

/// <summary>
/// Provides a static Hello World UI and a tool to trigger it for the client.
/// </summary>
[McpServerResourceType]
[McpServerToolType]
public class HelloUi
{
    private const string HelloUri = "ui://raindrop/hello";

    [McpServerResource(UriTemplate = HelloUri, Name = "Hello World UI", MimeType = "text/html")]
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
            MimeType = "text/html",
            Text = html
        };
    }

    [McpServerTool(Name = "show_raindrop_hello")]
    [Description("Displays the initial test UI view inside the chat window.")]
    public static CallToolResult ShowRaindropHello()
    {
        return new CallToolResult
        {
            Content = new List<ContentBlock> { new TextContentBlock { Text = "Opening the Raindrop test UI dashboard..." } },
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject { ["ResourceUri"] = HelloUri }
            }
        };
    }
}
