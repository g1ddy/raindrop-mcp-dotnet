using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NJsonSchema;
using Xunit;

namespace Mcp.Tests;

public class ServerJsonTests
{
    [Fact]
    public async Task ServerJson_ShouldBeValidAccordingToSchema()
    {
        // Arrange
        var repoRoot = FindRepoRoot();
        var serverJsonPath = Path.Combine(repoRoot, "src", "Mcp", ".mcp", "server.json");

        Assert.True(File.Exists(serverJsonPath), $"server.json not found at {serverJsonPath}");

        var jsonContent = await File.ReadAllTextAsync(serverJsonPath);

        // Act
        // Get the schema URL from the file itself
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        if (!jsonDoc.RootElement.TryGetProperty("$schema", out var schemaProperty))
        {
            Assert.Fail("server.json does not contain a $schema property.");
        }
        var schemaUrl = schemaProperty.GetString();
        Assert.NotNull(schemaUrl);

        // Download and parse the schema
        var schema = await JsonSchema.FromUrlAsync(schemaUrl);

        // Validate
        var errors = schema.Validate(jsonContent);

        // Assert
        if (errors.Count > 0)
        {
             var messages = string.Join("\n", errors.Select(e => $"{e.Path}: {e.Kind} - {e.Property}"));
             Assert.Fail($"Schema validation failed:\n{messages}");
        }
    }

    private string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("RaindropMcp.sln").Any())
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find RaindropMcp.sln in parent directories.");
    }
}
