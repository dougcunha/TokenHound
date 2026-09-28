# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T06 — Manual update check from the tray with the update dialog

## Outcome

The tray menu has "Check for Updates…" before About. Invoking it opens the update dialog, which runs a check and shows Checking, Up to date, Available (current → new version, release-notes link, **Update now**, **Later**, **Skip this version**), Rate limited (with resume time), or Error. **Skip** persists the version; **Later** closes. **Update now** is present but routed to a placeholder that T08 replaces.

## Dependencies and boundaries

- Depends on: T03
- Unblocks: T07, T08
- In scope: `TrayMenuItemKey.CheckForUpdates`, `TrayMenuModel`, `TrayIconViewModel` (new action), `UpdateViewModel` (check states, Later, Skip), `UpdateWindow.xaml(.cs)`, `UpdateDialog.cs`, `App.Updates.cs` (composition of settings store, state store, gate, client, check service, dialog), call site in `App.xaml.cs`, test csproj links, TC-18 (check part), TC-20.
- Out of scope: scheduler and balloon (T07), download/apply (T08), settings tab (T09).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| US-02 | `prd.md#stories-and-journeys` | Check now from the tray |
| FR-04 | `prd.md#functional-requirements` | Tray item always shows a result |
| FR-05 | `prd.md#functional-requirements` | Prompt with Update now / Later / Skip; Skip persisted |
| FR-13 | `prd.md#functional-requirements` | Failure shown, no duplicate checks |
| UX-1, UX-2, UX-3 | `prd.md#user-experience` | Tray entry, prompt content, specific messages |
| DEC-12, DEC-13 | `techspec.md#technical-decisions` | Single dialog, partial App file |
| CMP-16..CMP-19 | `techspec.md#components-and-flow` | App components |
| TC-18, TC-20 | `techspec.md#test-approach` | View model and tray tests |
| MA-1 | `techspec.md#test-approach` | Manual check script |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (no `ConfigureAwait(false)` in UI contexts); `docs/design/` is not needed (dialog, not HUD).
- Existing code: `src/TokenHound.App/UI/Tray/{TrayMenuItemKey,TrayMenuModel,TrayIconViewModel}.cs` (`Invoke` 80-112), `src/TokenHound.App/UI/Windows/ProviderStatusDialog.cs` (activate-or-create), `UI/Styles/DialogResources.xaml`, `App.Mcp.cs` (partial precedent), `App.xaml.cs:44-72` and `391-419`; tests `tests/TokenHound.Infrastructure.Tests/Tray/*` and csproj `Compile Include` links (41-72).
- Contract or integration: `techspec.md#errors-security-and-recovery` (messages per error).

## Work

- [x] T06.1 Tray: add `CheckForUpdates` key and "Check for Updates…" entry before About; `TrayIconViewModel` receives a check-for-updates action and dispatches it from `Invoke`; update tray tests (entry count/order).
- [x] T06.2 `UpdateViewModel`: states and commands (Check, Later, Skip, Update now placeholder, OpenReleaseNotes); busy flag prevents re-entry; persists skip through a delegate to `UpdateSettingsStore`.
- [x] T06.3 `UpdateWindow` + `UpdateDialog` (activate-or-create, owner = notch window).
- [x] T06.4 `App.Updates.cs`: build the update services, wire the tray action to open the dialog and start a check; one call in `OnStartup`/`InitializeTray`.
- [x] T06.5 Link the new App view model into the test project; tests TC-18 (check states, Later, Skip) and TC-20.

## Acceptance criteria

- The tray menu shows "Check for Updates…" immediately before "About…"; invoking it opens the dialog (or activates the open one) and starts a check.
- Up to date, available, rate limited (with local resume time), and error each render a distinct message; none closes the app.
- A second check request while one runs does not start another.
- **Skip this version** writes `Update.SkippedVersion` and closes; **Later** closes without writing.
- `App.xaml.cs` gains only call sites; the wiring lives in `App.Updates.cs`.

## Verification

- Unit: TC-18 (check part), TC-20.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-1 at the visual check (launch via Windows MCP `App` tool per `AGENTS.md`).
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: network access to api.github.com for MA-1 only.
- Expected evidence: passing tray and `UpdateViewModelTests`; app builds.

## Affected files

- Modify: `src/TokenHound.App/UI/Tray/{TrayMenuItemKey,TrayMenuModel,TrayIconViewModel}.cs`, `src/TokenHound.App/App.xaml.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/Tray/{TrayMenuModelTests,TrayIconViewModelTests}.cs`.
- Create: `src/TokenHound.App/App.Updates.cs`, `src/TokenHound.App/ViewModels/UpdateViewModel.cs`, `src/TokenHound.App/UI/Windows/{UpdateWindow.xaml,UpdateWindow.xaml.cs,UpdateDialog.cs}`, `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateViewModelTests.cs`.

