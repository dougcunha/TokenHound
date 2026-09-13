# Implementation plan — Refactoring shared rate-limit snapshots and failure mapping

## Stable sources

- PRD: `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md`
- TechSpec: `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Shared rate-limited snapshot builder; Cursor, Antigravity, and Claude delegate | — | T02 |
| T02 | Shared failure mapper; provider switches delegate with explicit hooks | T01 | — |
| T03 | Repair the QA-02 quality gate (CR-02) | — | T04 |
| T04 | Reconcile the PRD acceptance state (CR-04) | T01, T02, T03 | — |

> Correction cycle after `codereview_1` (REJECTED): T01 and T02 were reopened for CR-01 and CR-03; T03 and T04 carry the plan-artifact findings CR-02 and CR-04.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| R-01 | `prd.md#behaviors-to-preserve` | Cursor 429 snapshot unchanged | T01 | `CursorUsageProviderTests.Failures` |
| R-02 | `prd.md#behaviors-to-preserve` | Antigravity 429/403 unchanged | T01, T02 | `AntigravityUsageProviderTests.Failures` |
| R-03 | `prd.md#behaviors-to-preserve` | Claude windows preserved | T01 | `ClaudeOAuthProviderTests` |
| R-04 | `prd.md#behaviors-to-preserve` | OpenCode/Copilot outcomes unchanged | T02 | provider/billing tests |
| R-05 | `prd.md#behaviors-to-preserve` | Persisted deadline unchanged | T01, T02 | store/refresh tests |
| DEC-01..DEC-04 | `techspec.md#technical-decisions` | Builder/mapper/hook decisions | T01, T02 | code review |
| QA-01..QA-03 | `techspec.md#quality-profile` | Duplicate counts reduce; floor preserved | T01, T02 | scoped `rtk rg`, `RateLimitPolicy` tests |
| TC-01..TC-04 | `techspec.md#safety-net` | Provider tests | T01, T02 | MTP commands |
| CR-01 | `codereview_1/codereview.md#findings` | Cursor 403 and 429-without-`Retry-After` coverage | T01, T02 | `CursorUsageProviderTests` |
| CR-02 | `codereview_1/codereview.md#findings` | QA-02 valid class, reproducible baseline/target | T03 | scoped `rg` / `git grep` |
| CR-03 | `codereview_1/codereview.md#findings` | Factory method ≤ 30 lines | T01 | code review |
| CR-04 | `codereview_1/codereview.md#findings` | PRD acceptance agrees with evidence | T04 | `prd.md` + handoffs |

## Tasks

- [T01 — Extract shared rate-limited snapshot builder](done/task_01.md): one builder serves Cursor, Antigravity, and Claude. Reopened for CR-01 (Cursor 429-without-`Retry-After` coverage) and CR-03 (factory method size); both corrected.
- [T02 — Extract shared failure mapper](done/task_02.md): provider switches delegate to one decision core with explicit hooks. Reopened for CR-01 (Cursor 403 coverage); corrected.
- [T03 — Repair the QA-02 quality gate](done/task_03.md): valid class, reproducible command/baseline/target.
- [T04 — Reconcile the PRD acceptance state](done/task_04.md): acceptance boxes agree with post-correction evidence.

## Coverage gate

- Coverage: pass — AA-08 and AA-10 map to T01 and T02.
- Traceability: pass — R-01..R-05, DEC-01..04, QA-01..03, TC-01..04 mapped.
- Dependencies: pass — T01 → T02; no cycles.
- Atomicity: pass — each task one reviewable result.
- Executability: pass — MTP commands recorded.
- Validation profile: pass — E2E omitted for .NET desktop.
- Idempotency: pass.

## Assumptions and open items

- Open item: Antigravity `403 → null` intentionality (AA-10 sub-item). Until decided, T02 preserves it and encodes it explicitly.
- Open item: AA-11 exception hierarchy stays out of scope.
- Required environment: none.

## State

- [x] T01 — completed (reopened for CR-01/CR-03, corrected)
- [x] T02 — completed (reopened for CR-01, corrected)
- [x] T03 — completed
- [x] T04 — completed

## Problems and solutions

