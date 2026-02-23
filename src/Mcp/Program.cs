using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mcp;

[assembly: InternalsVisibleTo("Mcp.Benchmarks")]
[assembly: InternalsVisibleTo("RaindropMcp.Tests")]

var builder = Host.CreateApplicationBuilder(args);
// var builder = WebApplication.CreateBuilder(args);

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
    // .WithHttpTransport()
    .WithPromptsFromAssembly()
    .WithToolsFromAssembly();

var app = builder.Build();

try
{
    // Accessing Value triggers validation because of ValidateDataAnnotations()
    _ = app.Services.GetRequiredService<IOptions<RaindropOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine("Error: Raindrop configuration is invalid.");
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine($" - {failure}");
    }
    Console.Error.WriteLine();
    Console.Error.WriteLine("Please ensure the 'RAINDROP_API_TOKEN' environment variable is set.");
    Console.Error.WriteLine("See README.md for setup instructions.");
    Environment.Exit(1);
}

// app.MapMcp();

await app.RunAsync();
