# Implementation plan — Provider Adapter: Cursor

## Stable sources

- PRD: [tasks/prd-provider-cursor/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-cursor/prd.md)
- TechSpec: [tasks/prd-provider-cursor/techspec.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-cursor/techspec.md)

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Cursor SQLite Session & Credential Discovery | — | T02, T04 |
| T02 | Cursor Api Client with Workos Session Cookie | T01 | T04 |
| T03 | Cursor Composer Header Reader & State Tracker | — | T04, T05 |
| T04 | Cursor Usage Provider Adapter (IUsageProvider) | T01, T02, T03 | — |
| T05 | Cursor Activity Monitor (IActivityMonitor) | T03 | — |

## Tasks

- [T01 — Cursor SQLite Session & Credential Discovery](task_01.md): Queries state.vscdb ItemTable for auth keys.
- [T02 — Cursor Api Client with Workos Session Cookie](task_02.md): GET https://cursor.com/api/usage-summary with WorkosCursorSessionToken.
- [T03 — Cursor Composer Header Reader & State Tracker](task_03.md): Queries composerHeaders for active/pending runs.
- [T04 — Cursor Usage Provider Adapter](task_04.md): Implements IUsageProvider mapping totalPercentUsed.
- [T05 — Cursor Activity Monitor](task_05.md): Implements IActivityMonitor with ProcessLiveness on Cursor.exe.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