- T01: Added `RateLimitedSnapshotFactory.Create` (internal, returns `(Snapshot, ConsecutiveCount)`) and delegated Cursor, Antigravity, and Claude to it; Claude uses `TimeProvider.System` and the optional windows parameter. Build 0 errors/0 warnings; focused Cursor 7/7, Antigravity 11/11, Claude 13/13, `RateLimitPolicyTests` 12/12 passed. QA-01 blocking 2 → 0 hits; QA-03 blocking pass; `RateLimitPolicy` unchanged. MTP `--filter-class "*CursorUsageProviderTests.Failures*"` discovers 0 tests (partial merged into the declarer class); class-level filter used instead under `--minimum-expected-tests 1`. QA-02 booking baseline was understated in the TechSpec: actual current hits are 8, not 7; T01 introduced none, so no aggravating hit.
- T02: Added `SnapshotFailureMapper.Classify` (internal, `(HttpStatusCode?, hasCredential)` core + optional override hook) and migrated Cursor, Antigravity (`403 → null` encoded explicitly via hook), OpenCode (custom 401/403/429 exceptions carry their status, so type branches collapse to the shared core), `CopilotUsageProvider.MapHttpError`, and the `CopilotApiException` reason mapping in `CopilotHistoricalReportCollector`. `CopilotBillingService.MapBillingFailure` stayed provider-local because it classifies a non-HTTP gate exception (`RateLimitBlockedException`) — covered by DEC-03. `RateLimitPolicy` unchanged. Focused provider tests 57/57; QA-01 blocking 0 hits; QA-03 blocking pass. QA-02 booking: 8 → 4 hits (2 thin delegates): Antigravity's definition is in scope but its call site lives in the out-of-scope file `AntigravityUsageProvider.cs`, and Copilot billing is the DEC-03 non-HTTP decision; no duplicated switch body remains, so this is a recorded reservation, not a blocking hit. Integrated run of the full `TokenHound.Infrastructure.Tests` project passed 673/673.
- Review `codereview_1` (`codereview_1/codereview.md`) returned REJECTED after T01/T02 were marked complete. Findings: CR-01 (Cursor 403 and 429-without-`Retry-After` provider tests missing), CR-02 (`techspec.md` QA-02 class `booking` invalid, baseline 7 conflicts with the observed 8, target non-reproducible over a definition+call-site command), CR-03 (`RateLimitedSnapshotFactory.Create` spans 40 lines against the 30-line limit), CR-04 (`prd.md` acceptance unchecked while `tasks.md` marked T01/T02 complete). Reopened T01 and T02 through the DAG owner; added T03 (CR-02) and T04 (CR-04). Previous handoffs superseded: T01 claimed the factory plus Cursor/Antigravity/Claude delegation with focused tests 7/11/13 and QA-01 zero, but produced no 429-without-`Retry-After` test and left the method over-long; T02 claimed the mapper with Cursor/Antigravity/OpenCode/Copilot delegation and 57 focused tests, but produced no Cursor 403 test. Valid evidence from both prior handoffs is preserved in the task files' handoff history and remains reusable where the code state is unchanged. Correction scope: T01 fixes CR-03 + CR-01 (Cursor-429) within its existing contract; T02 fixes CR-01 (Cursor-403) within its existing contract; T03 repairs the quality profile; T04 reconciles the PRD acceptance state.
- Correction results: T01 split `RateLimitedSnapshotFactory.Create` (26-55, 30 lines) and added `GetSnapshotAsync_WhenApiReturnsRateLimitedWithoutRetryAfter_ReturnsFutureDeadline` (Cursor 9/9). T02 added `GetSnapshotAsync_WhenApiReturnsForbidden_ReturnsNeedsAuth` (Cursor 9/9; Antigravity 11, OpenCode 18, Copilot usage 11/historical 4 all pass). T03 replaced QA-02 with class `reservation`, the pipe-free command, verified baseline 4 (HEAD) and current 2. T04 re-ran the post-correction evidence and checked the four `prd.md` acceptance items with inline evidence: focused classes 9/11/13/18/11/6/4, `RateLimitPolicyTests` 12, full `TokenHound.Infrastructure.Tests` 675 passed, QA-01 0, QA-02 2 (baseline 4, target ≤2). All blocking findings addressed; feature returned for re-review.

