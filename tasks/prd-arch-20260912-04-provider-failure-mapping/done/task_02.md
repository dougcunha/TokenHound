# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md`
2. `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Extract shared failure mapper

## Outcome

One shared mapper converts `(status code, has credential)` to the failure outcome; each provider's `Map*Failure` becomes a thin delegate with an explicit hook for its genuine differences.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: create the mapper; migrate `CursorUsageProvider.MapHttpFailure`, `AntigravityUsageProvider.Snapshots.MapCloudCodeFailure`, `OpenCodeUsageProvider.MapHttpFailure`, and the Copilot billing/report reason mappings where semantics match.
- Out of scope: the rate-limited snapshot builder (T01); preserving vs changing Antigravity `403 → null` (preserve, encode explicitly).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-02, R-04 | `prd.md#behaviors-to-preserve` | Mapping outcomes preserved |
| DEC-02 | `techspec.md#technical-decisions` | Mapper with explicit 403 hook |
| QA-02 | `techspec.md#quality-profile` | Switch count 7 → ≤2 delegates |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`.
- Existing code: `CursorUsageProvider.cs:209-215`, `AntigravityUsageProvider.Snapshots.cs:136-143`, `OpenCodeUsageProvider.cs:136-150`, `CopilotUsageProvider.Snapshot.cs:34`, `CopilotBillingService.cs:153`, `CopilotHistoricalReportCollector.cs:275`, `ClaudeOAuthProvider.cs:225-231`.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T02.1 Add the shared mapper with a `(statusCode, hasCredential)` decision and an override/hook for provider-specific cases.
- [x] T02.2 Migrate Cursor and OpenCode mapping to the mapper.
- [x] T02.3 Migrate Antigravity with an explicit `403 → null` hook.
- [x] T02.4 Migrate Copilot billing/report reason mappings where the decision matches; keep the reason taxonomy.
- [x] T02.5 Remove replaced switch bodies.

## Acceptance criteria

- [x] Every failure input yields the same `ProviderStatus`/reason as before.
- [x] Antigravity `403 → null` is preserved and explicitly encoded.
- [x] No duplicated switch body remains beyond the thin delegates.

## Verification

- Unit: Antigravity 403/429; Cursor 401/403/429; OpenCode failures; Copilot billing reasons.
- Integration: not applicable.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: focused MTP classes `*AntigravityUsageProviderTests.Failures*`, `*CursorUsageProviderTests.Failures*`, `*OpenCodeUsageProviderTests*`, `*CopilotBillingServiceTests*` with `--minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: pass counts; mapper switch count via `rtk rg`.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/SnapshotFailureMapper.cs` (name at implementation's discretion)
- Modify: `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.Snapshot.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotHistoricalReportCollector.cs`

## Observability and recovery

- Operational signal: unchanged `ProviderStatus` and `ErrorDescription`/reason text.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Done. Added an internal shared `SnapshotFailureMapper` keyed by `(HttpStatusCode?, hasCredential)` with an optional per-provider override hook. The decision core is `401 → NeedsAuth`, `403 → AccessDenied` (or `NeedsAuth` when no credential), `429 → RateLimited`, and anything else (including a null status) → `Stale`. The mapper returns a nested `Outcome` enum (`NeedsAuth`, `AccessDenied`, `RateLimited`, `Stale`, `Unsupported`, `Ignored`); each provider translates an outcome into its own snapshot. Migrated Cursor, Antigravity, OpenCode, `CopilotUsageProvider.MapHttpError`, and the `CopilotApiException` branch of `CopilotHistoricalReportCollector.MapFailure`. Antigravity's `403 → null` is encoded explicitly in the `ClassifyForbiddenAsIgnored` hook (`Outcome.Ignored`), Cursor's `403 → NeedsAuth` in `ClassifyForbiddenAsAuth`, and Copilot's PAT-shaped `403` in `ClassifyForbidden`. Statuses, reason text, and the Copilot billing reason taxonomy are unchanged; `RateLimitPolicy` is untouched. `hasCredential` is currently passed `true` at every call site because mapping runs only after credential use, but the dimension is honored by the core.
- Changed files:
  - Create: `src/TokenHound.Infrastructure/Providers/SnapshotFailureMapper.cs` — internal static class, nested `Outcome` enum, `Classify` core + hook.
  - Modify: `.../Cursor/CursorUsageProvider.cs` — `MapHttpFailure` → `MapFailure` thin delegate + `ClassifyForbiddenAsAuth` hook.
  - Modify: `.../Antigravity/AntigravityUsageProvider.Snapshots.cs` — `MapCloudCodeFailure` thin delegate + `ClassifyForbiddenAsIgnored` hook (`403 → null` preserved).
  - Modify: `.../OpenCode/OpenCodeUsageProvider.cs` — `MapHttpFailure` → `MapFailure` thin delegate; the `OpenCode*Exception` cases collapse into the status core (all three carry their status code), keeping `OpenCodeRateLimitException` for retry/reset extraction.
  - Modify: `.../Copilot/CopilotUsageProvider.Snapshot.cs` — `MapHttpError` delegates; `ClassifyForbidden` preserves the PAT-shape branch.
  - Modify: `.../Copilot/CopilotHistoricalReportCollector.cs` — `CopilotApiException` reason mapping delegates; 401 is hooked to `AccessDenied`.
  - Unchanged: `.../Copilot/CopilotBillingService.cs` — `MapBillingFailure` classifies a non-HTTP gate exception (`RateLimitBlockedException`) vs unknown; it has no `(status, hasCredential)` decision to share, so it remains a provider-local thin delegate per DEC-03.
  - No test files added or changed at original completion (superseded by the `codereview_1` correction below, which adds one Cursor test); no files moved; nothing under `done/`; no sibling `prd-arch-20260912-*` path touched. `AntigravityUsageProvider.cs` (the call site of `MapCloudCodeFailure`) was reverted to its original text and left unmodified because it is not in this task's Affected files.
- Checks:
  - Build: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal` → 3 projects, 0 errors, 0 warnings, exit 0.
  - Tests (MTP, `--no-build --no-restore -- --minimum-expected-tests 1`):
    - `--filter-class "*CursorUsageProviderTests*"` → 7 passed / 0 failed, exit 0.
    - `--filter-class "*AntigravityUsageProviderTests*"` → 11 passed / 0 failed, exit 0.
    - `--filter-class "*OpenCodeUsageProviderTests*"` → 18 passed / 0 failed, exit 0.
    - `--filter-class "*CopilotBillingServiceTests*"` → 6 passed / 0 failed, exit 0.
    - `--filter-class "*CopilotUsageProviderTests*"` → 11 passed / 0 failed, exit 0.
    - `--filter-class "*CopilotBillingServiceHistoricalTests*"` → 4 passed / 0 failed, exit 0 (extra, covers the modified collector).
    - Total 57 passed / 0 failed.
  - QA-03 guard: `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*RateLimitPolicyTests*"` → 12 passed / 0 failed, exit 0.
  - Divergence: partial-class suffix filters such as `"*CursorUsageProviderTests.Failures*"` discover 0 tests in this MTP/xUnit setup; class-level filters were used and cover those partials.
