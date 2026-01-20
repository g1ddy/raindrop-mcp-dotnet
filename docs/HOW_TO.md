# How-To Guides for Developers

This guide provides recipes for developers who want to set up a local development environment, modify, or contribute to the Raindrop MCP .NET server.

**Prerequisite:** You have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later installed.

-   [Back to Home](../README.md)

---

### **How to Set Up a Development Environment**

Running the server from source is ideal for debugging, testing new features, or making contributions. This setup ensures you are working with the exact code in your local repository.

**1. Clone the Repository:**

```sh
git clone https://github.com/g1ddy/raindrop-mcp-dotnet.git
cd raindrop-mcp-dotnet
```

**2. Get Your Raindrop.io API Token:**

*   Go to https://app.raindrop.io/settings/integrations
*   Create a new app or use an existing one
*   Copy the "Test token" for development use

**3. Configure Your API Token:**

*   **For VS Code Users (Recommended):** The MCP configuration in `.vscode/mcp.json` uses VS Code's secure input system. Simply use the MCP server in chat and VS Code will prompt for your API key on first use.

*   **Alternative - .NET User Secrets:** If your MCP client doesn't support secure credential storage, you can use .NET's built-in secret manager:
    ```sh
    # In the src/Mcp directory
    dotnet user-secrets init
    dotnet user-secrets set "Raindrop:ApiToken" "PASTE_YOUR_TOKEN_HERE"
    ```

*   **Add the Server Configuration to your MCP client:**

    ```json
    {
      "servers": {
        "RaindropMcp": {
          "type": "stdio",
          "command": "dotnet",
          "args": [
            "run",
            "--project",
            "C:/path/to/your/raindrop-mcp-dotnet/src/Mcp"
          ],
          "env": {
            "Raindrop:BaseUrl": "https://api.raindrop.io/rest/v1"
          }
        }
      }
    }
    ```
    *Replace `C:/path/to/your/raindrop-mcp-dotnet/src/Mcp` with the absolute path to the `src/Mcp` directory on your machine.*

**4. Debug the MCP Server:**

To debug the running MCP server:

1. **Start the MCP server** by using it in VS Code chat (e.g., ask "@raindrop list my collections")
2. **Set breakpoints** in your code where you want to debug
3. **Attach the debugger:**
   - Press `F5` or go to Run and Debug view
   - Select ".NET Core Attach" configuration
   - Choose the `dotnet.exe` process that's running `RaindropMcp.dll`
   - Look for the process with command line arguments containing `src/Mcp`

---

### **How to Add a Custom Tool**

Here's how to add a new tool that gets the current user's information.

**1. Define the API Method (if necessary):**

The `IUserApi.cs` interface already defines a `GetCurrentUserAsync` method, so we can skip this step. If you were adding a new function, you would first add it to the appropriate API interface.

**2. Create the Tool Method:**

Open `src/Mcp/User/UserTools.cs` and add the following method:

```csharp
[McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true,
    Title = "Get User Info"),
    Description("Retrieves the details of the currently authenticated user.")]
public Task<ItemResponse<UserInfo>> GetUserInfoAsync() => Api.GetAsync();
```

**3. Update Documentation:**

After adding your tool, update the technical reference:

```sh
dotnet build ./src/Mcp
powershell -File ./scripts/generate-docs.ps1
```

---

### **Working with the Raindrop API**

When extending the server, be aware that the C# models are distinct from the raw API JSON.

*   **Collection Mapping:** The API expects nested objects for collections (e.g., `collection: { "$id": 123 }`). Use `Mcp.Common.IdRef` to map these correctly. Do not use flat integer fields like `collectionId` for Create/Update payloads.
*   **Partial Models:** The `Raindrop` C# model does not include every field returned by the API (e.g., `media`, `cover`, `highlights` are currently omitted). If you need these fields, update the `Raindrop` record and verify the JSON property names against the [official documentation](https://developer.raindrop.io/v1).
*   **VCR Testing:** When writing integration tests, always wrap cleanup operations (e.g., `DeleteBookmarkAsync`) in `try/catch` blocks within the `finally` clause. This ensures that a failure in one step doesn't leave other resources behind.

---

### **Next Steps: Contributing**

Now that you know how to extend the server, please review our **[Contributing Guide](../CONTRIBUTING.md)** before you submit a pull request.
