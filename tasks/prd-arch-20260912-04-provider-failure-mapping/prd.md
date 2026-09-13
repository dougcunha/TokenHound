# PRD — Refactoring shared rate-limit snapshots and failure mapping

## Context and motivation

The 2026-09-12 analysis (AA-08, AA-10) found the per-provider failure path re-implemented across the codebase. Cursor and Antigravity carry byte-for-byte identical `CreateRateLimitedSnapshot` bodies, Claude repeats the same algorithm with one variation, and seven status→snapshot switches (`MapHttpFailure`, `MapCloudCodeFailure`, `MapBillingFailure`, `MapFailure`) re-encode the same decision with drifting semantics. A retry-deadline or failure-classification change currently requires parallel edits and can silently diverge.

## Scope

- Target: rate-limited snapshot construction and HTTP/exception→snapshot mapping in the provider layer.
- Allowed structural change: introduce one shared rate-limited snapshot builder and one shared failure mapper; providers delegate. Every provider's observable outcome is preserved.
- Out of scope: HTTP failure **exception** construction (`arch-20260912-03`); exception-hierarchy unification (AA-11, pending); `UsageStore` decomposition (`arch-20260912-09`).

## Behaviors to preserve

| ID | Observable behavior | Source and evidence | Verification |
| --- | --- | --- | --- |
| R-01 | Cursor 429 produces a `RateLimited` snapshot with `Fidelity.Official`, empty `LimitWindows`, and an `ActiveBlock` deadline from `RateLimitPolicy.CalculateDeadline`. | `CursorUsageProvider.cs:217-246` | `CursorUsageProviderTests.Failures` |
| R-02 | Antigravity 429 produces the same shape; Antigravity 403 keeps returning `null` (fall-through) until a decision says otherwise. | `AntigravityUsageProvider.Snapshots.cs:136-174` | `AntigravityUsageProviderTests.Failures` |
| R-03 | Claude 429 produces the same shape but preserves `_lastSuccessfulSnapshot?.LimitWindows`. | `ClaudeOAuthProvider.cs:225-261` | `ClaudeOAuthProviderTests` |
| R-04 | OpenCode and Copilot keep their current (explicit-deadline) rate-limit snapshots and failure reasons. | `OpenCodeUsageProvider.cs:136-150,284`; `CopilotUsageProvider.Snapshot.cs:34,76`; `CopilotBillingService.cs:153`; `CopilotHistoricalReportCollector.cs:275` | provider + billing tests |
| R-05 | The persisted `ActiveBlock.ResetTimeUtc` / `RetryAfterSeconds` values are unchanged for equal inputs. | `RateLimitPolicy.CalculateDeadline` consumers | store/refresh tests |

## Constraints

- The shared mapping must let each provider express its genuine differences via an explicit hook, not by copy-pasting the switch.
- The minimum retry floor and zero-`Retry-After` protection in `RateLimitPolicy` must be preserved.
- No change to persisted state format.

## Acceptance criteria

- [x] All `R-NN` items were checked after refactoring. Evidence: focused MTP classes passed with `--minimum-expected-tests 1` — `*CursorUsageProviderTests*` 9 (R-01), `*AntigravityUsageProviderTests*` 11 (R-02), `*ClaudeOAuthProviderTests*` 13 (R-03), `*OpenCodeUsageProviderTests*` 18 and `*CopilotUsageProviderTests*` 11 plus `*CopilotBillingServiceTests*` 6 and `*CopilotBillingServiceHistoricalTests*` 4 (R-04), `*RateLimitPolicyTests*` 12 (R-05 policy); R-05 store coverage is included in the full project run.
- [x] No new behavior entered the scope silently. Evidence: full `TokenHound.Infrastructure.Tests` run passed 675 tests with `--minimum-expected-tests 1` (exit 0), and the provider failure classes assert the preserved statuses, reasons, and deadlines.
- [x] Provider failure tests pass with `--minimum-expected-tests 1`. Evidence: every focused MTP class run above and the full Infrastructure project run used the guard and exited 0.
- [x] Duplicate snapshot/mapping bodies are eliminated. Evidence: QA-01 `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` returned 0 hits (`rg` exit 1); corrected QA-02 `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` returned 2 thin delegates against baseline 4 (target ≤ 2).

## Assumptions and open items

- Open item: whether Antigravity's `403 → null` is intentional (it differs from Cursor/OpenCode). Until decided, this refactoring preserves `403 → null`; changing it would make that item defect-oriented and re-route.
