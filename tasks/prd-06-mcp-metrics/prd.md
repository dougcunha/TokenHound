# PRD — MCP access to live provider metrics

## Problem and context

TokenHound already collects provider usage and status for its desktop HUD. A user who works in an MCP-capable client cannot query those same current readings without looking at the HUD. The feature exposes the metrics for real providers currently shown by TokenHound through a local MCP server while the desktop application is running.

"Live" means each MCP request reads TokenHound's current in-memory readings. Their age is determined by the existing provider refresh schedule and reported timestamps. An MCP request does not promise a fresh upstream provider fetch.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | An MCP client can inspect the provider metrics currently available to the HUD. | A client connects to the documented local endpoint and receives the same current provider readings, status, and timestamps that underpin the HUD. |
| OBJ-02 | Exposing metrics does not change provider polling or reveal credentials. | Repeated MCP reads leave upstream request counts unchanged and responses contain no credential material. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer using an MCP client | Ask for the current metrics of visible providers | Inspect quota and availability without switching to the HUD | Client connects while TokenHound is running and requests all current metrics. |
| US-02 | Developer investigating one provider | Ask for one provider's current reading | Distinguish available, stale, blocked, and missing data | Client selects a provider and sees its status, reading time, and available metrics. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | TokenHound hosts an MCP endpoint over local HTTP while the desktop application is running, using `ModelContextProtocol.AspNetCore`. | An MCP client connects to a documented, stable local URL; the endpoint stops when the application exits. |
| FR-02 | The endpoint supports HTTP with Server-Sent Events transport. | A client configured for SSE can establish a stream and invoke the metrics capability over the corresponding HTTP message endpoint. |
| FR-03 | An MCP client can request metrics for all real providers currently represented in the HUD. | The response includes enabled providers with available snapshots and excludes disabled providers. A synthetic mock fallback is never presented as a real provider reading. |
| FR-04 | An MCP client can request the metrics of one provider by its stable provider identifier. | A known visible provider returns its current reading; an unknown, disabled, or not-yet-read provider has an explicit, distinguishable result. |
| FR-05 | Each provider result communicates the meaning and age of its data. | The result includes provider identifier, status, fidelity, UTC reading time, available quota windows and reset times, and applicable block or error state. Missing values remain absent or `null`. |
| FR-06 | Metrics already represented by the HUD outside quota windows are available when present. | Copilot billing and Cline account or local usage data represented in HUD rows appear for those providers, without inventing values for other providers. |
| FR-07 | MCP reads use current TokenHound state. | A reading changed by the existing refresh pipeline appears on the next MCP request; invoking MCP does not itself dispatch a provider refresh or alter the rate-limit schedule. |
| FR-08 | The user can configure an MCP client from repository documentation. | Documentation gives the endpoint URL, SSE connection mode, capability names, response meaning, and a working example of client setup. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Local exposure | The server listens only on loopback and rejects unexpected host or browser origin values; no remote network interface is exposed by default. |
| NFR-02 | Data privacy | MCP output contains usage and operational data only, with no access tokens, cookies, credential paths, raw provider responses, or private session content. |
| NFR-03 | Correctness | Unknown denominators and percentages remain `null`; a count of used units is never reported as remaining units; stale, rate-limited, unauthenticated, and unavailable readings are not reported as current successes. |
| NFR-04 | Availability | An MCP startup or client connection failure is observable to the user or in application diagnostics and does not prevent the existing HUD from operating. |
| NFR-05 | Lifecycle | MCP reads and shutdown tolerate concurrent provider updates and terminate active connections when the application closes. |

## Constraints and dependencies

- The server uses the `ModelContextProtocol.AspNetCore` library and HTTP/SSE transport requested for this feature.
- `TokenHound.Core` remains free of UI and OS dependencies.
- Provider refresh, persistence, credential ownership, and rate-limit behavior remain governed by existing TokenHound policies.
- The official C# SDK documents SSE as a legacy, stateful transport that must be enabled explicitly; its HTTP endpoint can coexist with SSE. See [SDK transport guidance](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md).

## Out of scope

- Remote or internet-facing hosting, multi-user access, and account authentication for MCP.
- MCP commands that refresh providers, change settings, or write data.
- A continuous subscription that pushes every provider update to clients; clients query the latest reading when needed.
- Changing provider adapters, polling cadence, or HUD presentation. Exception (workflow `DEC-23`, acceptance finding `ACC-01`): the Antigravity adapter stops storing its used-request count as remaining units, and the HUD reads that count from the corrected field. No HUD text or polling changes.

## Assumptions and sources

- Product assumption for approval: "HTTP / SSE" requires compatibility with clients that use the legacy SSE endpoint, even though the SDK recommends Streamable HTTP for new clients. The implementation may expose both when the SDK maps both transports.
- Product assumption for approval: "real time" means querying the latest reading held by TokenHound, with its timestamp and status; it does not mean forcing an upstream refresh or pushing change notifications.
- Product assumption for approval: the mock HUD fallback is synthetic demonstration data and must not be returned as genuine provider telemetry.
- Internal source: `src/TokenHound.Infrastructure/Engine/UsageStore.cs` keeps current snapshots and emits updates; `src/TokenHound.App/ViewModels/NotchViewModel.cs` applies provider enablement to HUD rings.
- Internal source: `src/TokenHound.Core/Models/Snapshot.cs` and `LimitWindow.cs` define status, fidelity, timestamps, and nullable quota values; `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` adds Copilot and Cline rows.
- Internal source: `docs/specs/01-READING-STRATEGY-RESILIENCE.md` defines provider refresh and stale-data behavior; `docs/specs/README.md` documents the no-fabricated-values and credential-ownership principles.
- External source: [official C# SDK transport guidance](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md) describes HTTP, SSE, and local host protection.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
