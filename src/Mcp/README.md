# Raindrop.io MCP Server

This package provides a [.NET-based MCP server](https://microsoft.github.io/copilot-extensibility-docs/docs/mcp/introduction) for interacting with your [Raindrop.io](https://raindrop.io) bookmarks using natural language.

It allows you to connect AI clients, like the one in Visual Studio Code, to manage your bookmarks through simple chat commands.

## Setup and Configuration

You can connect your client to the server in two ways:

1.  **DNX (Recommended):** The client dynamically downloads and runs the server from NuGet. This is the easiest way to get started and ensures you are always running the latest version.
2.  **Global Tool:** The server is installed as a .NET global tool and run as a standalone command. This is a stable, versioned alternative.

---

### Recommended: DNX Setup

The DNX method dynamically downloads and runs the tool from NuGet, which requires the **.NET 10 SDK (preview)** or a later version.

This is the easiest way to get started and ensures you are always running the latest version.

#### Step 1: Get Your Raindrop.io API Token

To access your account, the server needs a secure API token.

1.  Navigate to the [**For Developers**](https://app.raindrop.io/settings/integrations) section in your Raindrop.io settings.
2.  Click **+ Create new app**, give it a name (e.g., "My MCP Client"), and click **Create**.
3.  Under the **Test token** section, click **Create** to generate a new token.
4.  **Copy the token.** You will need it for the next step.

#### Step 2: Connect Your Client

Configure your MCP-compatible client to launch the server using `dnx`. This allows the client to manage the download, update, and execution process.

Below is an example for the **VS Code Copilot Agent**:

1.  **Open VS Code Settings** (`File > Preferences > Settings`) and search for "mcp".
2.  Click the **Edit in settings.json** link for the MCP configuration.
3.  Add the following server configuration to your `mcp.json` file:

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
            "Raindrop:ApiToken": "PASTE_YOUR_TOKEN_HERE",
            "Raindrop:BaseUrl": "https://api.raindrop.io/rest/v1"
          }
        }
      }
    }
    ```

4.  Replace `PASTE_YOUR_TOKEN_HERE` with the API token you copied from Raindrop.io.

---

### Alternative: Global Tool Setup

This is the traditional method for installing versioned .NET tools.

#### Step 1: Install the Tool

Install the server as a global .NET tool:

```sh
dotnet tool install --global Raindrop.Mcp.DotNet --version 0.1.1-beta
```

#### Step 2: Get Your Raindrop.io API Token

Follow the same token generation steps outlined in the DNX setup.

#### Step 3: Connect Your Client

Add the following server configuration to your `mcp.json` file:

```json
{
  "servers": {
    "RaindropMcp": {
      "type": "stdio",
      "command": "Mcp",
      "env": {
        "Raindrop:ApiToken": "PASTE_YOUR_TOKEN_HERE",
        "Raindrop:BaseUrl": "https://api.raindrop.io/rest/v1"
      }
    }
  }
}
```

Replace `PASTE_YOUR_TOKEN_HERE` with your API token.

---

## Manage Your Bookmarks

Once configured, you can interact with your bookmarks through your AI client.

**Example Prompt (VS Code):**

> `@RaindropMcp Find my bookmark about 'Gemini CLI' and add the tag '#ai-tool' to it.`

The client will automatically start the server to handle your request. Enjoy managing your bookmarks!