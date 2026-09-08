# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Gate background polling and activity monitoring per provider in `UsageStore`

## Outcome

`UsageStore` owns provider enablement. A disabled provider is skipped by `TickAsync`, `RefreshNowAsync`, and activity polling, so no network request, IPC command, SQLite query, or credential file read is issued for it. Re-enabling raises an event and dispatches an immediate refresh for that provider alone, still honoring any unexpired rate-limit deadline.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T04, T05, T07
- In scope: the gating registry, the guards at the three existing poll sites, the enablement event, `RefreshProviderNowAsync`, `RegisteredProviderIds`, and clearing activity state on disable.
- Out of scope: persistence (T01), view models (T04, T05), startup wiring (T07). Nothing calls `SetProviderEnabled` yet, so default behavior is unchanged: every provider stays enabled.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-04 | `prd.md#functional-requirements` | Skip disabled providers on periodic ticks and manual refresh; no I/O of any kind; re-enable queues an immediate refresh |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Toggling off halts background polling |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Enablement takes effect with no restart and no Apply step |
| NFR-04 | `prd.md#non-functional-requirements` | Zero measurable polling overhead for disabled providers |
| NFR-06 | `prd.md#non-functional-requirements` | Gating eliminates credential reads; re-enable honors unexpired `RateLimitGate` deadlines |
| DEC-03 | `techspec.md#technical-decisions` | Registry in a new `UsageStore.Gating.cs` partial; guards at `RefreshCoreAsync`, `PollActivityAsync`, `CheckAnyBusyAsync` |
| DEC-09 | `techspec.md#technical-decisions` | Clear `_activityStates` on disable; keep the snapshot |
| CMP-05, CMP-06 | `techspec.md#components-and-flow` | Gating partial and the modified refresh/activity partials |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs` — `RefreshCoreAsync` is the single path behind both `TickAsync` and `RefreshNowAsync`; `RefreshProviderAsync` already opens with the `IsRateLimited` guard that must keep running on the re-enable path
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs` — `PollActivityAsync`, `CheckAnyBusyAsync`, `_activityLock`, and `HasBusyActivity`, which feeds `RefreshSchedulePolicy.ShouldRefresh`
- Existing code: `src/TokenHound.Infrastructure/Engine/ProviderActivityChangedEventArgs.cs` — the event-args shape to mirror
- Existing code: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs` — NSubstitute `IUsageProvider` setup and the `TestContext.Current.CancellationToken` convention
- Contract or integration: `techspec.md#contracts-and-data` — the `UsageStore` public surface table

## Work

- [x] T02.1 Create `ProviderEnablementChangedEventArgs` carrying `ProviderId` and `IsEnabled`.
- [x] T02.2 Create the `UsageStore.Gating.cs` partial: `ConcurrentDictionary<string, bool>` registry where an absent key means enabled, plus `IsProviderEnabled`, `SetProviderEnabled`, `RegisteredProviderIds`, and the `ProviderEnablementChanged` event raised only on an actual change.
- [x] T02.3 Add `RefreshProviderNowAsync(providerId, ct)` routing through the unchanged `RefreshProviderAsync` under `_refreshLock`; no-op for an unknown or disabled id.
- [x] T02.4 Skip disabled providers in `RefreshCoreAsync`.
- [x] T02.5 Skip disabled providers' monitors in `PollActivityAsync` and `CheckAnyBusyAsync`.
- [x] T02.6 On disable, clear the provider's `_activityStates` entry under `_activityLock` and raise `ActivityUpdated` with a null session; leave `CurrentSnapshots` untouched.
- [x] T02.7 Add `UsageStoreGatingTests` covering TC-05 – TC-09 and TC-15.

## Acceptance criteria

- With two registered providers and one disabled, `RefreshNowAsync` invokes `GetSnapshotAsync` on the enabled provider once and never on the disabled one; `TickAsync` behaves identically.
- A disabled provider's `IActivityMonitor.CheckLivenessAsync` is never invoked.
- `SetProviderEnabled` is idempotent and raises `ProviderEnablementChanged` only when the value actually changes; calling it for an unregistered id is a no-op.
- Re-enabling dispatches a refresh for that provider only, and dispatches nothing while a persisted 429 deadline is unexpired.
- A provider that last reported `Busy` no longer forces the active cadence once disabled.
- `UsageStore.cs` stays under the 300-line cap in `AGENTS.md`; all new members live in the new partial.
- Default behavior is unchanged for callers that never gate: `UsageStoreTests`, `UsageStoreActivityTests`, and `UsageStoreLifecycleTests` stay green.

## Verification

- Unit: TC-15 activity-state clearing, observed through the cadence decision.
- Integration: TC-05 and TC-06 gating on both refresh paths; TC-07 activity-monitor gating; TC-08 re-enable dispatch and event; TC-09 rate-limit deadline honored on re-enable. Doubles are NSubstitute `IUsageProvider`/`IActivityMonitor`, which represent the contract the engine actually calls — but note the recorded limit: they prove the *dispatch decision*, not that a real adapter opens no socket or SQLite handle. That residue is closed by MAN-05 in T07, not here.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStoreGatingTests*"`
  - Regression before handoff: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: none. No network, no credentials, no live provider.
