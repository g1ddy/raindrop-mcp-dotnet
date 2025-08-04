# Raindrop MCP .NET Integration

[![Raindrop.io Logo](https://avatars.githubusercontent.com/u/182288589?s=80&v=4)](https://raindrop.io)

**A robust, high-quality Model Context Protocol (MCP) server for the Raindrop.io API, built with C# and .NET.**

This project provides a bridge, allowing AI models and development tools to securely and intelligently interact with your Raindrop.io bookmarks, collections, and highlights. It's designed for AI integrators, power users, and .NET developers looking to build powerful, context-aware applications.

![High-level diagram showing AI models connecting to the MCP Server, which in turn communicates with the Raindrop.io API.](https://i.imgur.com/example.png)

---

## 🚀 Key Features

- **AI-Ready:** Exposes Raindrop.io features as MCP tools, ready for consumption by agents like VS Code Copilot.
- **Secure:** Uses access tokens to securely connect to your Raindrop.io account.
- **Extensible:** Designed with dependency injection to be easily extended with new tools or data sources.
- **Developer-Friendly:** Strong-typed, well-documented, and easy to run and debug.

---

## 🗺️ Documentation Map

This project's documentation is structured using the [Diátaxis framework](https://diataxis.fr) to help you find what you need quickly.

| I want to...                                | Documentation                                                                | Description                                                                     |
| :------------------------------------------ | :--------------------------------------------------------------------------- | :------------------------------------------------------------------------------ |
| **...get started quickly.**                 | 📖 **[Tutorial](./docs/TUTORIAL.md)**                                         | A hands-on guide to get the server running and make your first API call.        |
| **...use the tool to manage my bookmarks.** | 🧑‍💻 **[How-To Guides for Users](./docs/how-to-guides/for-users.md)**           | Practical recipes for managing your Raindrop.io data using natural language.    |
| **...add new functionality to the code.**   | 👩‍💻 **[How-To Guides for Developers](./docs/how-to-guides/for-developers.md)** | Technical recipes for extending the server with new tools and features.         |
| **...understand the technical details.**    | 🔬 **[Technical Reference](./docs/REFERENCE.md)**                             | The encyclopedia of the project: API schemas, configuration, and class details. |
| **...understand the project's design.**     | 🧠 **[Explanation](./docs/EXPLANATION.md)**                                   | The "why" behind the project's architecture and design choices.                 |
| **...contribute to the project.**           | ❤️ **[Contributing Guide](./CONTRIBUTING.md)**                                | Our guide for how to contribute code, documentation, or bug reports.            |

---

## quick-start-guide Quick Start

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

1. **Clone the repository:**

    ```sh
    git clone https://github.com/g1ddy/raindrop-mcp-dotnet.git
    cd raindrop-mcp-dotnet
    ```

2. **Run the server:**

    ```sh
    dotnet run --project ./src/Mcp
    ```

3. **Connect your client:**
    For details on configuring your IDE or agent, see the **[Tutorial](./docs/TUTORIAL.md)**.

---

## 📝 License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
