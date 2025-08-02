# How-To Guides for Developers

This guide provides a collection of recipes for developers who want to modify, extend, or contribute to the Raindrop MCP .NET server.

**Prerequisite:** You understand the basics of the project's architecture. For a deep dive, please review the [Explanation.md](../EXPLANATION.md) and [REFERENCE.md](../REFERENCE.md) documents.

-   [Back to Home](../../README.md)

---

### **How to Add a Custom Tool**

One of the most powerful features of this server is its extensibility. Here's how to add a new tool that gets the current user's information.

**1. Define the API Method (if necessary):**

The `IUserApi.cs` interface already defines a `GetCurrentUserAsync` method, so we can skip this step. If you were adding a completely new function, you would first add it to the appropriate API interface.

**2. Create the Tool Method:**

Open `src/Mcp/User/UserTools.cs` and add the following method:

```csharp
[Tool("Gets information about the current authenticated user.")]
public async Task<UserInfo> GetCurrentUser(CancellationToken cancellationToken) {
    var response = await _userApi.GetCurrentUserAsync(cancellationToken);
    return response.User;
}
```

-   The `[Tool]` attribute makes the method discoverable to the MCP server and provides the description that will be shown to the AI model.
-   The method calls the underlying `_userApi` to fetch the data.

**4. Update Documentation:**

After adding your tool and verifying it works, update the technical reference:

```sh
dotnet build ../../src/Mcp
../../scripts/generate-docs.ps1
```

---

### **How to Securely Store Your API Token**

Pasting secrets in `appsettings.json` is fine for quick tests, but not secure. .NET provides a "Secret Manager" tool to keep secrets separate from your project code.

**1. Initialize User Secrets:**

In the `src/Mcp` directory, run this command:

```sh
dotnet user-secrets init
```

**2. Set Your API Token Secret:**

Use this command to securely store your API token. The `Raindrop:ApiToken` key directly maps to the configuration the application uses.

```sh
dotnet user-secrets set "Raindrop:ApiToken" "PASTE_YOUR_TOKEN_HERE"
```

The application is already configured to read from user secrets in a development environment, so no code changes are needed. It will prioritize the secret from the Secret Manager over the value in `appsettings.json`.

---

### **Next Steps: Contributing**

Now that you know how to extend the server, consider contributing your changes back to the project! Before you submit a pull request, please review our **[Contributing Guide](../../CONTRIBUTING.md)** for information on our development process and code style.

---

*More guides will be added here as the project evolves. Have a request? Open an issue!*
