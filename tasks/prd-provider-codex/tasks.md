# Implementation plan — Provider Adapter: OpenAI Codex

## Stable sources

- PRD: [tasks/prd-provider-codex/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-codex/prd.md)
- TechSpec: [tasks/prd-provider-codex/techspec.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-codex/techspec.md)

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Codex Auth & JWT Account Discovery | — | T04 |
| T02 | Codex AppServer JSON-RPC Client | — | T04 |
| T03 | Codex Rollout Log & SQLite Tail Reader | — | T04, T05 |
| T04 | Codex Usage Provider Adapter (IUsageProvider) | T01, T02, T03 | — |
| T05 | Codex Activity Monitor (IActivityMonitor) | T03 | — |

## Tasks

- [T01 — Codex Auth & JWT Account Discovery](task_01.md): Discovers and parses auth.json, decoding JWT ID token for email and plan type.
- [T02 — Codex AppServer JSON-RPC Client](task_02.md): Implements stdio JSON-RPC 2.0 client communicating with `codex app-server`.
- [T03 — Codex Rollout Log & SQLite Tail Reader](task_03.md): Indexes active rollout JSONL files and extracts tail rate_limits.
- [T04 — Codex Usage Provider Adapter](task_04.md): Implements IUsageProvider mapping primary (5h) and secondary (weekly) windows with 2-stage fallback.
- [T05 — Codex Activity Monitor](task_05.md): Implements IActivityMonitor polling LastWriteTimeUtc on active rollout file within 8s window.

## State

- [ ] T01 — pending
- [ ] T02 — pending
- [ ] T03 — pending
- [ ] T04 — pending
- [ ] T05 — pending
