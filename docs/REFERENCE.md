# Technical Reference

This document provides a detailed reference for the technical components of the Raindrop MCP server. For a more hands-on guide, see the [Tutorial](./TUTORIAL.md).

-   [Back to Home](../README.md)

---

## **Project Structure**

This is a high-level overview of the most important files and directories in the `src/Mcp` project.

-   **`Program.cs`**: The entry point of the application. Configures and runs the MCP host.
-   **`RaindropServiceCollectionExtensions.cs`**: An extension method to register all the Raindrop.io services with the DI container.
-   **`/Collections`, `/Highlights`, etc.:** Each directory contains the models, API interface, and MCP tools for a specific Raindrop.io resource.
-   **`/Common`**: Contains shared models and base classes used across the project.
-   **`appsettings.json`**: Used for configuration, such as API keys.

---

### **MCP Tools**

_This section is auto-generated. To update it, please see the **[developer how-to guide](./how-to-guides/for-developers.md)** for instructions._



---

## **Configuration (`appsettings.json`)**

| Key                     | Type     | Description                                                       |
| :---------------------- | :------- | :---------------------------------------------------------------- |
| `Raindrop:ClientId`     | `string` | **Required.** The Client ID for your Raindrop.io application.     |
| `Raindrop:ClientSecret` | `string` | **Required.** The Client Secret for your Raindrop.io application. |

---

## **Core Classes & Interfaces**

-   **`ICollectionsApi`**: Interface defining the contract for interacting with the Raindrop.io Collections API.
-   **`CollectionsTools`**: Implements the MCP tools related to collections.
-   **`RaindropToolBase`**: A base class for tool containers, providing common functionality.

For more details on the architecture, see the [Explanation](./EXPLANATION.md) document.