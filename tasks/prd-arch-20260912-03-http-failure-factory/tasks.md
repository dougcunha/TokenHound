# Implementation plan — Refactoring shared HTTP failure construction

## Stable sources

- PRD: `tasks/prd-arch-20260912-03-http-failure-factory/prd.md`
- TechSpec: `tasks/prd-arch-20260912-03-http-failure-factory/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Shared `ProviderHttpException.FromResponse`; Cursor/Antigravity duplicate constructors removed | — | T02 |
| T02 | Retry-After pass-through deleted; four Copilot call sites use `HttpRetryAfterParser` directly | — | — |

External dependency: `arch-20260912-01-dead-exports` must land before T02 (shared `CopilotApiClient.cs`).

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| R-01 | `prd.md#behaviors-to-preserve` | Cursor failure unchanged | T01 | `CursorApiClientTests` |
| R-02 | `prd.md#behaviors-to-preserve` | Antigravity failure unchanged | T01 | `AntigravityCloudCodeClientTests` |
| R-03 | `prd.md#behaviors-to-preserve` | Retry-After parse order unchanged | T01, T02 | parser/provider tests |
| R-04 | `prd.md#behaviors-to-preserve` | Copilot call sites parse identically | T02 | `Copilot*Tests` |
| R-05 | `prd.md#behaviors-to-preserve` | Only 429 parses Retry-After | T01 | focused tests |
| DEC-01..DEC-04 | `techspec.md#technical-decisions` | Factory + retargets | T01, T02 | code review |
| QA-01..QA-03 | `techspec.md#quality-profile` | Duplicate/pass-through counts reach 0 | T01, T02 | scoped `rtk rg` |
| TC-01..TC-03 | `techspec.md#safety-net` | Provider tests | T01, T02 | MTP commands |

## Tasks

- [T01 — Add shared failure factory; migrate Cursor and Antigravity](done/task_01.md): one `FromResponse` factory replaces two duplicate constructors.
- [T02 — Delete Retry-After pass-through; retarget Copilot call sites](done/task_02.md): the wrapper disappears and four sites call the shared parser.

## Coverage gate

- Coverage: pass — AA-07 and AA-14 map to T01 and T02.
- Traceability: pass — R-01..R-05, DEC-01..04, QA-01..03, TC-01..03 mapped.
- Dependencies: pass — T02 requires T01 external predecessor WS01; no cycles.
- Atomicity: pass — each task produces one reviewable result.
- Executability: pass — commands recorded.
- Validation profile: pass — E2E omitted for .NET desktop; MTP runner recorded.
- Idempotency: pass — retargeting is idempotent.

## Assumptions and open items

- Assumption: `TimeProvider.System` remains the clock at these call sites.
- Open item: AA-11 exception-hierarchy unification stays out of scope.
- Required environment: none.

## State

- [x] T01 — completed
- [x] T02 — completed

## Problems and solutions

- T01: Added `ProviderHttpException.FromResponse(HttpResponseMessage, string)` (429-only Retry-After via `HttpRetryAfterParser.ExtractSeconds(..., TimeProvider.System)`) and migrated Cursor/Antigravity with message text passed verbatim; both private `CreateException` bodies removed. QA-01 2 → 0 hits; focused Cursor 4/4 and Antigravity 6/6 passed; orchestrator review additionally ran `*AntigravityUsageProviderTests*` 11/11 (covers the 429 Retry-After integration path). No blocking hits.
- T02: Deleted `CopilotRateLimitExtractor.ExtractRetryAfterSeconds` (kept `TryExtractRateLimit`); retargeted the four call sites to `HttpRetryAfterParser.ExtractSeconds` with the same time providers (`_timeProvider` in the gate, `TimeProvider.System` elsewhere); added the parent-namespace using to the four call sites and removed it from the extractor. Build 0 errors/0 warnings; focused Copilot 40/40 passed; QA-02 5 → 0 hits, QA-03 1 → 1. No blocking hits.
