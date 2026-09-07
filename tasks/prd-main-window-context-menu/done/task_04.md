# Stable execution context

Load in this order: [prd.md](prd.md), [techspec.md](techspec.md), then this file. Reuse unchanged sources already read. Consult [tasks.md](tasks.md) for authoritative dependencies/state.

# T04: Deliver the four-action HUD context menu

## Outcome

The visible HUD exposes Close, Refresh, Settings, and About with working actions, honest refresh feedback, and coordinated shutdown.

## Dependencies and boundaries

- Depends on: T01 and T03.
- Unblocks: T05.
- In scope: App composition, HudActionsViewModel, ApplicationLifetime, complete menu wiring, refresh popup, removal of production mock injection, and action tests.
- Out of scope: Persistence prerequisite P01, new counters/providers, global shortcuts, mock-provider removal from test utilities, production error harnesses, broad adapter changes.
- Implementation authorization: planning does not authorize execution; see manifest state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| PRD and TechSpec IDs | PRD requirements/stories/outcomes; TechSpec decisions/components/test approach | FR-01 through FR-08, NFR-01 through NFR-05, US-01 through US-04, OBJ-01 through OBJ-03; DEC-01 through DEC-08; CMP-01/CMP-02/CMP-03/CMP-04/CMP-08/CMP-10; TC-01 through TC-08/TC-10; GAP-01. |

## Context to recover on demand

- Applicable skills: sdd-execute-task for execution; repository-cli-efficiency before searches/diffs; dotnet-efficient-validation and MTP reference before .NET validation; no-workarounds for lifecycle/error fixes. Follow applicable UI skills when implementing visuals.
- Existing code: recover only the affected files below and their immediate callers/tests. Preserve unrelated worktree changes.
- Contracts: TechSpec Contracts and data, Interfaces/errors/recovery, and Test approach are authoritative. Keep Core free of WPF/OS dependencies; borrowed credentials are read-only.
- Validation: manifest V01/V02 defines environment and build prerequisites; no task changes runner or package versions.

## Work

- [x] T04.1 Compose the completed DialogService and engine lifecycle in App. Track startup/manual work, stop/drain before disposing owned resources, and call Application.Shutdown on the dispatcher. Keep OnExit cleanup idempotent.
- [x] T04.2 Implement HudActionsViewModel with guarded awaited refresh, terminal CloseAsync, dialog callbacks, observable state, and concise status messages as specified.
- [x] T04.3 Attach the complete four-item ContextMenu to the visible capsule, explicitly resolve the detached menu data context, and preserve Rings binding and left-button drag behavior.
- [x] T04.4 Show progress/completion/error feedback in the non-activating popup outside measured HUD layout; disable only Refresh while pending. Keep agent IsBusy semantics unchanged.
- [x] T04.5 Remove automatic production MockUsageProvider injection and keep test use explicit. Verify empty/unavailable/partial outcomes without inferring success from old snapshots.
- [x] T04.6 Add focused action tests and extend source links. Inspect credential/error-text flow and registered provider disposal/cancellation. Build and run affected checks; prepare the exact manual paths for T05.

## Acceptance criteria

- All four exact labels dispatch their real actions; there are no temporary dead entries.
- Repeated refresh requests produce one pending UI refresh, Close remains responsive, and no new work is admitted after closing.
- Missing credentials do not activate mock production counters; unknown values stay unknown.
- Existing WM_MOUSEACTIVATE/MA_NOACTIVATE and SWP_NOZORDER behavior remains intact; popup feedback does not resize/reposition the HUD.
- Unit/source checks pass; UI and durable-rate-limit acceptance are explicitly delegated to T05/P01, not claimed as passed here.

## Verification

- Unit: HudActionsViewModelTests, UsageStoreLifecycleTests, NotchViewModelTests, ProviderRingViewModelTests, covering the scenarios above.
- Integration: preserve real boundaries; P01 owns durable storage design/evidence. Fakes establish coordination only, not provider or filesystem semantics.
- E2E: omitted by desktop .NET policy, including local full-application automation.
- Manual: Owner: implementing developer/Windows reviewer in T05; MAN-01 through MAN-04. Do not automate desktop E2E.
- Environment dependency: Windows/.NET 10.0.400-compatible SDK and matching Release outputs; see V01/V02. No new external-service authority is inferred.
- Expected evidence: commands and exit codes, executed/failed/skipped counts when tests run, build/source revision, and named manual results. Listing/zero tests is not a pass.

After V01/V02 prerequisites:

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreLifecycleTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderRingViewModelTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

## Affected files

Modify `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, its `.xaml.cs`, and `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`. Create `src/TokenHound.App/ApplicationLifetime.cs`, `src/TokenHound.App/ViewModels/HudActionsViewModel.cs`, and `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs`. Extend `src/TokenHound.App/UI/Styles/DialogResources.xaml` for menu/popup states. Existing ViewModel tests are regression consumers.

## Observability and recovery

- Operational signal: Refresh status and provider details expose current progress and failure; Close terminates the process after coordinated cleanup.
- Recovery: revert only this delivery's changes using normal version control after assessing dependent tasks; never delete credentials or provider state. Invalidate evidence only for affected source/build changes.
- Re-entry: inspect current task state and existing files before creating anything; preserve IDs, handoffs, and completed work. Report collisions rather than overwrite them.

## Handoff

- Produced result: Delivered the four-action HUD context menu (Close, Refresh, Settings, About) with `HudActionsViewModel`, `ApplicationLifetime` shutdown and drain coordinator, non-activating refresh feedback popup outside HUD capsule layout, removal of production `MockUsageProvider` injection (GAP-01/DEC-08), and 9 new unit tests.
- Changed files:
  - `src/TokenHound.App/App.xaml.cs` (modified: composed lifetime, dialog service, hud actions viewmodel, removed production mock provider injection)
  - `src/TokenHound.App/ApplicationLifetime.cs` (created: coordinated cancellation, usage store drain, dialog closure, provider disposal, dispatcher shutdown)
  - `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` (created: guarded refresh, terminal close, dialog callbacks, status formulation)
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml` (modified: added HudContextMenuStyle and HudMenuItemStyle)
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml` (modified: 4-item ContextMenu on CapsuleBorder, non-activating StatusPopup)
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` (modified: hooked actions, popup visibility, menu item IsEnabled, preserved non-activation and DragMove)
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs` (created: 9 unit tests covering concurrency, state transitions, status formulation, shutdown cancellation)
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (modified: linked HudActionsViewModel.cs)
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit code: 0)
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit code: 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"` (exit code: 0, 9 passed, 0 failed, 0 skipped)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreLifecycleTests*"` (exit code: 0, 8 passed, 0 failed, 0 skipped)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"` (exit code: 0, 6 passed, 0 failed, 0 skipped)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderRingViewModelTests*"` (exit code: 0, 14 passed, 0 failed, 0 skipped)
  - Full `TokenHound.Infrastructure.Tests` suite: 271 passed, 0 failed, 0 skipped.
- Validated state: Clean Release builds and 100% passing tests across all affected classes and the complete suite. HUD context menu and status popup correctly wired; production composition free of mock fallback; left-click dragging and non-activating window styles intact.
- Open items: Manual acceptance checks (MAN-01 through MAN-04) and release-readiness verification delegated to T05; durable 429 rate-limit persistence pending external prerequisite P01 / GAP-02.

### ADR candidates

None - direct TechSpec implementation (DEC-01 through DEC-03, DEC-08).

