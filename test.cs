using System;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Extensions.Apps;

class Program
{
    static void Main()
    {
        var resource = new TextResourceContents
        {
            Uri = "ui://test",
            MimeType = McpApps.HtmlMimeType,
            Text = "html"
        };

        Console.WriteLine(resource.GetType().Name);
    }
}
