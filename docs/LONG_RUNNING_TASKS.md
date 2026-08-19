# MCP Long-Running Tasks Specification

## Status

- **Document status:** Proposed
- **Target capability:** Experimental `io.modelcontextprotocol/tasks` task-augmented `tools/call` on MCP protocol `2026-07-28` or later
- **Initial tool:** `analyze_library`
- **MCP implementation:** `ModelContextProtocol.Extensions.Tasks` 2.2.0
- **Current implementation status:** Partially implemented with the experimental `ModelContextProtocol.Extensions.Tasks` package. The SDK adapter, polling, cancellation plumbing, and synchronous fallback are present; progress reporting, production-grade task storage, and end-to-end host conformance remain planned.

MCP Tasks remain experimental. This document defines the behavior implemented through the official .NET SDK Tasks extension so the library analytics engine does not invent a proprietary task protocol.

## Objective

Allow an MCP client to start a complete Raindrop library analysis without holding open a normal tool call. The server paginates through the selected bookmark scope, aggregates collection, tag, and domain statistics, supports cooperative cancellation, and retains the final `CallToolResult` for deferred retrieval. Factual progress updates are planned; SDK 2.2.0 currently relies on polling and does not provide server-push task status notifications.

The first task-capable operation will be the model-facing tool:

```text
analyze_library(collectionId?)
```

The tool expresses user intent. Raindrop pagination, page size, retries, task TTL, polling, concurrency, and server safety limits are implementation details and are not tool arguments.

## Non-goals

The initial implementation will not:

- Add a custom protocol that resembles MCP Tasks.
- Implement MCP task methods before the .NET SDK supports them.
- Actively probe bookmark URLs or classify broken links.
- Include Trash in a full-library report unless explicitly requested.
- Guarantee that a report survives server process termination.
- Require an MCP App to start or retrieve an analysis.
- Expose API pagination or page-size controls to the model.
- Retain complete bookmark records when incremental aggregates are sufficient.

## User-visible tool contract

### Tool name

`analyze_library`

### Description

Analyzes the user's Raindrop library and returns its collection hierarchy, bookmark distribution, most common domains and tags, and organization-quality metrics. Omit `collectionId` to analyze the complete non-trash library. Supply a positive collection ID to analyze that collection and its subcollections, `-1` for Unsorted, or `-99` for Trash.

### Input

| Field | Type | Required | Default | Meaning |
| --- | --- | --- | --- | --- |
| `collectionId` | integer | No | `0` | `0` analyzes all non-trash bookmarks, `-1` analyzes Unsorted, `-99` analyzes Trash, and a positive value analyzes that collection and its descendants. |

All other negative collection IDs are invalid.

### Annotations

- Read-only: `true`
- Destructive: `false`
- Idempotent: `true`
- Task support: `optional` during initial rollout; it may become `required` only after supported clients are verified and a migration plan exists

When task augmentation is unavailable, the server executes the same operation as a normal tool call. Analysis continues until Raindrop returns the end of the selected scope, cancellation is requested, an API failure remains after the configured retries, or the configured page safety limit is reached. The default is 1,000 pages of 50 bookmarks and configuration accepts at most 10,000 pages. A failed or safety-limited scan produces a clearly marked partial report containing the aggregates collected so far.

## Analysis scope

### Full library

When `collectionId` is omitted or `0`, the analyzer will:

1. Fetch root collections.
2. Fetch child collections.
3. Construct the user collection hierarchy.
4. Fetch every non-trash bookmark through Raindrop collection `0` in pages of 50.
5. Deduplicate bookmarks by ID.
6. Group bookmarks by their collection reference.
7. Roll direct counts up through the collection tree.
8. Calculate library-wide tag, domain, and organization metrics.

Unsorted bookmarks will appear as a synthetic system-collection node. Trash is excluded.

### User collection

When `collectionId` is positive, the analyzer will include the selected collection and its descendants. The report will distinguish:

- bookmarks directly in each collection;
- bookmarks in descendant collections; and
- total bookmarks in each collection subtree.

### System collections

- `-1` analyzes Unsorted.
- `-99` analyzes Trash.

System collections will be represented as synthetic report nodes because Raindrop does not return them in ordinary collection-list responses.

## Analysis engine contract

The core analyzer must not depend on MCP task protocol types. It will accept:

- the normalized analysis request;
- an optional progress reporter; and
- a cancellation token.

It will return a typed `LibraryAnalyticsReport`.

This boundary allows the same analyzer to run from:

- a normal MCP tool call;
- a future task-augmented MCP tool call;
- unit and integration tests; and
- a future scheduled or hosted worker.

### Progress phases

The analyzer will publish factual phase and count information:

1. `loading_collections`
2. `building_collection_tree`
3. `loading_bookmarks`
4. `aggregating`
5. `finalizing`
6. `completed`

