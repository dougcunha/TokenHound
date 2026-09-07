# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. T01 is the completed dependency before execution.

---

# T02 - Add durable archive and UsageStore orchestration

## Outcome

UsageStore restores last-good readings and absolute Copilot deadlines, prevents dispatch during a persisted rate limit including forced refresh, and runs the separate two-second activity polling/event plumbing while preserving the existing 60/300-second quota cadence.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T03, T04, and T05
- In scope: TokenHound-owned JSON archive, serialized/atomic persistence, startup recovery, pre-dispatch gate, UsageStore timer/event changes, and engine tests.
- Out of scope: provider HTTP/credential implementation, Copilot file/process heuristics, WPF composition, archive expiry beyond existing policy, and borrowed tool state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-07, FR-11, FR-12 | `prd.md#functional-requirements` | Retain stale history, enforce deadlines before every dispatch, and expose activity plumbing to existing consumers. |
| NFR-01, NFR-03, NFR-04, NFR-05 | `prd.md#non-functional-requirements` | Own only TokenHound files, survive restart, preserve schema failures, and maintain timing/cadence. |
| AC-05, AC-06, AC-07, AC-10 | `prd.md#acceptance-criteria` | Restore stale data, persist 429 deadlines, schedule activity, and defer forced refresh. |
| DEC-05, DEC-06, DEC-08 | `techspec.md#technical-decisions` | Add UsageArchive, shared dispatch gate, and activity event/timer plumbing. |
| CMP-07, CMP-08 | `techspec.md#components-and-flow` | Own archive and UsageStore integration. |
| TC-06, TC-07, TC-09, TC-12 | `techspec.md#test-approach` | Prove restart/deadline, polling, and atomic-state behavior. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`, `UsageStoreLifetime.cs`, and their existing provider/activity timer ownership.
- Contract or integration: TechSpec sections `Archive data`, `Activity event`, `Status and history matrix`, and `Errors, security, and recovery`.

## Work

- [x] T02.1 Create `UsageArchive` for `%LOCALAPPDATA%/TokenHound/last_readings.json` and `state.json`, loading missing state safely and preserving unrelated provider/deadline keys.
- [x] T02.2 Serialize archive writes, use atomic replacement, retain last successful snapshots with original `FetchedAtUtc`, and never write borrowed credentials.
- [x] T02.3 Integrate startup archive loading and stale conversion into UsageStore without fabricating a quota or silently clearing a future deadline.
- [x] T02.4 Gate every provider dispatch, including forced refresh, on current and persisted absolute deadlines; persist 429 deadlines before publishing stale results.
- [x] T02.5 Add `ProviderActivityChangedEventArgs`, a separate two-second activity timer, cancellation/drain behavior, and event publication without UI work on timer threads.
- [x] T02.6 Preserve the existing 60-second busy and 300-second idle quota cadence and add engine tests with fake providers, monitors, clocks, and archive files.

## Acceptance criteria

- A successful reading is restored as Stale after restart with the original fetched timestamp; a stale/429 result retains it, while NeedsAuth/Unsupported clears it.
- Archive writes are serialized and atomic, missing archives degrade safely, unrelated provider keys survive, and a future known deadline is not erased.
- No provider call occurs before an active deadline, including a forced refresh after restart and for `Retry-After: 0`.
- Activity polling is separate from quota polling, publishes an idle/null result on monitor failure, and does not change the 60/300-second quota cadence.
- Cancellation stops both timers and disposes resources without a leaked watcher/event callback.

## Verification

- Unit: UsageArchive and UsageStore tests cover missing/malformed/partial state, atomic writes, history retention, forced-refresh gating, cancellation, and activity event plumbing.
- Integration: restart fixtures use temporary TokenHound-owned directories and a fake provider; no live endpoint is used.
- E2E: omitted by .NET desktop policy.
- Manual: None; visual activity routing is verified by T05/T06.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStore*"`.
- Environment dependency: .NET SDK 10.0.400, native MTP runner, and isolated temporary directories.
- Expected evidence: archive and UsageStore test output with non-zero count, no writes outside the TokenHound temporary fixture, and serialized deadline assertions.

## Affected files

- Create: `src/TokenHound.Infrastructure/Engine/UsageArchive.cs`
- Create: `src/TokenHound.Infrastructure/Engine/UsageArchive.Persistence.cs`
- Create: `src/TokenHound.Infrastructure/Engine/ProviderActivityChangedEventArgs.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`
- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs`
- Create: `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.Activity.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageArchiveTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreActivityTests.cs`

## Observability and recovery

- Operational signal: existing SnapshotUpdated plus the new ActivityUpdated event; archive timestamps and deadline keys are diagnostic evidence without token data.
- Recovery: remove only T02-owned TokenHound archive registration/code if needed. Do not delete or rewrite any Copilot CLI, gh, VS Code, session, or log file.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: T02 complete. TokenHound-owned snapshot/deadline archives, restart recovery, pre-dispatch deadline gating, and independent activity polling/event plumbing are implemented.
- Changed files: `src/TokenHound.Infrastructure/Engine/UsageArchive.cs`; `src/TokenHound.Infrastructure/Engine/UsageArchive.Persistence.cs`; `src/TokenHound.Infrastructure/Engine/ProviderActivityChangedEventArgs.cs`; `src/TokenHound.Infrastructure/Engine/UsageStore.cs`; `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`; `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs`; `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs`; `tests/TokenHound.Infrastructure.Tests/Engine/UsageArchiveTests.cs`; `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`; `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreActivityTests.cs`.
- Checks: Infrastructure build exit 0 with the pre-existing NU1903 and xUnit1051 advisories; current Core build/test remained green at 43 tests; current full Infrastructure test project passed with 324 tests; focused UsageStore and archive/activity runs passed; `git diff --check` passed.
- Validated state: Missing archives are safe, malformed state exposes `ArchiveErrorDescription`, writes are serialized/atomic, unrelated state keys survive, archived readings restore as stale with original timestamps, future persisted deadlines block forced refresh after restart, NeedsAuth clears archive history, and activity failures publish idle/null. Quota refresh cadence remains 60 seconds active and 300 seconds idle.
- Open items: None for T02. T03 and T04 are complete; the provider's IP-01 plaintext Copilot CLI fallback gap remains recorded in T03.

### ADR candidates

None - direct TechSpec implementation or local decision.