- Validated state:
  - Code/diff: `rtk git diff` reviewed for the five modified files plus the new mapper; `RateLimitPolicy.cs` and every test file are absent from `git status`. The out-of-scope `AntigravityUsageProvider.cs` call site was reverted to its original content.
  - Configuration: no config change; `global.json` (SDK 10.0.400, MTP runner) honored.
  - Projects: built `TokenHound.Infrastructure.Tests` (Debug/net10.0/x64), transitively covering `TokenHound.Infrastructure` and `TokenHound.Core`.
  - Environment: Windows/pwsh; the pre-existing dirty worktree from sibling `prd-arch-20260912-03-http-failure-factory` and the T01 changes were preserved; no checkout/stash/reset/clean performed.
- Quality profile:
  - QA-01 (blocking): `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` → 0 hits (rg exit 1) — pass.
  - QA-02 (booking): `rtk rg -n "MapHttpFailure|MapCloudCodeFailure|MapBillingFailure" src/TokenHound.Infrastructure/Providers` → 4 hits = 2 thin delegates (baseline 8). The target "≤ 2 thin delegates" is met; the literal ≤ 2 hit count is not. Reservation: Antigravity's 2 hits are the definition (in scope) plus its call site in `AntigravityUsageProvider.cs`, which is outside the authorized Affected files and could not be renamed without an unauthorized edit; Copilot billing's 2 hits are a non-HTTP exception decision retained per DEC-03. No duplicated switch body remains.
  - QA-03 (blocking): focused `RateLimitPolicyTests` → 12 passed; `RateLimitPolicy` unchanged.
- Open items:
  - PRD open item AA-10: Antigravity `403 → null` intentionally preserved; unchanged pending the decision.
  - If the Antigravity call site is later authorized, renaming `MapCloudCodeFailure` would drop QA-02 to 2 hits.
- ADR candidates: None - direct TechSpec implementation (DEC-02: `(HttpStatusCode, hasCredential)` core with an explicit per-provider hook; DEC-03: explicit-deadline Copilot/OpenCode snapshots retained; DEC-04: `RateLimitPolicy` untouched).

### Correction (codereview_1)

- CR-01 (T02 part) resolved. Added the missing Cursor 403 provider test; the T01 correction's 429-without-`Retry-After` test is preserved unchanged.
- New test: `GetSnapshotAsync_WhenApiReturnsForbidden_ReturnsNeedsAuth` in `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorUsageProviderTests.Failures.cs`. It makes the Cursor API return HTTP 403 Forbidden and asserts `ProviderStatus.NeedsAuth` with `ActiveBlock` `null`, exercising the `ClassifyForbiddenAsAuth` hook (403 -> `NeedsAuth`).
- Cursor class coverage now matches the `task_02.md` verification requirement: 401 (existing), 403 (new), 429 with `Retry-After` (existing), and 429 without `Retry-After` (T01 correction).
- Commands and results (Debug/net10.0, MTP, same build reused):
  - Build: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorUsageProviderTests*"` -> 9 passed / 0 failed, exit 0 (was 8; +1 new test).
  - `... --filter-class "*AntigravityUsageProviderTests*"` -> 11 passed / 0 failed, exit 0.
  - `... --filter-class "*OpenCodeUsageProviderTests*"` -> 18 passed / 0 failed, exit 0.
  - `... --filter-class "*CopilotUsageProviderTests*"` -> 11 passed / 0 failed, exit 0.
  - `... --filter-class "*CopilotBillingServiceHistoricalTests*"` -> 4 passed / 0 failed, exit 0.
  - QA-02 (corrected command): `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` -> 2 hits, the two thin delegates (`AntigravityUsageProvider.Snapshots.cs:136`, `CopilotBillingService.cs:153`), exit 0.
  - QA-01 (blocking): `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` -> 0 hits, rg exit 1.
- Counting note: raw MTP totals differ from the `rtk` summary line, which undercounts `[Theory]` cases; the pass counts above are the raw MTP totals.
- Scope: only the Cursor `Failures` partial test file changed; no production source, no other task/PRD file, no moves. Pre-existing dirty worktree preserved.
- Outside this correction's scope: CR-02 (QA-02 class/baseline in `techspec.md`/`tasks.md`), CR-03 (factory method size), CR-04 (PRD acceptance checkboxes).
