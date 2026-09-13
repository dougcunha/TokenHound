# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md`
2. `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Extract shared rate-limited snapshot builder

## Outcome

One shared builder constructs the rate-limited `Snapshot`; Cursor, Antigravity, and Claude delegate to it with the same observable fields as today.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: create the internal builder; migrate `CursorUsageProvider.CreateRateLimitedSnapshot`, `AntigravityUsageProvider.Snapshots.CreateRateLimitedSnapshot`, and Claude's `CreateRateLimitedSnapshot(string, int?)`.
- Out of scope: failure-mapping switches (T02); `RateLimitPolicy` itself; OpenCode/Copilot explicit-deadline builders.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01, R-03 | `prd.md#behaviors-to-preserve` | Snapshot fields and windows preserved |
| R-05 | `prd.md#behaviors-to-preserve` | Deadline values unchanged |
| DEC-01, DEC-03, DEC-04 | `techspec.md#technical-decisions` | Builder shape; policy untouched |
| QA-01 | `techspec.md#quality-profile` | Duplicate builder bodies 2 → 0 |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`.
- Existing code: `CursorUsageProvider.cs:217-246`, `AntigravityUsageProvider.Snapshots.cs:145-174`, `ClaudeOAuthProvider.cs:225-261`, `RateLimitPolicy.CalculateDeadline`.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T01.1 Add an internal builder that computes the deadline via the existing policy inputs and returns the `Snapshot` plus the updated consecutive count.
- [x] T01.2 Migrate Cursor to the builder (empty `LimitWindows`).
- [x] T01.3 Migrate Antigravity to the builder (empty `LimitWindows`).
- [x] T01.4 Migrate Claude to the builder with `_lastSuccessfulSnapshot?.LimitWindows`.
- [x] T01.5 Remove the replaced private methods and unused usings.

## Acceptance criteria

- [x] For equal inputs, `Status`, `Fidelity`, `ActiveBlock.ResetTimeUtc`, and `RetryAfterSeconds` are identical to before.
- [x] `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` returns 0 hits.
- [x] `RateLimitPolicy` is unchanged.

## Verification

- Unit: Cursor 429 with/without Retry-After; Antigravity 429; Claude 429 windows preservation.
- Integration: not applicable.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: focused MTP classes `*CursorUsageProviderTests.Failures*`, `*AntigravityUsageProviderTests.Failures*`, `*ClaudeOAuthProviderTests*` with `--minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: pass counts plus zero-hit `rg`.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/RateLimitedSnapshotFactory.cs` (name at implementation's discretion)
- Modify: `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`

## Observability and recovery

- Operational signal: unchanged `Snapshot`/`ActiveBlock` fields.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Done. One internal builder, `RateLimitedSnapshotFactory.Create`, now constructs the rate-limited `Snapshot` for Cursor, Antigravity, and Claude. The factory computes `nowUtc` from the supplied `TimeProvider`, applies `Math.Min(count + 1, max)`, calls `RateLimitPolicy.CalculateDeadline`, and returns `(Snapshot, ConsecutiveCount)`; each provider assigns the returned count back to its private `_consecutiveRateLimits`. Observable fields preserved: `Status = RateLimited`, `Fidelity = Official`, `ActiveBlock.ResetTimeUtc` from the unchanged policy, `RetryAfterSeconds` passthrough, empty `LimitWindows` for Cursor/Antigravity, and `_lastSuccessfulSnapshot?.LimitWindows` for Claude. `RateLimitPolicy` untouched.
- Changed files:
  - Create: `src/TokenHound.Infrastructure/Providers/RateLimitedSnapshotFactory.cs` (internal static, Providers namespace).
  - Modify: `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs` — `MapHttpFailure` passes `exception.Message` / `exception.RetryAfterSeconds`; private builder now delegates.
  - Modify: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs` — same delegation; `403 → null` preserved.
  - Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs` — delegation with `TimeProvider.System` (equivalent to the former `DateTimeOffset.UtcNow`) and `_lastSuccessfulSnapshot?.LimitWindows`; added `using TokenHound.Infrastructure.Providers;`.
  - No test files added or changed; no files moved; nothing under `done/` and no sibling task directory touched.
- Checks:
  - Build: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal` → 3 projects, 0 errors, 0 warnings, exit 0.
  - Tests, MTP, `--no-build --no-restore -- --minimum-expected-tests 1`:
    - `--filter-class "*CursorUsageProviderTests*"` → 7 passed / 0 failed, exit 0.
    - `--filter-class "*AntigravityUsageProviderTests*"` → 11 passed / 0 failed, exit 0.
    - `--filter-class "*ClaudeOAuthProviderTests*"` → 13 passed / 0 failed, exit 0.
    - QA-03 guard `--project tests/TokenHound.Core.Tests --filter-class "*RateLimitPolicyTests*"` → 12 passed / 0 failed, exit 0.
  - Divergence: the literal `--filter-class "*CursorUsageProviderTests.Failures*"` discovered 0 tests because the `Failures` file is a partial of the single declarer `CursorUsageProviderTests` (xUnit filter-class sees only the merged class name); it exited 9 against `--minimum-expected-tests 1`. Re-ran at class level to cover the `Failures` partial; all passed. Not a code failure.
  - QA-01: `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` → 0 hits (rg exit 1); baseline 2 → target 0 met.
