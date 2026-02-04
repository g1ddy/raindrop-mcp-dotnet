# Maintainer TODO List

This file tracks the immediate, concrete tasks required to finalize the project for its initial release and distribution.

---

### **1. Finalize and Publish the NuGet Package**

The goal is to make this tool easily installable for other .NET developers via NuGet.

-   [x] **Update `.csproj` Metadata:** Before the first publish, review and confirm the package metadata in `src/Mcp/RaindropMcp.csproj`. Ensure the `<PackageId>`, `<Authors>`, `<Description>`, and `<RepositoryUrl>` are correct. (`<Version>` is handled by MinVer during the build/pack process).

    ```xml
    <PackageId>Raindrop.Mcp.DotNet</PackageId>
    <Authors>g1ddy</Authors>
    ```

-   [x] **Automate Versioning:** Replaced manual `<PackageVersion>` in `.csproj` with `MinVer` to automatically derive version from Git tags.

-   [x] **Pack the Project:** Run the `pack` command to create the `.nupkg` file.

    ```sh
    dotnet pack ./src/Mcp -c Release
    ```

-   [x] **Publish to NuGet.org:** Ensure the `NUGET_API_KEY` secret is set in GitHub Actions, then create a **GitHub Release** to trigger the automated [publish workflow](.github/workflows/publish.yml). See [Releasing Guide](docs/RELEASING.md).

    *Note: The command below is for reference only. We use the automated workflow to publish.*
    ```sh
    dotnet nuget push ./src/Mcp/bin/Release/Raindrop.Mcp.DotNet.0.1.3-beta.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
    ```

---

### **2. Update Documentation for NuGet and `dnx` Usage**

The goal is to provide clear instructions for end-users on how to consume the published NuGet package using the standard `dnx` tool.

-   [x] **Update `TUTORIAL.md`:** Refactor the tutorial to make using `dnx` the primary, recommended setup method. The current tutorial, which relies on `dotnet run --project`, should become a secondary guide for developers. (The tutorial now points to the package README which covers this).

-   [x] **Incorporate `dnx` Configuration:** The updated tutorial/README should instruct users to create a `.vscode/mcp.json` (for VS Code) or `.mcp.json` (for Visual Studio) file with the following configuration.

    ```json
    {
      "servers": {
        "RaindropMcp": {
          "type": "stdio",
          "command": "dnx",
          "args": [
            "Raindrop.Mcp.DotNet",
            "--prerelease",
            "--yes"
          ],
          "env": {
            "Raindrop:ApiToken": "${env:RAINDROP_API_TOKEN}",
            "Raindrop:BaseUrl": "https://api.raindrop.io/rest/v1"
          }
        }
      }
    }
    ```

-   [x] **Add Reference Links:** Add a section to the `README.md` or `TUTORIAL.md` with helpful links for users to learn more about the MCP ecosystem.

    -   [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
    -   [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
    -   [Official MCP Documentation](https://modelcontextprotocol.io/)
