# Bookmark Link Health

## Status

- **Document status:** Design required; implementation deferred
- **Current analytics:** Metadata-only
- **Active probing:** Not implemented
- **Dependency:** Analytics and MCP Tasks operational hardening

## Objective

Define a safe, bounded way to help users identify bookmarks that may need review without turning the MCP server into an unrestricted network scanner or conflating Raindrop metadata with observations from active HTTP requests.

## Design

### Decision gate

Before implementing network requests, evaluate three modes:

1. **Raindrop metadata only:** report documented health or broken-link fields already returned by the API.
2. **Active probing only:** make new HTTP observations under a strict security policy.
3. **Combined:** display Raindrop’s state and the server’s observation as separate facts with separate timestamps.

Prefer metadata-only behavior unless active probing provides enough additional user value to justify its security and operational cost.

### Outcome model

Do not reduce every failure to “broken.” A future result should distinguish at least:

- reachable;
- redirecting;
- authentication or permission required;
- client error;
- server error;
- DNS failure;
- TLS failure;
- timeout;
- blocked by security policy;
- unsupported scheme; and
- indeterminate.

Each observation needs a timestamp, attempted method, final public URL when safe, response status where available, and a bounded diagnostic that contains no secret or internal network detail.

## Architecture

```text
selected bookmarks
       |
       v
URL validation -> DNS/IP policy -> bounded scheduler -> HTTP probe
       |                                  |
       +------ blocked outcome             +--> redirect revalidation
                                                  |
                                                  v
                                        typed health observations
```

The probing engine must be independent of MCP protocol types. A model-facing tool or Task adapter supplies scope, cancellation, and progress; the engine supplies validated observations. Dashboard integration consumes the typed result but does not initiate unrestricted fetches from browser JavaScript.

### Tasks boundary

Small, explicitly bounded reviews might run synchronously. Whole-library active probing is expected to be long-running and should use MCP Tasks after host conformance, concurrency, cancellation, and retention behavior are validated. Operational controls remain server configuration rather than model arguments unless a clear product need emerges.

## Threat model and network policy

Active probing must address:

- SSRF to loopback, private, link-local, multicast, carrier-grade NAT, and cloud metadata addresses;
- DNS rebinding and differences between validation-time and connection-time resolution;
- redirects to forbidden schemes, hosts, ports, or address ranges;
- IPv4, IPv6, mapped-address, integer, octal, hexadecimal, and alternative hostname representations;
- proxy behavior that could bypass local address validation;
- credential-bearing URLs and accidental header forwarding;
- oversized or endless responses;
- slowloris behavior and connection exhaustion; and
- excessive requests to one origin.

Every redirect hop must be revalidated. Do not send the Raindrop API token, cookies, user headers, or ambient credentials. Allow only explicitly supported schemes and ports. Enforce connection, header, body, redirect, per-host, and overall deadlines.

## Operational controls

- Bounded global concurrency and lower per-origin concurrency.
- Rate limiting and backoff that respects `Retry-After`.
- Maximum redirects.
- Maximum response bytes; do not download full bodies for health classification.
- A documented HEAD-versus-bounded-GET strategy.
- Cancellation propagated to queued and in-flight work.
- Bounded result, diagnostic, and example counts.
- No automatic retries for permanent classifications or security-policy blocks.
- Metrics for request count, latency, classification, throttling, and cancellation without logging sensitive URLs unnecessarily.

## Remaining tasks

### Product and API research

- [ ] Verify documented Raindrop health fields and endpoint semantics.
- [ ] Decide metadata-only, active, or combined mode.
- [ ] Define user-visible scope and whether Trash is ever included.
- [ ] Define typed outcomes and freshness semantics.

### Security design

- [ ] Complete an SSRF and DNS-rebinding threat model.
- [ ] Select a connection-time IP enforcement approach compatible with `HttpClient`.
- [ ] Define allowed schemes, ports, redirects, proxies, and credentials.
- [ ] Add adversarial URL and DNS test fixtures.
- [ ] Obtain security review before enabling active probes.

### Execution and Tasks

- [ ] Implement a protocol-independent, cancellable probing engine.
- [ ] Add global and per-origin schedulers, deadlines, and response limits.
- [ ] Decide synchronous thresholds versus required task execution.
- [ ] Verify task polling, cancellation, TTL, result-size, and shutdown behavior.
- [ ] Sanitize all errors and observable network details.

### Presentation and operations

- [ ] Add health as a later analytics result/dashboard version, not mutable resource state.
- [ ] Explain observation time, partial completion, and indeterminate outcomes.
- [ ] Benchmark representative libraries and tune conservative defaults.
- [ ] Add opt-in configuration and operational documentation.

## Acceptance criteria

Active probing must not ship until security-policy bypass tests, redirect revalidation, bounded scheduling, cooperative cancellation, sanitized results, host-compatible Tasks execution, and operational measurements all pass. Until then, analytics remains metadata-only.

## References

- Raindrop API: https://developer.raindrop.io/v1
- OWASP SSRF Prevention Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html
- MCP Tasks SEP-2663: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/seps/2663-tasks-extension.md