- Validated state:
  - Code/diff: reviewed `rtk git diff` for the three modified files plus the new factory; net `+38 −73` across the three modifications and no unrelated edits. `RateLimitPolicy.cs` absent from `git diff --stat`.
  - Configuration: no config change; `global.json` (SDK 10.0.400, `Microsoft.Testing.Platform` runner) honored.
  - Projects: built `TokenHound.Infrastructure.Tests` (Debug/net10.0/x64), transitively covering `TokenHound.Infrastructure` and `TokenHound.Core`.
  - Environment: Windows/pwsh; pre-existing dirty worktree from the sibling feature preserved; no checkout/stash/reset/clean performed.
- Quality profile:
  - QA-01 (blocking): 0 hits — pass.
  - QA-02 (booking): `rtk rg -n "MapHttpFailure|MapCloudCodeFailure|MapBillingFailure" src/TokenHound.Infrastructure/Providers` → 8 hits vs recorded baseline 7, target ≤ 2. Reservation, not a T01 defect: T01 extracted only the builder and added no mapping hit; failure-switch delegation is T02 scope. The observed count reflects the current dirty worktree (sibling changes) and is pre-existing debt.
  - QA-03 (blocking): focused `RateLimitPolicyTests` → 12 passed; `RateLimitPolicy` unchanged.
- Open items:
  - T02 remains: shared `SnapshotFailureMapper`, failure-switch delegation, QA-02 → ≤ 2.
  - PRD open item AA-10: Antigravity `403 → null` intentionally preserved, unchanged pending a decision.
- ADR candidates: None - direct TechSpec implementation (DEC-01 shape followed: parameterized internal builder returning `(Snapshot, ConsecutiveCount)`, policy inputs passed through, DEC-03/DEC-04 respected).

### Correction (codereview_1)

> Reopened after the REJECTED review in `codereview_1/codereview.md`. Addresses only CR-03 (blocking, style) and the T01 part of CR-01 (blocking, coverage). The Cursor 403 half of CR-01 is T02 scope and was not touched.

- CR-03 (method size) — CORRECTED:
  - Change: no behavior change. Extracted two focused private methods from `RateLimitedSnapshotFactory.Create`: `CreateActiveBlock(reason, retryAfterSeconds, deadline)` builds the `UsageBlock`, and `BuildSnapshot(providerId, nowUtc, limitWindows, activeBlock)` materializes the `Snapshot`. `Create` still computes `nowUtc`, `Math.Min(count + 1, max)`, and `RateLimitPolicy.CalculateDeadline` in the same order, then returns `(snapshot, updatedCount)`.
  - Method span before: `RateLimitedSnapshotFactory.Create`, lines 26-65, 40 lines.
  - Method span after: `RateLimitedSnapshotFactory.Create`, lines 26-55, 30 lines (<= 30). `CreateActiveBlock` lines 57-67 (11 lines); `BuildSnapshot` lines 69-82 (14 lines). File is 83 lines (<= 300).
- CR-01 (T01 coverage) — CORRECTED:
  - New test: `GetSnapshotAsync_WhenApiReturnsRateLimitedWithoutRetryAfter_ReturnsFutureDeadline` in `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorUsageProviderTests.Failures.cs:70-99`.
  - It returns HTTP 429 with no `Retry-After` and asserts `ProviderStatus.RateLimited`, `ActiveBlock.IsBlocked`, `ActiveBlock.RetryAfterSeconds` is `null`, and `ActiveBlock.ResetTimeUtc >= nowUtc.AddMinutes(1)` (future policy deadline).
  - Existing 429 test with `Retry-After: 0` retained unchanged at lines 40-68. New helper `CreateRateLimitResponseWithoutRetryAfter()` at lines 158-159. No Cursor helper file was changed.
- Correction checks:
  - Build: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings, exit 0.
  - Cursor, MTP: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorUsageProviderTests*"` -> 8 passed / 0 failed, exit 0 (was 7; +1 new test).
  - QA-01: `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` -> 0 hits, `rg` exit 1.
  - QA-03: focused `*RateLimitPolicyTests*` (Core project) -> 12 passed / 0 failed, exit 0.
  - Regression from factory split: `--filter-class "*ClaudeOAuthProviderTests*"` -> 13 passed / 0 failed, exit 0; `--filter-class "*AntigravityUsageProviderTests*"` -> 11 passed / 0 failed, exit 0.
- Preserved from the prior handoff: build/test evidence above, `RateLimitPolicy` untouched, QA-01 0 hits, no files under `done/` or sibling task directories touched.
- Remaining review findings outside this correction: CR-02 (QA-02 class/baseline), CR-04 (PRD acceptance state), and the Cursor 403 portion of CR-01 remains T02 scope.
