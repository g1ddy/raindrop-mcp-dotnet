using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mcp;

[assembly: InternalsVisibleTo("Mcp.Benchmarks")]
[assembly: InternalsVisibleTo("RaindropMcp.Tests")]

var builder = Host.CreateApplicationBuilder(args);
// var builder = WebApplication.CreateBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddHostedService<Mcp.Raindrops.RaindropMonitorService>()
    .AddRaindropApiClient(builder.Configuration)
    .AddMcpServer(options =>
    {
        options.Capabilities ??= new ModelContextProtocol.Protocol.ServerCapabilities();
        options.Capabilities.Experimental ??= new System.Collections.Generic.Dictionary<string, object>();
        options.Capabilities.Experimental["claude/channel"] = new object();
        options.ServerInstructions = """
            This Raindrop MCP server exposes bookmark-management tools for Raindrop.io.
            Note: This server fires background notifications to the `claude/channel` when new bookmarks are detected.
            Follow the workflow: Explore → Plan → Create → Move → Verify.
            Start with ListCollections and ListChildCollections to review your hierarchy.
            Create new collections using the parent field for subcollections.
            Merge collections with both the 'to' parameter and 'ids' array.
            Special IDs: 0 (all), -1 (unsorted), -99 (trash).
            Update bookmarks in bulk by explicit ID and verify counts before and after changes.
            Renderable functions like RenderTable, RenderTree and RenderChart can visualize results.
        """;
    })
    .WithStdioServerTransport()
    // .WithHttpTransport()
    .WithPromptsFromAssembly()
    .WithToolsFromAssembly();

var app = builder.Build();

// Fail-fast configuration check
try
{
    _ = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RaindropOptions>>().Value;
}
catch (Microsoft.Extensions.Options.OptionsValidationException ex)
{
    Console.Error.WriteLine($"\nError: Configuration validation failed. {ex.Message}");
    app.Dispose();
    Environment.Exit(1);
}

// app.MapMcp();

await app.RunAsync();
