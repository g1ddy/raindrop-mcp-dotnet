# Maintainer TODO List

This file tracks the immediate, concrete tasks required to finalize the project for its initial release and distribution.

---

### **1. Finalize and Publish the NuGet Package**

The goal is to make this tool easily installable for other .NET developers via NuGet.

-   [ ] **Update `.csproj` Metadata:** Before the first publish, review and confirm the package metadata in `src/Mcp/Mcp.csproj`. Ensure the `<PackageId>`, `<Version>`, `<Authors>`, `<Description>`, and `<RepositoryUrl>` are correct.

    ```xml
    <PackageId>Raindrop.Mcp</PackageId>
    <Version>0.1.0-alpha</Version>
    <Authors>Your Name or Org</Authors>
    ```

-   [ ] **Pack the Project:** Run the `pack` command to create the `.nupkg` file.

    ```sh
    dotnet pack ./src/Mcp -c Release
    ```

-   [ ] **Publish to NuGet.org:** Create an API key on NuGet.org and push the package. This should only be done when ready for the first public pre-release.

    ```sh
    dotnet nuget push ./src/Mcp/bin/Release/Raindrop.Mcp.0.1.0-alpha.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
    ```

---

### **2. Update Documentation for NuGet and `ndx` Usage**

The goal is to provide clear instructions for end-users on how to consume the published NuGet package using the standard `ndx` tool.

-   [ ] **Update `TUTORIAL.md`:** Refactor the tutorial to make using `ndx` the primary, recommended setup method. The current tutorial, which relies on `dotnet run --project`, should become a secondary guide for developers.

-   [ ] **Incorporate `ndx` Configuration:** The updated tutorial should instruct users to create a `.vscode/mcp.json` (for VS Code) or `.mcp.json` (for Visual Studio) file with the following configuration. This command uses `ndx` to download and run the specific package version defined in our `mcp-manifest.json`.

    ```json
    {
      "servers": {
        "RaindropMcp": {
          "type": "stdio",
          "command": "ndx",
          "args": [
            "run",
            "RaindropMcp",
            "--manifest-path",
            "./mcp-manifest.json"
          ]
        }
      }
    }
    ```

-   [ ] **Add Reference Links:** Add a section to the `README.md` or `TUTORIAL.md` with helpful links for users to learn more about the MCP ecosystem.

    -   [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
    -   [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
    -   [Official MCP Documentation](https://modelcontextprotocol.io/)

### **3. Implement Secure Token Configuration for `ndx`**

The goal is to provide a secure and standard way for `ndx` users to provide their API token without hardcoding it in `appsettings.json`.

-   [ ] **Create `mcp-manifest.json`:** Create this file in the root of the repository. This will be the standard manifest for users who consume the package via `ndx`.

    ```json
    {
      "servers": {
        "RaindropMcp": {
          "type": "package",
          "package": {
            "id": "Raindrop.Mcp",
            "version": "0.1.0-alpha"
          },
          "environment": {
            "RAINDROP__APITOKEN": "#{RaindropApiToken}"
          }
        }
      },
      "secrets": {
        "RaindropApiToken": {
          "description": "Your personal API token from the Raindrop.io developer settings."
        }
      }
    }
    ```

-   [ ] **Update Documentation:** After this file is created, update the `TUTORIAL.md` to make using `ndx` with the manifest the primary, recommended setup method for end-users.
