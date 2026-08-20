# MCP Apps Architecture

## Status

- **Document status:** Active architecture guidance
- **SDK:** `ModelContextProtocol.Extensions.Apps` 2.2.0
- **Existing App:** Bookmark Explorer
- **Planned App:** Library Analytics Dashboard

## Objective

Define the shared architecture and engineering rules for MCP Apps in this repository so every App is static, inspectable, isolated, safely rendered, and useful in hosts without Apps.

## Design

### Stable resources, dynamic results

Every App uses a stable `ui://` URI and `text/html;profile=mcp-app`. The resource contains only the application artifact. Tool-specific data belongs in `structuredContent`, with concise `content` as the fallback.

Never use per-invocation resource URIs, GUID resource names, a mutable resource body, or a singleton latest-result cache. Hosts may prefetch and cache UI resources, so reading a URI must return the same artifact regardless of tool-call order.

### Tool visibility

- Model-facing launch tools include model visibility.
- A tool callable only from its App uses app-only visibility.
- App-only tools must be narrowly scoped and independently authorized by the host.
- An App must not assume it can call tools from another server.

### Lifecycle

Register `ontoolresult`, `ontoolcancelled`, and other one-shot handlers before `connect()`. Render an initial waiting state, validate every incoming result, and treat cancellation, tool errors, malformed data, connection errors, and teardown as distinct states.

## Architecture

```text
MCP server
  |-- tools/list: _meta.ui.resourceUri + visibility
  |-- tools/call: content + structuredContent
  +-- resources/read: immutable text/html;profile=mcp-app
                                      |
                                      v
Host sandbox <---- postMessage ---- App SDK instance
```

### Source and artifact layout

- Editable App source lives under `src/Mcp/Ui/Static` or an App-specific source directory.
- The frontend build emits a self-contained HTML artifact.
- The artifact is embedded by the .NET project and read through a resource method.
- Generated artifacts are committed only when required by the repository’s packaging model and must remain reproducible from their source and lockfile.

### Result isolation

App state is instance-local. A notification replaces or updates only that instance’s state. Server resource providers contain no invocation data. Detail requests should use request-local state or identifiers so late responses cannot overwrite a newer selection.

## Security

- Use the narrowest CSP domain allowlists and sandbox permissions; empty allowlists are preferred when an App needs no network resources.
- Do not use `innerHTML` for user- or bookmark-controlled content.
- Validate URL schemes before navigation and use `noopener noreferrer`.
- Do not expose the App instance or server-call helpers on `window`.
- Do not expose tokens, headers, exception details, or internal diagnostics to the App.
- Validate structured-content types, sizes, arrays, and schema versions before rendering.
- Keep dependencies bundled and prohibit runtime CDN loading.

## Accessibility and host integration

- Use semantic headings, lists, tables, buttons, and status regions.
- Preserve keyboard operation and visible focus.
- Provide text equivalents for charts and status colors.
- Respect host theme, container dimensions, safe-area information, and reduced motion where practical.
- Allow the SDK’s resize integration to report content size without fixed assumptions about the host.

## Testing strategy

### Server and metadata

- Verify nested `_meta.ui.resourceUri` and visibility.
- Verify the resource exists, uses the MCP App MIME profile, and returns identical bytes.
- Verify non-App hosts receive a meaningful text result.

### Artifact

- Verify handlers precede `connect()`.
- Verify no global exposure, unsafe HTML insertion, or external runtime references.
- Verify waiting, empty, invalid, partial, cancelled, error, and success states.
- Verify malicious titles, tags, excerpts, domains, and URLs render as inert text.

### Isolation and packaging

- Verify overlapping calls and detail requests cannot overwrite one another incorrectly.
- Verify resources do not change before or after calls.
- Verify artifacts are present in build, publish, and package output.
- Smoke-test supported hosts because Apps remains an extension with host-specific adoption.

## Remaining tasks

### Shared infrastructure

- [ ] Add reusable result-schema validation helpers or per-App validators.
- [ ] Add a repeatable frontend build verification command in CI.
- [ ] Add CSP and external-reference assertions for every App artifact.
- [ ] Add publish and NuGet package artifact tests.
- [ ] Document supported host/version combinations.

### Explorer

- [ ] Render bookmark details semantically instead of as raw JSON.
- [ ] Prevent stale detail responses from replacing a newer request.
- [ ] Test malformed structured results and unsafe bookmark URLs.
- [ ] Test host-context responsiveness and teardown.

### Analytics dashboard

- [ ] Implement the work specified in [Library Analytics Dashboard](ANALYTICS_DASHBOARD.md).

## Acceptance criteria

An App follows this architecture when its tool metadata is standards-aligned, its resource is immutable and packaged, its result is validated and isolated, all bookmark-controlled values are inert, all UI states are accessible, and a useful text fallback remains available.

## References

- MCP Apps specification: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/draft/apps.mdx
- MCP C# SDK Apps package: https://www.nuget.org/packages/ModelContextProtocol.Extensions.Apps
- MCP resources specification: https://modelcontextprotocol.io/specification/2025-11-25/server/resources