Progress will contain fields such as:

- current phase;
- pages fetched;
- bookmarks processed;
- collections processed; and
- a human-readable status message.

The server will not publish a percentage unless it has a reliable total. Messages such as "Analyzed 600 bookmarks across 12 pages" are preferred to speculative percentages.

## Result contract

The final result will contain a concise text summary for the model and a structured analytics report.

### Scope and completeness

- requested and effective collection ID;
- start and completion timestamps;
- pages fetched;
- unique bookmarks processed;
- whether analysis completed the selected scope;
- termination reason; and
- diagnostics encountered during pagination or hierarchy construction.

Supported termination reasons include:

- `end_of_results`
- `safety_limit_reached`
- `deadline_exceeded`
- `api_error`
- `cancelled`

### Summary metrics

- bookmarks analyzed;
- root and child collection counts;
- maximum collection depth;
- unique domains;
- unique tags;
- untagged bookmarks;
- bookmarks without domains;
- bookmarks without excerpts; and
- Unsorted bookmarks when they are in scope.

Additional metrics may be added only when their Raindrop fields are documented and covered by tests.

### Collection distribution

Each collection entry will contain:

- collection ID and title;
- parent collection ID;
- hierarchy depth;
- direct bookmark count;
- descendant bookmark count;
- subtree bookmark count; and
- percentage of analyzed bookmarks.

The report may include Raindrop's collection-level count for comparison, but it must label API-reported and analyzer-observed counts separately.

### Domain and tag distributions

Domain and tag entries will contain a normalized label, count, and percentage of analyzed bookmarks. Tag percentages may total more than 100 percent because a bookmark can have multiple tags.

Ordering will be deterministic: count descending, then label ascending.

### Diagnostics

The report will record bounded diagnostics for:

- duplicate bookmark IDs encountered across pages;
- orphaned collection parent references;
- bookmarks assigned to unknown collections;
- collection hierarchy cycles;
- failed or incomplete API pages; and
- safety-limit or deadline termination.

## Incremental aggregation

The analyzer will update counts while reading each page. It will not retain every bookmark solely to calculate aggregates.

Memory use should primarily scale with distinct collections, tags, and domains. Any example bookmark lists included in the report must have explicit per-category and total limits.

Pagination will use a deterministic sort and deduplicate by bookmark ID to reduce errors caused by library changes during a scan. A report is a bounded observation over its start-to-completion interval, not an atomic database snapshot.

## MCP task protocol behavior

### Capability negotiation

Task support will be advertised only when the server has a conforming implementation for all advertised operations.

The server advertises the `io.modelcontextprotocol/tasks` extension through the SDK when the task store is configured. The SDK supplies task creation, polling, input updates, and cancellation handlers. This stdio implementation does not expose a task-list operation.

The `analyze_library` tool will declare `execution.taskSupport` as `optional` initially. Clients must not augment the call unless the server advertises task support and the tool permits it.

### Creation

A task-augmented `analyze_library` call will return a `CreateTaskResult` promptly. The initial task will contain:

- a cryptographically secure, opaque task ID;
- status `working`;
- creation and last-update timestamps;
- the actual TTL selected by the server;
- a suggested polling interval; and
- a useful status message.

The initial response does not contain the analytics result. The completed task returned by polling contains the serialized underlying `CallToolResult`.

### Status and polling

Clients poll through `tasks/get` and should respect the task's polling interval.

Task status will follow only these transitions:

- `working` to `completed`
- `working` to `failed`
- `working` to `cancelled`

`input_required` is not needed for the initial analysis operation. Terminal states never transition again.

### Result retrieval

After completion, `tasks/get` returns a completed task containing exactly the serialized `CallToolResult` that a successful normal invocation would have returned. SDK clients may use `CallToolWithPollingAsync` to poll and unwrap that result automatically.

### Cancellation

`tasks/cancel` will cancel the analyzer's cancellation token and prevent new API pages from starting. In-flight I/O receives the same token.

Task cancellation is cooperative and eventually consistent. The SDK acknowledges `tasks/cancel`, marks a non-terminal in-memory task as cancelled, and signals the tool's cancellation token. Tools must pass that token to each API request and stop before starting another page. A terminal result wins races with late cancellation, and cancelled analyses do not return partial aggregates as a successful result.

### TTL and cleanup

The task store will:

- apply a bounded default TTL;
- honor a requested TTL only within configured minimum and maximum values;
- remove expired task state and results;
- limit active tasks per requestor;
- limit retained completed results per requestor; and
- limit serialized result size.

TTL expiration is measured from task creation as defined by MCP.

## Task storage and requestor isolation

The current stdio deployment is normally a single-user process and may initially use an in-memory task store. In-memory tasks do not survive server termination.

