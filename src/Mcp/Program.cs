using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Mcp;

var builder = Host.CreateApplicationBuilder(args);
// var builder = WebApplication.CreateBuilder(args);

// START UX IMPROVEMENT
// Manual pre-flight check to validate configuration.
// NOTE: While redundant with ValidateOnStart(), this check is necessary to suppress
// the default OptionsValidationException stack trace logged by the Host,
// ensuring a clean, user-friendly error message for the CLI.
var apiToken = builder.Configuration["Raindrop:ApiToken"];
if (string.IsNullOrWhiteSpace(apiToken))
{
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(@"
Error: Raindrop API Token is missing.

To use this MCP server, you must provide a Raindrop.io Test Token.
You can set it via:
1. Environment Variable: RAINDROP__APITOKEN=your_token_here
2. appsettings.json: { ""Raindrop"": { ""ApiToken"": ""your_token_here"" } }

Get your token from: https://app.raindrop.io/settings/integrations
");
    Console.ForegroundColor = originalColor;
    Environment.Exit(1);
}
// END UX IMPROVEMENT

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

// app.MapMcp();

await app.RunAsync();