- Expected evidence: non-zero executed test count with exit code 0; the three pre-existing `UsageStore*` test classes still passing.

## Affected files

- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Gating.cs`
- Create: `src/TokenHound.Infrastructure/Engine/ProviderEnablementChangedEventArgs.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreGatingTests.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs`

## Observability and recovery

- Operational signal: structured Serilog entry per enablement change (provider id and new state). The *absence* of `ProviderFaultLog` entries for a disabled provider is the evidence MAN-05 reads in T07.
- Recovery: the registry is in-memory only; restarting with no persisted settings restores all-enabled behavior.

## Handoff

- Produced result: `UsageStore` now owns per-provider monitoring enablement. A new `UsageStore.Gating.cs` partial holds a `ConcurrentDictionary<string, bool>` registry (absent key means enabled) plus `RegisteredProviderIds`, `IsProviderEnabled`, `SetProviderEnabled`, `RefreshProviderNowAsync`, and the `ProviderEnablementChanged` event. Guards were added at the three choke points named in DEC-03 — `RefreshCoreAsync` (covering both `TickAsync` and `RefreshNowAsync`), `PollActivityAsync`, and `CheckAnyBusyAsync` — so a disabled provider receives no `GetSnapshotAsync` and no `CheckLivenessAsync` call. `SetProviderEnabled` is idempotent, no-ops for an unregistered id, logs a structured Serilog entry, and on disable clears the provider's `_activityStates` entry under `_activityLock` and raises `ActivityUpdated` with a null session (DEC-09) while leaving `CurrentSnapshots` untouched. `RefreshProviderNowAsync` takes `_refreshLock` and routes through the unchanged `RefreshProviderAsync`, so the existing `IsRateLimited` guard still blocks a re-enabled provider whose persisted 429 deadline has not elapsed — no parallel path around the rate limiter. `UsageStore.cs` was not modified and stays at 207 lines.
- Changed files:
  - Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Gating.cs` (96 lines)
  - Create: `src/TokenHound.Infrastructure/Engine/ProviderEnablementChangedEventArgs.cs` (34 lines)
  - Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreGatingTests.cs` (286 lines, 9 tests)
  - Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs` (enablement guard in `RefreshCoreAsync`)
  - Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs` (guards in `PollActivityAsync` and `CheckAnyBusyAsync`; new `ClearActivityState`)
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings; exit 0
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStoreGatingTests*"` -> 9 tests passed; exit 0
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> 382 tests passed; exit 0 (full-project regression before handoff)
  - Pre-existing classes confirmed individually: `*UsageStoreTests*` 14 passed, `*UsageStoreActivityTests*` 2 passed, `*UsageStoreLifecycleTests*` 8 passed; all exit 0
  - Extra sanity check beyond the task commands: `rtk dotnet restore TokenHound.slnx` then `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` -> 7 projects, 0 errors, 0 warnings; exit 0. The restore was required because `TokenHound.App` and `TokenHound.Core.Tests` had no assets file in this worktree — a pre-existing environment condition, not a code failure.
- Validated state: working tree of branch `settings` with T01's files present and untouched; `Debug` configuration; `net10.0` for `TokenHound.Core` / `TokenHound.Infrastructure` / test projects and `net10.0-windows` for `TokenHound.App`; MTP runner (`xunit.v3.mtp-v2`, `UseMicrosoftTestingPlatformRunner`); no network, no credentials, no live provider used.
- Open items:
  - Test coverage: TC-05, TC-06, TC-07, TC-08, TC-09 and TC-15 are covered, plus two extra cases (idempotent no-event repeat, and the unregistered-id no-op across `SetProviderEnabled` / `IsProviderEnabled` / `RefreshProviderNowAsync` / `RegisteredProviderIds`) and one extra case gating `PollActivityAsync` through the live activity timer.
  - Recorded residue carried from the TechSpec risk register: NSubstitute doubles prove the dispatch decision, not that a real adapter opens no socket, SQLite handle, or credential file. NFR-04 / NFR-06 close through MAN-05 log inspection in T07, not here.
  - Nothing calls `SetProviderEnabled` yet, so default behavior for every existing caller is unchanged; persistence (T01) is wired to the engine only in T07.
  - E2E omitted by the .NET desktop policy in the TechSpec verification profile.

### ADR candidates

None - direct TechSpec implementation (DEC-03, DEC-09) with no deviation. Two local choices worth flagging in review but below the ADR bar:

- `ClearActivityState` raises `ActivityUpdated` with a null session unconditionally on a real disable transition, rather than only when an entry existed. This matches the literal DEC-09 wording ("clears its `_activityStates` entry and raises `ActivityUpdated` with a null session") and is safe because `SetProviderEnabled` calls it only on an actual state change.
- `IsProviderEnabled` returns `true` for an id that is not in the registry, which means a monitor registered for a provider that was never registered as an `IUsageProvider` (as in `UsageStoreActivityTests`) keeps polling. This preserves existing behavior and follows the "unknown id => true" contract in `techspec.md#contracts-and-data`; the practical consequence is that such a monitor-only provider cannot be gated, since `SetProviderEnabled` no-ops for an unregistered id.
