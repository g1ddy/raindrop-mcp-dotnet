# Technology Stack

## Framework & Runtime
- **.NET 8.0**: Target framework with nullable reference types enabled
- **C#**: Primary programming language with implicit usings
- **Console Application**: Packaged as executable MCP server tool

## Key Dependencies
- **ModelContextProtocol**: Core MCP SDK for server implementation
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting framework
- **Microsoft.Extensions.Http**: HTTP client factory integration
- **Refit.HttpClientFactory**: Type-safe REST API client generation

## Architecture Patterns
- **Dependency Injection**: Services registered via extension methods
- **Resource-Based Organization**: Code structured by API resource (Collections, Tags, Raindrops, etc.)
- **Refit Interfaces**: Type-safe API client definitions (I{Resource}Api.cs)
- **MCP Tools**: Methods decorated with `[McpServerTool]` attribute
- **Configuration**: User secrets for API tokens, appsettings.json for other config

## Common Commands

### Build & Test
```bash
# Restore dependencies
dotnet restore RaindropMcp.sln

# Build project
dotnet build RaindropMcp.sln --no-restore

# Run all tests
dotnet test RaindropMcp.sln --no-build --no-restore
```

### Development
```bash
# Run from source (development)
dotnet run --project src/Mcp

# Set API token securely (run from solution root)
dotnet user-secrets set "Raindrop:ApiToken" "YOUR_TOKEN" --project src/Mcp
# Generate documentation
powershell -File ./scripts/generate-docs.ps1
```

### Package Management
- **PackAsTool**: true - Creates global .NET tool
- **PackageType**: McpServer - Identifies as MCP server package
- **Version**: Semantic versioning with beta releases
