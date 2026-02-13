using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

using Mcp;

[assembly: InternalsVisibleTo("Mcp.Benchmarks")]
[assembly: InternalsVisibleTo("RaindropMcp.Tests")]

var builder = Host.CreateApplicationBuilder(args);
// var builder = WebApplication.CreateBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Manual pre-flight validation to prevent noisy Host startup logs when configuration is missing.
var options = builder.Configuration.GetSection("Raindrop").Get<RaindropOptions>() ?? new RaindropOptions();
var validationContext = new ValidationContext(options);
var validationResults = new List<ValidationResult>();

if (!Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true))
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("❌ Configuration Error: Raindrop API Token is missing or invalid.");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Please ensure the 'Raindrop:ApiToken' configuration value is set.");
    Console.Error.WriteLine("You can set this via an environment variable:");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  export Raindrop__ApiToken=\"your-api-token\"");
    Console.Error.WriteLine();

    foreach (var result in validationResults)
    {
        Console.Error.WriteLine($"  - {result.ErrorMessage}");
    }

    Console.Error.WriteLine();
    Console.Error.WriteLine("See the README for more details.");

    Environment.Exit(1);
}

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
