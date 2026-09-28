# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Update settings, persisted update state, and the GitHub rate-limit gate

## Outcome

The `Update` section of `settings.json` loads and saves with defaults and clamping, `update-state.json` persists `lastCheckUtc`, `deadlineUtc`, and `consecutiveFailures` atomically, and `UpdateRequestGate` refuses dispatch before a persisted deadline and records 429 / 403-limit responses with the `RateLimitPolicy` floor.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T03, T09
- In scope: CMP-05 (`UpdateSettings`, `UpdateSettingsStore`, `UserSettings.Update`), CMP-06 (`UpdateStateStore`), CMP-07 (`UpdateRequestGate`), tests TC-07, TC-08.
- Out of scope: HTTP calls (T03), scheduler (T07), settings UI (T09).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | Interval persisted in JSON, default 24 h, 0/disabled |
| FR-05 | `prd.md#functional-requirements` | `SkippedVersion` persisted |
| FR-12 | `prd.md#functional-requirements` | Persisted deadline, no dispatch before it, `Retry-After: 0` floored |
| NFR-03 | `prd.md#non-functional-requirements` | Network budget via persisted last check |
| NFR-06 | `prd.md#non-functional-requirements` | `System.Text.Json` like existing stores |
| DEC-03, DEC-04 | `techspec.md#technical-decisions` | Settings shape, gate behavior |
| CMP-05..CMP-07 | `techspec.md#components-and-flow` | Components |
| TC-07, TC-08 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (429 invariant, JSON config, `ConfigureAwait(false)` in Infrastructure).
- Existing code: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs:93-99` and `RefreshSettings.cs` (section store and resolve pattern); `UserSettings.cs:11-39`; `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs` (gate pattern, persistence-failure block); `Engine/CopilotHttpArchive.cs` (conservative deadline read); `Engine/AtomicJsonFile.cs`; `src/TokenHound.Core/Policies/RateLimitPolicy.cs:58-109`.
- Contract or integration: `techspec.md#contracts-and-data` (`Update` section, `update-state.json`).

## Work

- [x] T02.1 `UpdateSettings` record (`Enabled`, `CheckIntervalHours`, `SkippedVersion`, resolved properties with defaults and 0..720 clamping) and `UpdateSettingsStore` over `SectionStore` with section name `Update`; add `UserSettings.Update`.
- [x] T02.2 `UpdateStateStore` reading/writing `update-state.json` in the TokenHound LocalAppData directory (path injectable for tests) through `AtomicJsonFile`; unreadable file → empty state; parseable deadline kept even when other fields are corrupt.
- [x] T02.3 `UpdateRequestGate`: `CanDispatch`, `ActiveDeadlineUtc`, `RecordRateLimitAsync(retryAfterSeconds, resetUtc)`, `RecordSuccessAsync`, deadline = max(policy deadline, reset), persistence failure → process-blocked; `TimeProvider` injected.
- [x] T02.4 Tests TC-07 and TC-08 in `tests/TokenHound.Infrastructure.Tests/{Configuration,Updates}/`.

## Acceptance criteria

- Missing `Update` section → `Enabled=true`, interval 24 h, no skipped version; saving `Update` leaves the other sections byte-for-byte equivalent in content.
- `CheckIntervalHours` 0 → periodic disabled; negative clamps to 0; above 720 clamps to 720.
- After a 429 with `Retry-After: 120`, `CanDispatch` is false until the deadline, and a new gate built from the same file is also blocked.
- `Retry-After: 0` produces a deadline at or above the `RateLimitPolicy` floor, never "now".
- 403 with `X-RateLimit-Remaining: 0` and a reset 30 min ahead → deadline ≥ reset.
- A failure to persist the deadline blocks the gate for the process and surfaces an exception.

## Verification

- Unit: TC-07, TC-08 with temp directories and `ManualTimeProvider`.
- Integration: file round-trip in a temp directory (real `AtomicJsonFile`).
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: passing test run with `UpdateSettingsStoreTests`, `UpdateStateStoreTests`, `UpdateRequestGateTests`.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`.
- Create: `src/TokenHound.Infrastructure/Configuration/{UpdateSettings,UpdateSettingsStore}.cs`, `src/TokenHound.Infrastructure/Updates/{UpdateState,UpdateStateStore,UpdateRequestGate}.cs`, matching tests.

## Observability and recovery

- Operational signal: `UpdateRateLimited {DeadlineUtc} {Failures}` log on recorded limits.
- Recovery: deleting `update-state.json` clears the deadline (manual, documented in the log message only).

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `UpdateSettings` (`Enabled`, `CheckIntervalHours`, `SkippedVersion`; `IsEnabled` defaults to true, `IntervalHours` defaults to 24 and clamps to 0..720) with `UpdateSettingsStore` over `SectionStore` section `Update`; `UserSettings.Update`; `UserSettingsFile.MergeWithDefaults` now carries `Update` (without it the section was dropped on load — found by TC-07). `UpdateState` + `UpdateStateStore` (`update-state.json`, atomic writes, each field read independently, unreadable file → empty state). `UpdateRequestGate`: `CanDispatch`, `ActiveDeadlineUtc`, `LastCheckUtc`, `ConsecutiveFailures`, `IsProcessBlocked`, `RecordRateLimitAsync(retryAfter, resetUtc)` (deadline = max(policy deadline, reset, active deadline), persisted; persistence failure → `UpdateStatePersistenceException` and process block), `RecordSuccessAsync` (clears deadline/streak, stores last check; persistence best effort with a warning log). Logs `UpdateRateLimited {DeadlineUtc} {Failures}`.
- Changed files: modified `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`, `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs` (one line, not listed in the task's affected files but required by CMP-05); created `src/TokenHound.Infrastructure/Configuration/{UpdateSettings,UpdateSettingsStore}.cs`, `src/TokenHound.Infrastructure/Updates/{UpdateState,UpdateStateStore,UpdateStatePersistenceException,UpdateRequestGate}.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/UpdateSettingsStoreTests.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/UpdateRequestGateTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings; filtered `UpdateSettingsStoreTests` + `UpdateRequestGateTests` → 16 passed (TC-07, TC-08); full `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` → 849 passed. Quality profile over the touched files: QA-01..QA-05, QA-07, QA-08 empty; largest file 189 lines.
- Validated state: base `a8bd1bf` + T01 + the files above; Core, Infrastructure, Infrastructure.Tests, Debug, net10.0.
- Open items: saving any section rewrites `Refresh` with its computed `ActiveInterval`/`IdleInterval` properties (pre-existing `RefreshSettings` serialization, not changed here); the sibling-section test therefore checks values instead of byte equality. No reservation hits.

### ADR candidates

None - direct TechSpec implementation or local decision.
