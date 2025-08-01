using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mcp;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddRaindropApiClient(builder.Configuration)
    .AddMcpServer(options =>
    {
        options.ServerInstructions = """
            This Raindrop MCP server exposes bookmark-management tools for Raindrop.io.
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
    .WithPromptsFromAssembly()
    .WithToolsFromAssembly();

var app = builder.Build();

// app.MapMcp();

await app.RunAsync();