## Observability and recovery

- Operational signal: `Tray action {Action} invoked` for `CheckForUpdates`, plus T03 check logs.
- Recovery: removing the tray entry and the partial file restores the previous app.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Tray entry `CheckForUpdates` ("Check for Updates…") between Settings and About; `TrayIconViewModel.CheckForUpdates()` logs `Tray action {Action} invoked` and calls the `init` property `CheckForUpdatesAction` (null = log only; property instead of a 5th constructor parameter to avoid aggravating QA-08). `UpdateViewModel` renders Checking, UpToDate, Available, RateLimited (local resume time `HH:mm`, date added when not today), and Error (Failed and Unavailable with distinct copy and the outcome `Reason` as detail); a busy flag makes a second `CheckAsync`/`StartCheck` a no-op while one runs; **Later** raises `CloseRequested` without writing; **Skip this version** saves `LatestVersion.ToString()` (the form `UpdatePolicy.ResolveSkip` parses) through `SaveSkippedVersionAsync` and closes, or stays open with an error when saving fails or throws; **Update now** hands the release to `UpdateNow` (App placeholder `OpenReleasePage` opens the HTTPS release page until T08); dispose cancels a pending check without an error state. Mapping choices: a manual check that gets `Skipped` still offers the version (Available state, "You chose to skip this version earlier"); `Unavailable` renders as Error with `UNAVAILABLE` copy. `UpdateDialog` is activate-or-create (owner = notch window) and returns the live view model so `App.ShowUpdateDialog` starts a check on both paths. `App.Updates.cs` composes `UpdateSettingsStore`, `UpdateStateStore`, `UpdateRequestGate`, `GitHubReleaseClient`, `UpdateCheckService` (disposables tracked); `App.xaml.cs` gains only the `InitializeUpdates(disposableResources)` call and the `CheckForUpdatesAction = ShowUpdateDialog` initializer.
- Changed files: modified `src/TokenHound.App/UI/Tray/{TrayMenuItemKey,TrayMenuModel,TrayIconViewModel}.cs`, `src/TokenHound.App/App.xaml.cs` (+4 lines), `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (4 links), `tests/TokenHound.Infrastructure.Tests/Tray/{TrayMenuModelTests,TrayIconViewModelTests}.cs`; created `src/TokenHound.App/App.Updates.cs`, `src/TokenHound.App/ViewModels/{UpdateViewModel,UpdateDialogState,UpdateDialogDependencies,UpdateMessages}.cs`, `src/TokenHound.App/UI/Windows/{UpdateWindow.xaml,UpdateWindow.xaml.cs,UpdateDialog.cs}`, `tests/TokenHound.Infrastructure.Tests/ViewModels/{UpdateViewModelTests,UpdateViewModelActionTests,UpdateViewModelFixtures}.cs`. The state enum, dependency record, and copy live in their own files (not in the planned list) to keep `UpdateViewModel` under the 300-line rule and one type per file; the tests are split for the same reason.
- Checks: `rtk dotnet build TokenHound.slnx --no-restore` → 7 projects, 0 errors, 0 warnings; filtered `*UpdateViewModel*` + `*Tray*` → 41 passed (TC-18 check part, TC-20; `TrayIconHostTests` unchanged and passing); full Infrastructure suite → 900 passed; Core suite → 161 passed. Quality profile over the T06 files: QA-01..QA-05 empty; QA-07 not applicable (App/UI files omit `ConfigureAwait`); QA-06 largest `App.xaml.cs` 453 (baseline 449, +4 call-site lines under DEC-13), others ≤ 295; QA-08 hits `TrayIconViewModel.cs:30` and `App.xaml.cs:367` are pre-existing lines, `UpdateViewModelFixtures.cs:15` is a `DateTimeOffset` value constructor (not a parameter list).
- Validated state: base `a8bd1bf` + T01..T05 + the files above; Debug, net10.0 / net10.0-windows.
- Open items: MA-1 (manual, at the visual check). The update window is closed by `Application.Shutdown` as an owned window; `DialogService.CloseAll` does not know it (DialogService is 274 lines) — T08, which owns shutdown during apply, should confirm the close path. `UpdateDialogState.Downloading`/`Applying` exist for T08 and are not rendered yet.

### ADR candidates

None - direct TechSpec implementation or local decision.
