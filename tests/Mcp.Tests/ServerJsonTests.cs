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
    public void ServerJson_ShouldExistAndHaveCorrectCommand()
    {
        // Arrange
        var repoRoot = FindRepoRoot();
        var serverJsonPath = Path.Combine(repoRoot, "src", "Mcp", "server.json");

        Assert.True(File.Exists(serverJsonPath), $"server.json not found at {serverJsonPath}");

        var jsonContent = File.ReadAllText(serverJsonPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);

        // Act
        // Verify root properties
        var schema = jsonDoc.RootElement.GetProperty("$schema").GetString();
        var name = jsonDoc.RootElement.GetProperty("name").GetString();
        var version = jsonDoc.RootElement.GetProperty("version").GetString();

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("io.github.g1ddy/raindrop-mcp-dotnet", name);
        Assert.Equal("0.0.0-dev", version);

        // Verify packages array
        var packages = jsonDoc.RootElement.GetProperty("packages");
        Assert.True(packages.GetArrayLength() > 0);

        var firstPackage = packages[0];
        Assert.Equal("nuget", firstPackage.GetProperty("registryType").GetString());
        Assert.Equal("Raindrop.Mcp.DotNet", firstPackage.GetProperty("identifier").GetString());
        Assert.Equal("0.0.0-dev", firstPackage.GetProperty("version").GetString());

        var transportType = firstPackage.GetProperty("transport").GetProperty("type").GetString();
        Assert.Equal("stdio", transportType);
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