If an authorization context exists, every task must be bound to it. Task polling, input updates, and cancellation must reject tasks belonging to a different context.

If the server cannot identify requestors:

- task IDs must be cryptographically secure and unguessable;
- TTLs should be short;
- task operations must be rate-limited; and
- the server must not add a task-list operation without requestor isolation.

Persistent execution is a separate deployment concern. Durable tasks that survive process restarts require persistent state plus a worker whose lifetime is independent of the stdio server process.

## Concurrency and resource controls

The implementation will configure server-side limits rather than exposing them as tool parameters:

- maximum concurrent analyses globally;
- maximum concurrent analyses per requestor;
- a configurable page safety limit, defaulting to 1,000 and capped at 10,000 pages;
- an optional overall analysis deadline;
- Raindrop API retry and backoff policy;
- maximum result and diagnostic sizes; and
- task TTL bounds.

The analyzer will page sequentially unless measurements show that bounded concurrent pagination is both safe and consistent. Collection and child-collection metadata requests may run concurrently when cancellation and API limits are respected.

## Failure behavior

Errors returned to clients will be sanitized. API tokens, request headers, internal stack traces, and raw exception messages must not appear in task status or results.

The task will fail when the analyzer cannot produce a trustworthy report. If Raindrop fails after retries, the analyzer returns an explicitly partial report with termination reason `api_error` and bounded, sanitized diagnostics; cancellation continues to propagate rather than being converted into a partial success.

Transient API errors may be retried using the server's existing policy. Cancellation is not an error and must not be retried.

## SDK and compatibility

The repository uses `ModelContextProtocol.Extensions.Tasks` 2.2.0. The server configures the SDK-provided `InMemoryMcpTaskStore`, which advertises the experimental Tasks extension, executes opted-in calls outside the initiating request, supplies polling and cancellation handlers, and retains results for the configured TTL.

Only `analyze_library` is task-capable. Other tools remain synchronous. Task execution is optional, so clients without the extension continue to receive the ordinary result from the same tool contract. No proprietary task arguments or custom task protocol handlers are used.

The in-memory store is appropriate for the current single-process stdio deployment, but tasks and results do not survive process termination. A persistent `IMcpTaskStore` is required before restart durability can be promised.

## Implementation checklist

### Phase 1: Task-independent analytics engine

- [x] Define the optional `collectionId` request contract and validation.
- [x] Define `LibraryAnalyticsReport`, distribution, hierarchy, completeness, and diagnostic contracts.
- [x] Implement root- and child-collection retrieval.
- [x] Construct the hierarchy independently of API response order.
- [x] Detect missing parents, duplicate IDs, self-parenting, and cycles.
- [x] Represent Unsorted and Trash as synthetic nodes when in scope.
- [x] Implement paginated bookmark retrieval using 50 items per page.
- [x] Use a deterministic sort and deduplicate bookmarks by ID.
- [x] Propagate cancellation through every API operation.
- [x] Aggregate bookmark, collection, tag, and domain metrics incrementally.
- [x] Calculate direct, descendant, and subtree collection counts.
- [x] Apply deterministic distribution ordering.
- [x] Record scope, timestamps, completion state, and termination reason.
- [ ] Bound examples and diagnostics in the result.
- [ ] Add an internal progress reporting abstraction.

### Phase 2: Normal MCP tool

- [x] Register `analyze_library` as read-only, non-destructive, and idempotent.
- [x] Keep `collectionId` as the only public scope argument.
- [x] Default omitted `collectionId` to `0`.
- [x] Reject unsupported negative collection IDs.
- [x] Continue pagination up to the configurable page safety limit.
- [x] Return a partial report when a bookmark page still fails after HTTP retries.
- [ ] Apply an optional overall deadline and concurrency controls.
- [x] Return a concise text summary plus structured analytics data.
- [x] Clearly identify partial results in both text and structured output.
- [x] Do not associate a UI resource in the initial tool implementation.

### Phase 3: Analyzer tests

- [ ] Test empty, root-only, and deeply nested collection hierarchies.
- [ ] Test missing parents, duplicate collection IDs, and hierarchy cycles.
- [x] Test direct, descendant, and subtree count calculations.
- [x] Test full-library, selected-collection, and Trash scopes.
- [ ] Test empty, short, multiple-full, exact-multiple, and terminal-empty pagination.
- [x] Test duplicate bookmarks across pages.
- [ ] Test cancellation between and during page requests.
- [x] Test API failure after successful pages.
- [x] Test graceful partial results after page-retrieval failure.
- [ ] Test deadline termination.
- [ ] Test null, empty, repeated, and case-varied tags and domains.
- [x] Test deterministic ordering and percentage calculations.
- [ ] Test bounded diagnostics and result size.

### Phase 4: MCP Tasks extension readiness gate

