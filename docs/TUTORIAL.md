# Tutorial: Your First Raindrop.io Automation

**Welcome!** This tutorial will guide you through setting up and using the Raindrop MCP server to automate your bookmark management.

---

### **Step 1: Setup and Configuration**

We recommend using `dnx` (part of the .NET SDK) to run the server. This method dynamically downloads the latest version of the server, ensuring you always have the newest features and fixes.

**Prerequisite:** Ensure you have the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later installed.

#### **1. Get Your Raindrop.io API Token**

To access your account, the server needs a secure API token.

1.  Navigate to the [**For Developers**](https://app.raindrop.io/settings/integrations) section in your Raindrop.io settings.
2.  Click **+ Create new app**, give it a name (e.g., "My MCP Client"), and click **Create**.
3.  Under the **Test token** section, click **Create** to generate a new token.
4.  **Copy the token.** You will need it for the next step.

#### **2. Secure Your API Token**

For security, it is best to store your API token in an environment variable rather than pasting it directly into your configuration files.

**Set the Environment Variable:**

*   **Windows (PowerShell):**
    ```powershell
    $env:RAINDROP_API_TOKEN="YOUR_TOKEN_HERE"
    ```
*   **macOS/Linux:**
    ```bash
    export RAINDROP_API_TOKEN="YOUR_TOKEN_HERE"
    ```

*Note: This variable will only be set for your current terminal session. For a permanent solution, add the command to your shell's startup file (e.g., `.bashrc`, `.zshrc`, or your PowerShell profile) or system environment variables.*

#### **3. Create the Client Manifest**

Configure your MCP client (like VS Code) to launch the server. This configuration file is often referred to as the **manifest** (`mcp.json`).

1.  **Open VS Code Settings** (`File > Preferences > Settings`) and search for "mcp".
2.  Click the **Edit in settings.json** link for the MCP configuration.
3.  Add the following server configuration to your `mcp.json` file. This tells the client to use `dnx` to run the server and securely passes your token from the environment.

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

**That's it!** Your client will now securely load the token from your environment and automatically download the latest version of the server.

---

### **Step 2: Interact with Your Bookmarks!**

Now for the fun part! With the server configured, you can open the Copilot chat in VS Code and try asking it to perform a task. The agent will use your configured MCP server to interact with your Raindrop.io data.

Here are some examples to get you started:

**Managing Bookmarks**

*   **Find a specific bookmark:**
    > "@RaindropMcp Find my bookmark about 'Model Context Protocol'."

*   **Add a tag to a bookmark:**
    > "@RaindropMcp Find my bookmark about 'Gemini CLI' and add the tag '#ai-tool' to it."

*   **Update the title of a bookmark:**
    > "@RaindropMcp Change the title of my bookmark with id 12345 to 'My New Title'."

**Managing Collections**

*   **List all of your collections:**
    > "@RaindropMcp List all of my collections."

*   **Create a new collection:**
    > "@RaindropMcp Create a new collection named 'AI Research'."

**Working with Highlights**

*   **Add a highlight to a bookmark:**
    > "@RaindropMcp Add a highlight with the text 'This is a key insight' to my bookmark with id 12345."

**Congratulations!** You have successfully set up and used the Raindrop MCP server.

---

### **Next Steps**

Now that you have the basics down, you can explore more advanced topics:

-   **[Developer How-To Guide](./HOW_TO.md):** Learn how to contribute, add new tools, and set up a development environment.
-   **[Technical Reference](./REFERENCE.md):** Explore the full list of available tools and their parameters.
