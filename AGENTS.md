# Agent Instructions

This document provides instructions for AI agents working with this repository.

## Code Layout

This project is a .NET-based MCP server for Raindrop.io. The code is structured by resource type.

-   **`src/Mcp/Program.cs`**: The application entry point. It configures the DI container and starts the MCP host.
-   **`src/Mcp/RaindropServiceCollectionExtensions.cs`**: Registers all the services for the application.
-   **`src/Mcp/{Resource}`**: Each resource (e.g., `Collections`, `Tags`, `Raindrops`) has its own directory containing:
    -   **`I{Resource}Api.cs`**: The Refit interface for the Raindrop.io API.
    -   **`{Resource}Tools.cs`**: The MCP tools for that resource. Methods in these files are decorated with the `[McpServerTool]` attribute.
    -   **Models**: C# classes that represent the data structures for that resource.
-   **`src/Mcp/Common`**: Contains shared models and base classes.
-   **`src/Mcp/Prompts`**: Contains pre-defined prompts for common workflows.

## Development Environment and Validation

The development environment is pre-configured with the .NET 8 SDK. To restore dependencies, build the project, and run all tests, you can use the following commands:

```bash
dotnet restore RaindropMcp.sln
dotnet build RaindropMcp.sln --no-restore
dotnet test RaindropMcp.sln --no-build --no-restore
```
