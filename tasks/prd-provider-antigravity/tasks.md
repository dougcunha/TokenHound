# Implementation plan — Provider Adapter: Google Antigravity / Gemini

## Stable sources

- PRD: [tasks/prd-provider-antigravity/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-antigravity/prd.md)
- TechSpec: [tasks/prd-provider-antigravity/techspec.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-antigravity/techspec.md)

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Antigravity Process & Ephemeral TCP Port Discovery | — | T02, T04 |
| T02 | Antigravity Language Server HTTPS Client | T01 | T04 |
| T03 | Antigravity Local Transcript Reader | — | T04, T05 |
| T04 | Antigravity Usage Provider Adapter (IUsageProvider) | T01, T02, T03 | — |
| T05 | Antigravity Activity Monitor (IActivityMonitor) | T03 | — |

## Tasks

- [T01 — Antigravity Process & Ephemeral TCP Port Discovery](task_01.md): WMI process discovery for language_server.exe CSRF token and iphlpapi TCP table port lookup.
- [T02 — Antigravity Language Server HTTPS Client](task_02.md): Queries RetrieveUserQuotaSummary, bypassing localhost SSL and inverting remainingFraction.
- [T03 — Antigravity Local Transcript Reader](task_03.md): Aggregates MODEL turns from transcript.jsonl for today's local date.
- [T04 — Antigravity Usage Provider Adapter](task_04.md): Implements IUsageProvider coordinating Language Server RPC and Transcript fallback.
- [T05 — Antigravity Activity Monitor](task_05.md): Implements IActivityMonitor checking 45s threshold on transcript.jsonl.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
