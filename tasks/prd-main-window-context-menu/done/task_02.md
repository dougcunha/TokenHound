# Stable execution context

Load in this order: [prd.md](prd.md), [techspec.md](techspec.md), then this file. Reuse unchanged sources already read. Consult [tasks.md](tasks.md) for authoritative dependencies/state.

# T02: Provide an empty application-styled Settings dialog

## Outcome

A reusable DialogService can open, reactivate, dismiss, and reopen the empty Settings window on the WPF dispatcher.

## Dependencies and boundaries

- Depends on: None.
- Unblocks: T03.
- In scope: SettingsWindow, shared dialog resources, owned modeless lifecycle, work-area placement, and keyboard dismissal.
- Out of scope: Settings fields/persistence, About, HUD menu wiring, custom chrome, new dependencies or branding.
- Implementation authorization: planning does not authorize execution; see manifest state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| PRD and TechSpec IDs | PRD requirements/stories/outcomes; TechSpec decisions/components/test approach | FR-06/FR-08, NFR-01 through NFR-03, US-03; DEC-05/DEC-07; CMP-04/CMP-05/CMP-08; TC-05/TC-07. |

## Context to recover on demand

- Applicable skills: sdd-execute-task for execution; repository-cli-efficiency before searches/diffs; dotnet-efficient-validation and MTP reference before .NET validation; no-workarounds for lifecycle/error fixes. Follow applicable UI skills when implementing visuals.
- Existing code: recover only the affected files below and their immediate callers/tests. Preserve unrelated worktree changes.
- Contracts: TechSpec Contracts and data, Interfaces/errors/recovery, and Test approach are authoritative. Keep Core free of WPF/OS dependencies; borrowed credentials are read-only.
- Validation: manifest V01/V02 defines environment and build prerequisites; no task changes runner or package versions.

## Work

- [x] T02.1 Create SettingsWindow and shared resources using the current app baseline and the TechSpec's standard chrome decision; merge resources once in App.xaml.
- [x] T02.2 Implement the Settings portion of DialogService: assign HUD owner, Show modelessly, activate an existing instance, and clear its reference on Closed.
- [x] T02.3 Implement Escape/close behavior and initial work-area placement. Add WindowPlacement only if required for the stated monitor/DPI contract; keep OS dependencies in App.
- [x] T02.4 Build the WPF project and inspect resource resolution, focus states, ownership, and lifecycle. Carry manual TC-05/TC-07 to T05; do not create a temporary user-facing menu or claim a click-through before T04 wires the entry point.

## Acceptance criteria

- Settings has only its title, empty body, and dismissal controls; it never writes preferences or shuts down the app.
- Repeated service calls use one live Settings instance; closing permits a new instance.
- The dialog can activate while the HUD retains its existing non-activation styles.

## Verification

- Unit: No additional implementation-mirroring unit tests required; use the build/source and manual evidence specified here.
- Integration: preserve real boundaries; P01 owns durable storage design/evidence. Fakes establish coordination only, not provider or filesystem semantics.
- E2E: omitted by desktop .NET policy, including local full-application automation.
- Manual: Owner: Windows reviewer in T05, MAN-02/MAN-03. Full user-path acceptance remains pending until menu integration; this task's result is the buildable dialog/service slice.
- Environment dependency: Windows/.NET 10.0.400-compatible SDK and matching Release outputs; see V01/V02. No new external-service authority is inferred.
- Expected evidence: commands and exit codes, executed/failed/skipped counts when tests run, build/source revision, and named manual results. Listing/zero tests is not a pass.

Commands: apply V01 for the App build; T05 reuses valid checks and uses V03 only if a single-file artifact is needed.

## Affected files

Create `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, its `.xaml.cs`, `src/TokenHound.App/UI/Windows/DialogService.cs`, and `src/TokenHound.App/UI/Styles/DialogResources.xaml`. Modify `src/TokenHound.App/App.xaml`. Conditional create: `src/TokenHound.App/Interop/WindowPlacement.cs`.

## Observability and recovery

- Operational signal: A visible Settings window with no configuration controls; failures must not be swallowed.
- Recovery: revert only this delivery's changes using normal version control after assessing dependent tasks; never delete credentials or provider state. Invalidate evidence only for affected source/build changes.
- Re-entry: inspect current task state and existing files before creating anything; preserve IDs, handoffs, and completed work. Report collisions rather than overwrite them.

## Handoff

> Updated by sdd-execute-task during implementation.

- Produced result: Modeless Settings dialog with standard chrome, dark theme styling matching the HUD capsule baseline, empty content area, and accessible dismissal (Close button and Escape key). DialogService manages single-instance modeless lifecycle, owner assignment, reactivation/restoration, and Closed cleanup without invoking Application.Shutdown. WindowPlacement interop helper provides Win32 monitor work-area detection with DPI scaling, clamps dialog placement within the monitor work area, and enables DWM dark mode title bar.
- Changed files:
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml`
  - `src/TokenHound.App/App.xaml`
  - `src/TokenHound.App/Interop/WindowPlacement.cs`
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`
  - `src/TokenHound.App/UI/Windows/DialogService.cs`
  - `tasks/prd-main-window-context-menu/task_02.md`
- Checks:
  - `rtk dotnet restore src/TokenHound.App/TokenHound.App.csproj --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` (exit 0, 252 passed, 0 failed, 0 skipped)
- Validated state: Clean Release build with zero errors. Visual and modeless lifecycle constraints verified via code structure and resource dictionaries. TC-05 and TC-07 manual verification carried forward to T05.
- Open items: None for T02. Modeless dialog entry point will be wired in T04.

### ADR candidates

None - direct TechSpec implementation of DEC-05, DEC-07, CMP-04, CMP-05, and CMP-08.

