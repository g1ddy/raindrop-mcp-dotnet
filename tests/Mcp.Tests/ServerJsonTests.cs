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
        // Navigate to servers.RaindropMcp
        var servers = jsonDoc.RootElement.GetProperty("servers");
        var raindropServer = servers.GetProperty("RaindropMcp");
        var command = raindropServer.GetProperty("command").GetString();
        var args = raindropServer.GetProperty("args");

        // Assert
        Assert.Equal("dnx", command);

        // Verify args contains the package name
        var argsArray = args.EnumerateArray().Select(a => a.GetString()).ToArray();
        Assert.Contains("Raindrop.Mcp.DotNet", argsArray);
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
