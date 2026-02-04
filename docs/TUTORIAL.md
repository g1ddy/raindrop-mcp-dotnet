# Tutorial: Your First Raindrop.io Automation

**Welcome!** This tutorial will guide you through using the Raindrop MCP server to perform a simple automation after you have completed the initial setup.

---

### **Step 1: Choose Your Setup Path**

Before you can use the server, it must be configured. Please select the guide that matches your goal:

-   **For Users (Recommended):**
    If you want to use the server without modifying its code, follow the quick and easy setup instructions in the **[Package README](../src/Mcp/README.md)**. This guide covers the recommended installation methods, including using `dnx` with the manifest for secure token configuration.

    <!--
    MAINTAINER NOTE:
    The setup instructions are intentionally kept in src/Mcp/README.md because that file is
    packaged with the NuGet release and displayed on NuGet.org. We avoid duplicating the
    instructions here to keep our documentation DRY and ensure the distributed README is
    always the single source of truth for end-users.
    -->

-   **For Developers:**
    If you want to run the server from source to debug, modify, or contribute to the project, follow the **[How to Set Up a Development Environment](./HOW_TO.md)** guide.

**Once you have completed one of these setup guides, return here to continue.**

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
