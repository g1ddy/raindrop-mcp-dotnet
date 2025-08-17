# Project Structure

## Solution Organization
```
RaindropMcp.sln                    # Visual Studio solution file
├── src/                           # Source code
│   └── Mcp/                       # Main MCP server project
└── tests/                         # Test projects
    └── Mcp.Tests/                 # Unit tests for MCP server
```

## Source Code Layout (`src/Mcp/`)
```
src/Mcp/
├── Program.cs                     # Application entry point, DI container setup
├── RaindropServiceCollectionExtensions.cs  # Service registration
├── RaindropOptions.cs             # Configuration options
├── appsettings.json               # Application configuration
├── {Resource}/                    # Resource-based organization
│   ├── I{Resource}Api.cs          # Refit API interface
│   ├── {Resource}Tools.cs         # MCP tools with [McpServerTool] attributes
│   └── Models/                    # Data transfer objects
├── Common/                        # Shared models and base classes
├── Prompts/                       # Pre-defined workflow prompts
└── .mcp/                          # MCP server metadata
```

## Resource Directories
Each Raindrop.io API resource has its own directory:
- **Collections/**: Bookmark collections management
- **Raindrops/**: Individual bookmark operations
- **Tags/**: Tag management and organization
- **Highlights/**: Bookmark highlights functionality
- **User/**: User account information
- **Filters/**: Search and filtering tools

## Documentation Structure (Diátaxis Framework)
```
docs/
├── TUTORIAL.md                    # Getting started guide
├── HOW_TO.md                      # Developer recipes
├── REFERENCE.md                   # API documentation (auto-generated)
└── EXPLANATION.md                 # Architecture and design decisions
```

## Key Files
- **Program.cs**: Entry point, configures DI container and starts MCP host
- **{Resource}Tools.cs**: Contains MCP tool methods decorated with `[McpServerTool]`
- **I{Resource}Api.cs**: Refit interface definitions for Raindrop.io API
- **RaindropServiceCollectionExtensions.cs**: Registers all application services

## Naming Conventions
- **API Interfaces**: `I{Resource}Api` (e.g., `ICollectionsApi`)
- **Tool Classes**: `{Resource}Tools` (e.g., `CollectionsTools`)
- **Models**: Descriptive names in `{Resource}/Models/` directories
- **MCP Tools**: Method names should be descriptive and action-oriented
