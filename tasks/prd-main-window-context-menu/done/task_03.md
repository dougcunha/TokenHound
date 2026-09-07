# Stable execution context

Load in this order: [prd.md](prd.md), [techspec.md](techspec.md), then this file. Reuse unchanged sources already read. Consult [tasks.md](tasks.md) for authoritative dependencies/state.

# T03: Show application information and the running build version

## Outcome

About displays the real product description and full build version through the shared dialog lifecycle.

## Dependencies and boundaries

- Depends on: T02 (shared resources and DialogService).
- Unblocks: T04.
- In scope: AboutWindow, immutable ApplicationInfo metadata reader, service integration, and source-linked metadata tests.
- Out of scope: Update checks, links, licensing panels, versioning-policy changes, new packages, and release publication.
- Implementation authorization: planning does not authorize execution; see manifest state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| PRD and TechSpec IDs | PRD requirements/stories/outcomes; TechSpec decisions/components/test approach | FR-07/FR-08, NFR-01 through NFR-03, US-04, OBJ-03; DEC-05 through DEC-07; CMP-04/CMP-06/CMP-07/CMP-10; TC-06/TC-07. |

## Context to recover on demand

- Applicable skills: sdd-execute-task for execution; repository-cli-efficiency before searches/diffs; dotnet-efficient-validation and MTP reference before .NET validation; no-workarounds for lifecycle/error fixes. Follow applicable UI skills when implementing visuals.
- Existing code: recover only the affected files below and their immediate callers/tests. Preserve unrelated worktree changes.
- Contracts: TechSpec Contracts and data, Interfaces/errors/recovery, and Test approach are authoritative. Keep Core free of WPF/OS dependencies; borrowed credentials are read-only.
- Validation: manifest V01/V02 defines environment and build prerequisites; no task changes runner or package versions.

## Work

- [x] T03.1 Create ApplicationInfo with the TechSpec assembly-injection contract; read informational metadata from the App assembly supplied by the caller, retain prerelease/build information, and use the explicit unavailable state when missing.
- [x] T03.2 Create AboutWindow with TokenHound, the PRD description, readable version text, and shared style/focus/dismissal behavior.
- [x] T03.3 Extend DialogService with one independently owned About instance; ensure Settings and About can coexist and reactivate separately.
- [x] T03.4 Link the WPF-independent metadata source into Infrastructure.Tests using its established pattern. Add tests for explicit assembly selection, full version, missing metadata, and long metadata display input.

## Acceptance criteria

- About contains only the specified information and dismissal controls; no hard-coded release version is presented.
- Tests exercise metadata from an explicitly supplied assembly rather than accidentally accepting the test host's version.
- About shares Settings lifecycle and appearance, with long version text allowed to wrap.

## Verification

- Unit: ApplicationInfoTests, covering the scenarios above.
- Integration: preserve real boundaries; P01 owns durable storage design/evidence. Fakes establish coordination only, not provider or filesystem semantics.
- E2E: omitted by desktop .NET policy, including local full-application automation.
- Manual: Owner: Windows reviewer in T05, MAN-02/MAN-03/MAN-05. Compare a normal and single-file build; unit metadata fixtures are not packaging evidence.
- Environment dependency: Windows/.NET 10.0.400-compatible SDK and matching Release outputs; see V01/V02. No new external-service authority is inferred.
- Expected evidence: commands and exit codes, executed/failed/skipped counts when tests run, build/source revision, and named manual results. Listing/zero tests is not a pass.

After V01/V02 prerequisites:

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ApplicationInfoTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

## Affected files

Create `src/TokenHound.App/Presentation/ApplicationInfo.cs`, `src/TokenHound.App/UI/Windows/AboutWindow.xaml`, its `.xaml.cs`, and `tests/TokenHound.Infrastructure.Tests/Presentation/ApplicationInfoTests.cs`. Modify `src/TokenHound.App/UI/Windows/DialogService.cs` and `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`.

## Observability and recovery

- Operational signal: The visible version is genuine build metadata; Version unavailable is not a release pass.
- Recovery: revert only this delivery's changes using normal version control after assessing dependent tasks; never delete credentials or provider state. Invalidate evidence only for affected source/build changes.
- Re-entry: inspect current task state and existing files before creating anything; preserve IDs, handoffs, and completed work. Report collisions rather than overwrite them.

## Handoff

> Updated by sdd-execute-task during implementation.

- Produced result:
  - `ApplicationInfo` immutable metadata reader with assembly-injection contract (`FromAssembly(Assembly?)`, `Get(Assembly?)`, `Current`), reading `AssemblyInformationalVersionAttribute` or falling back to valid assembly version, returning explicit "Version unavailable" if missing or unversioned default. Zero WPF dependencies.
  - `AboutWindow` modeless dialog with title "About TokenHound", application name, PRD description ("TokenHound monitors LLM usage, rate limits, and agent activity across AI coding tools on your Windows desktop."), readable version text with `TextWrapping="Wrap"` in a styled card, and accessible dismissal (Close button with `IsCancel="True"` and Escape key handler; never calls `Application.Shutdown`). Integrates Win32 dark mode via `WindowPlacement.EnableDarkMode`.
  - `DialogService` extended with `ShowAbout(Window? owner = null)`, `CloseAbout()`, `IsAboutOpen`, and updated `CloseAll()`. Settings and About coexist modelessly and can be activated or restored independently without cross-interference.
  - `TokenHound.Infrastructure.Tests.csproj` updated to link `ApplicationInfo.cs`.
  - Comprehensive unit test suite in `ApplicationInfoTests.cs` covering explicit assembly injection, full informational version with prerelease and build metadata, fallback to `AssemblyVersion`, unavailable version fallback for unversioned/null metadata, whitespace trimming, long version preservation, product name/description constants, and `ApplicationInfo.Current`.
- Changed files:
  - `src/TokenHound.App/Presentation/ApplicationInfo.cs`
  - `src/TokenHound.App/UI/Windows/AboutWindow.xaml`
  - `src/TokenHound.App/UI/Windows/AboutWindow.xaml.cs`
  - `src/TokenHound.App/UI/Windows/DialogService.cs`
  - `tests/TokenHound.Infrastructure.Tests/Presentation/ApplicationInfoTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - `tasks/prd-main-window-context-menu/task_03.md`
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ApplicationInfoTests*"` (exit 0, 10 passed, 0 failed, 0 skipped)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` (exit 0, 262 passed, 0 failed, 0 skipped)
- Validated state: Clean Release build with zero errors. All 10 ApplicationInfoTests passed. Full test suite (262 tests) passed. Manual verification (TC-06, TC-07, MAN-02, MAN-03, MAN-05) remains scheduled for integrated verification in T05.
- Open items: None for T03. About action wiring into the HUD context menu will be completed in T04.

### ADR candidates

None - direct TechSpec implementation of DEC-05, DEC-06, DEC-07, CMP-04, CMP-06, CMP-07, and CMP-10.

