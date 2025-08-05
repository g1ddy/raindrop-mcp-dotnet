# How-To Guides for Developers

This guide provides recipes for developers who want to set up a local development environment, modify, or contribute to the Raindrop MCP .NET server.

**Prerequisite:** You have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later installed.

-   [Back to Home](../../README.md)

---

### **How to Set Up a Development Environment**

Running the server from source is ideal for debugging, testing new features, or making contributions. This setup ensures you are working with the exact code in your local repository.

**1. Clone the Repository:**

```sh
git clone https://github.com/g1ddy/raindrop-mcp-dotnet.git
cd raindrop-mcp-dotnet
```

**2. Securely Store Your API Token:**

Pasting secrets in configuration files is not secure. .NET provides a "Secret Manager" tool to keep secrets separate from your project code.

*   **Initialize User Secrets:** In the `src/Mcp` directory, run:
    ```sh
    dotnet user-secrets init
    ```

*   **Set Your API Token Secret:** Use this command to securely store your token. The application is already configured to read this key.
    ```sh
    dotnet user-secrets set "Raindrop:ApiToken" "PASTE_YOUR_TOKEN_HERE"
    ```

**3. Connect Your Client to the Local Source:**

Configure your MCP-compatible client to run the server directly from your local project file.

*   **Open VS Code Settings** (`File > Preferences > Settings`) and find the `mcp.json` file.
*   **Add the Server Configuration:**

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

### **Next Steps: Contributing**

Now that you know how to extend the server, please review our **[Contributing Guide](../../CONTRIBUTING.md)** before you submit a pull request.