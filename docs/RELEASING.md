# Releasing Guide

This guide describes how to publish new versions of the **Raindrop MCP .NET** server to NuGet.org and GitHub Packages.

## 🚀 The Release Process

The publishing process is fully automated using GitHub Actions. It is triggered whenever a **GitHub Release** is published.

### 1. Create a Tag (Optional but Recommended)

While you can create a tag directly in the GitHub Release UI, it is often better to create it locally to ensure you are tagging the exact commit you want.

To create and push the `v0.1.4-beta` tag (as an example), run:

```bash
git tag v0.1.4-beta
git push origin v0.1.4-beta
```

### 2. Create a GitHub Release

1.  Go to the **[Releases](../../releases)** page of the repository.
2.  Click **"Draft a new release"**.
3.  **Choose a tag**: Select the tag you just pushed (e.g., `v0.1.4-beta`), or create a new one here if you skipped step 1.
4.  **Release title**: Enter a title (e.g., `v0.1.4-beta`).
5.  **Description**: Describe the changes in this release.
6.  Click **"Publish release"**.

Once published, the [Publish workflow](../../actions/workflows/publish.yml) will automatically:
1.  Build and pack the project.
2.  Set the package version to match the tag (e.g., `0.1.4-beta`).
3.  Push the package to **NuGet.org**.
4.  Push the package to **GitHub Packages**.

---

## 🔑 Configuration & Secrets

To enable publishing, you must configure a secret for NuGet.org. GitHub Packages authentication is handled automatically by the `GITHUB_TOKEN`.

### Setting up the NuGet API Key

1.  **Generate a Key**:
    *   Log in to your [NuGet.org](https://www.nuget.org/) account.
    *   Go to **API Keys** -> **Create**.
    *   **Name**: `Raindrop MCP CI`.
    *   **Scopes**: Select **Push**.
    *   **Glob Pattern**: Enter `*` or `Raindrop.Mcp.DotNet` to restrict the key to this package.
    *   **Expires in**: 365 days (max).
    *   Click **Create** and **Copy** the key immediately (you won't see it again).

2.  **Add to GitHub**:
    *   Go to this repository's **Settings** -> **Secrets and variables** -> **Actions**.
    *   Click **New repository secret**.
    *   **Name**: `NUGET_API_KEY`
    *   **Secret**: Paste the key you copied from NuGet.org.
    *   Click **Add secret**.

### Best Practices for Secrets

*   **Scoping**: Always restrict your NuGet API keys to specific packages if possible. This limits the damage if a key is compromised.
*   **Expiration**: NuGet keys expire after a maximum of 1 year. **Set a calendar reminder** to rotate your keys before they expire to avoid failed builds.
*   **Least Privilege**: The GitHub Action uses the `GITHUB_TOKEN` for GitHub Packages, which is scoped only to this repository. This is secure by default.

---

## 📦 Versioning

The project uses **MinVer** to automatically handle versioning.
*   The version is determined solely by the Git tag (e.g., `v0.1.5-beta`).
*   There is **no** `<PackageVersion>` in the `.csproj` file.
*   The CI pipeline automatically picks up the version from the tag during the build process.
