# Tutorial: Your First Raindrop.io Automation

**Welcome!** This tutorial will guide you through setting up the Raindrop MCP server and using it to perform a simple automation: finding and tagging a bookmark.

This guide assumes you have already cloned the repository and have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

---

### **Step 1: Configure Authentication**

Before you can use the server, you need to give it access to your Raindrop.io account using a secure token.

1.  **Go to your Raindrop.io settings** and navigate to the [**For Developers**](https://app.raindrop.io/settings/integrations) section.
2.  Click **+ Create new app**, give it a name (e.g., "My MCP Server"), and click **Create**.
3.  In the app settings, find the **Test token** section and click **Create**. This will generate a secure API token for you.
4.  **Copy the generated token.**
5.  **Configure `appsettings.json`:** Open the `src/Mcp/appsettings.json` file and paste your token into the `ApiToken` field.

    ```json
    {
      // ... other settings
      "Raindrop": {
        "ApiToken": "PASTE_YOUR_TOKEN_HERE",
        "BaseUrl": "https://api.raindrop.io/rest/v1"
      }
    }
    ```

*Note: For better security, we recommend using the .NET Secret Manager instead of pasting the token directly. See our [Developer How-To Guide](./how-to-guides/for-developers.md) for details.*

### **Step 2: Run the MCP Server**

With authentication configured, you can now start the server.

```sh
cd raindrop-mcp-dotnet
dotnet run --project ./src/Mcp
```

The server is now running and waiting for a client to connect.

### **Step 3: Connect Your Client (Example: VS Code)**

To interact with the server, you need an MCP-compatible client. Here’s how to configure the VS Code Copilot Agent:

1.  **Open VS Code Settings:** Go to `File > Preferences > Settings` and search for "mcp".
2.  **Edit `mcp.json`:** Click the "Edit in settings.json" link for the MCP configuration.
3.  **Add the Server:** Add the following configuration to your `mcp.json` file. This tells the agent how to start and communicate with your server.

    ```json
    {
      "servers": {
        "RaindropMcp": {
          "type": "stdio",
          "command": "dotnet",
          "args": ["run", "--project", "C:/path/to/your/raindrop-mcp-dotnet/src/Mcp"],
          "env": {
            "Raindrop:ApiToken": "PASTE_YOUR_TOKEN_HERE",
            "Raindrop:BaseUrl": "https://api.raindrop.io/rest/v1"
          }
        }
      }
    }
    ```
    *Make sure to replace the path with the absolute path to the `src/Mcp` directory on your machine.*

### **Step 4: Interact with Your Bookmarks!**

Now for the fun part! Open the Copilot chat in VS Code and try asking it to perform a task. The agent will use your running MCP server to interact with your Raindrop.io data.

**Example Prompt:**

> "@RaindropMcp Find my bookmark about 'Gemini CLI' and add the tag '#ai-tool' to it."

**Congratulations!** You have successfully set up and used the Raindrop MCP server.

---

### **Next Steps**

Now that you have the basics down, you can explore more advanced topics:

-   **[User How-To Guide](./how-to-guides/for-users.md):** Discover more recipes for managing your bookmarks with natural language.
-   **[Technical Reference](./REFERENCE.md):** Explore the full list of available tools and their parameters.