- [x] Identify a .NET MCP SDK package with task APIs.
- [ ] Verify target hosts negotiate `tasks.requests.tools.call`.
- [ ] Confirm SDK serialization against the `2026-07-28` Tasks extension protocol used by SDK 2.2.0.
- [ ] Decide whether `analyze_library` task support remains optional or becomes required.
- [ ] Document fallback behavior for hosts without Tasks.
- [ ] Add protocol conformance fixtures before advertising capabilities.

### Phase 5: Task store and executor

- [ ] Define task state, result, ownership, TTL, and progress storage abstractions.
- [ ] Generate cryptographically secure opaque task IDs.
- [ ] Bind tasks to authorization/requestor context when available.
- [ ] Implement bounded global and per-requestor execution queues.
- [ ] Start accepted tasks outside the initiating request lifetime.
- [ ] Link task cancellation to analyzer cancellation.
- [ ] Persist final `CallToolResult` until TTL expiration.
- [ ] Implement cleanup for expired tasks and abandoned execution.
- [ ] Enforce active-task, retained-result, and serialized-size limits.
- [ ] Cancel active work during graceful server shutdown.

### Phase 6: MCP task protocol adapter

- [x] Advertise the experimental Tasks extension through the SDK task store integration.
- [x] Keep non-analytics tools in synchronous execution mode.
- [x] Configure `analyze_library` for optional task execution.
- [x] Accept task-augmented `tools/call` requests through the SDK.
- [x] Return `CreateTaskResult` promptly with status, TTL, and polling interval.
- [x] Use the SDK implementation of `tasks/get`.
- [x] Return the original serialized `CallToolResult` in completed task state.
- [x] Use the SDK implementation of `tasks/cancel` and terminal-state validation.
- [ ] Add a task-list operation only if requestor isolation and a product requirement are established.
- [ ] Publish optional task progress notifications when the SDK exposes a working-status update API.

### Phase 7: Task protocol tests

- [ ] Test capability and tool-level task negotiation.
- [ ] Test normal calls when task support is optional.
- [ ] Test rejection of augmentation for forbidden tools.
- [ ] Test immediate task creation response.
- [ ] Test every valid status transition and reject invalid transitions.
- [ ] Test polling interval and timestamp updates.
- [ ] Test completed, failed, and cancelled result retrieval.
- [ ] Test cancellation races with in-flight completion.
- [ ] Test TTL bounds and expiration.
- [ ] Test requestor isolation and unguessable task IDs.
- [ ] Test task-list pagination if supported.
- [ ] Test rate and concurrency limits.
- [ ] Test graceful shutdown behavior.
- [ ] Test that secrets and internal exceptions never appear in status or results.

### Phase 8: Operational validation

- [ ] Benchmark representative small, medium, and large libraries.
- [ ] Record API calls, elapsed time, allocations, and final result size.
- [ ] Tune the page limit, retries, concurrency, deadlines, and polling cadence from measurements.
- [ ] Verify behavior when the library changes during pagination.
- [ ] Verify host polling, cancellation, and result retrieval end to end.
- [ ] Document that in-memory stdio tasks do not survive process termination.
- [ ] Add persistent execution only if restart durability becomes a requirement.

## Acceptance criteria

The long-running task implementation is complete when:

1. The model invokes `analyze_library` with no operational parameters.
2. A task-capable host can request task augmentation and receive a prompt task handle.
3. The server exposes factual task state through polling; future progress messages report counts rather than speculative percentages.
4. The client can poll, cancel, and retrieve the final result using standard MCP methods.
5. The final result is identical in shape to the normal tool result.
6. Full-library analysis includes root and child collections plus all non-trash bookmarks.
7. Selected-collection analysis includes the selected collection and descendants.
8. Results distinguish direct and recursive collection distribution.
9. Pagination, retries, cancellation, deadlines, and graceful partial failure are deterministic and tested.
10. Task ownership, TTL cleanup, concurrency limits, and secret handling satisfy this specification.
11. The server advertises only task capabilities it actually implements.
12. Hosts without Tasks retain documented synchronous behavior that scans the same scope up to the configured safety limit and degrades gracefully after exhausted retries.

## Sources

- .NET MCP Tasks documentation: https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.2.0/docs/concepts/tasks/tasks.md
- .NET MCP Tasks package: https://www.nuget.org/packages/ModelContextProtocol.Extensions.Tasks
- Tasks extension SEP-2663: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/seps/2663-tasks-extension.md
- MCP tool specification: https://modelcontextprotocol.io/specification/2025-11-25/server/tools
- MCP Apps specification: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/draft/apps.mdx
- Raindrop collections documentation: https://developer.raindrop.io/v1/collections
- Raindrop multiple-bookmark API: https://developer.raindrop.io/v1/raindrops/multiple
