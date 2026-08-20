# Library Analytics Dashboard

## Status

- **Document status:** Proposed
- **Backend:** Comprehensive metadata analytics exists through `analyze_library`
- **Dashboard tool and App:** Not implemented
- **Active URL probing:** Out of scope; see [Link Health](LINK_HEALTH.md)

## Objective

Present bookmark metadata analytics as an accessible MCP App without coupling the presentation resource to mutable server-side report state. Text-only and non-App clients must still receive a useful result.

## Design

### Product boundary

The dashboard visualizes metadata Raindrop already returns: collections, domains, tags, favorites, missing tags, missing domains, and missing excerpts. It does not make HTTP requests to bookmark URLs.

The comprehensive analysis should remain the source of truth. Do not create a second normalization or aggregation implementation merely for the dashboard. If host interoperability requires a separate `view_library_analytics` tool, it should project shared analytics contracts and must clearly define whether it performs a comprehensive synchronous scan or displays a supplied/completed report.

### Result contract

The App-facing structured result should include:

- a schema version;
- collection scope and descendant behavior;
- pages and bookmarks analyzed;
- generated timestamp, completeness, and termination reason;
- summary metrics;
- deterministically ordered domain, tag, and collection distributions;
- bounded diagnostics and review examples; and
- an explicit note that tag percentages overlap.

The text content should summarize scope, completeness, bookmark count, and leading findings for hosts that do not support MCP Apps.

### Presentation

- Summary metric cards.
- Ranked horizontal HTML/CSS bars with labels and numeric values.
- Collection distribution or hierarchy where it remains readable.
- Bounded review lists for untagged bookmarks and missing excerpts.
- Waiting, empty, invalid, partial, cancelled, and error states.
- Responsive layout using host context when available.
- No Chart.js, runtime CDN, external font, or network dependency.

## Architecture

```text
analytics tool result
   |-- content ------------------> model / text-only host
   |-- structuredContent --------> MCP App notification
   +-- _meta.ui.resourceUri -----> ui://raindrop/library-analytics
                                         |
                                         v
                              immutable embedded HTML artifact
```

The `ui://` resource is a static template. Results arrive only through the tool result notification; they are never interpolated into the resource and are never placed in a singleton “latest report” cache. Two invocations use the same resource bytes while retaining distinct structured results.

### Tasks boundary

`analyze_library` is optionally task-backed. The dashboard must not assume that an MCP App can call `tasks/get`, unwrap a completed Task, or receive the eventual result from every host. Before associating the App directly with `analyze_library`, verify the behavior of target hosts. Until that is proven, preserve the task-independent structured result and use a separately specified synchronous App entry point if necessary.

## Security and accessibility

- Validate the schema version and required structured fields before rendering.
- Render bookmark-controlled values with DOM text APIs, never unsafe HTML insertion.
- Permit navigation only to validated `http` or `https` URLs and use `noopener noreferrer`.
- Provide textual values for every visual bar; do not encode meaning through color alone.
- Keep keyboard focus, headings, table semantics, contrast, and reduced-motion behavior usable.
- Declare the narrowest MCP App CSP and permissions; the metadata-only dashboard needs no external origins.

## Remaining tasks

### Contracts and backend

- [ ] Decide whether the App attaches to `analyze_library` after host verification or uses `view_library_analytics`.
- [ ] Add an App-facing schema version and explicit output schema where supported.
- [ ] Share normalization and distribution code with the comprehensive analyzer.
- [ ] Bound diagnostic, distribution, and review-list payloads.
- [ ] Document partial-result rendering and normalization semantics.

### Static App

- [ ] Add the stable `ui://raindrop/library-analytics` resource.
- [ ] Bundle and embed one self-contained artifact.
- [ ] Register one-shot handlers before `connect()`.
- [ ] Validate structured content and render all UI states.
- [ ] Implement accessible summary, distribution, hierarchy, and review components.
- [ ] Add host-context responsive sizing without making host context mandatory.

### Verification

- [ ] Test useful text and typed structured results.
- [ ] Test empty, partial, invalid, cancelled, and error results.
- [ ] Test overlapping invocation isolation and immutable resource bytes.
- [ ] Test that bookmark-controlled content cannot inject markup or unsafe URLs.
- [ ] Assert that the artifact contains no runtime CDN or external resource references.
- [ ] Verify the embedded artifact in build, publish, and NuGet package output.
- [ ] Test target hosts with Apps alone and with Apps plus Tasks.

## Acceptance criteria

The dashboard is ready when the same immutable resource safely renders independent analytics calls, communicates scope and completeness accurately, remains useful without Apps, has no report cache or external runtime dependency, and passes accessibility, isolation, and packaging checks.

## References

- MCP Apps specification: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/draft/apps.mdx
- MCP tools specification: https://modelcontextprotocol.io/specification/2025-11-25/server/tools
- Raindrop API: https://developer.raindrop.io/v1
