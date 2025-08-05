# Explanation: Design and Architecture

This document explains the "why" behind the technical choices made in this project. It's intended to give contributors and developers a deeper understanding of the project's design.

-   [Back to Home](../README.md)
-   [How-To Guide](./HOW_TO.md)

---

## Core Philosophy

### **Why Model Context Protocol (MCP)?**

The primary goal of this project is to enable AI models to interact with Raindrop.io. While we could have built a traditional REST API, MCP offers several key advantages in this context:

1.  **Designed for AI:** MCP is specifically designed for the kind of interaction that AI agents require. It provides a structured way for the model to discover available tools and understand their capabilities.
2.  **Client-Side Control:** The MCP client (e.g., the IDE extension) is in control of when and how to execute tools. This is a better security model than exposing a generic API to the web.
3.  **Simplicity:** For this use case, MCP is simpler to implement and manage than a full-blown web server with authentication, routing, and CORS to worry about.

---

## Architectural Decisions

The server is built on modern .NET principles, emphasizing modularity, testability, and extensibility.

-   **Dependency Injection (DI):** We use .NET's built-in DI container to manage services. The main `Program.cs` registers all the necessary services, including the Raindrop.io API clients and the MCP tools themselves. This makes it easy to add new services or replace existing ones.

-   **Tool Discovery via Reflection:** Instead of manually registering each tool, the server uses reflection to discover any public method decorated with the `[Tool]` attribute. This reduces boilerplate and makes adding new tools as simple as writing a method.

-   **Strongly-Typed API Client:** The interaction with the Raindrop.io API is handled by a strongly-typed client, defined in the various `I*Api.cs` interfaces and their implementations. This provides compile-time safety and a better developer experience.

-   **Async Everywhere:** All operations that involve I/O (like calling the Raindrop.io API) are implemented asynchronously using `async`/`await`. This ensures the server remains responsive and can handle multiple requests efficiently.

---

## Future Considerations

-   **API Evolution:** The Raindrop.io API will evolve. Our use of a dedicated, strongly-typed client layer (`I*Api.cs`) isolates the rest of our application from these changes. When the API is updated, we only need to update the client layer, not the core logic of our tools.

-   **Cross-Platform Support:** By building on .NET, the server is inherently cross-platform and can run on Windows, macOS, and Linux.