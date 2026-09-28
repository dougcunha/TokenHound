# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T08 — Update now: download, apply, restart, and post-update cleanup

## Outcome

**Update now** in the dialog runs the full path: detect mode, (portable) check writability, select the asset for the process architecture, download with progress and Cancel, verify, then apply. Portable: swap and start `--updated`. Installed: launch the silent setup. Then the app shuts down gracefully. A process started with `--updated` waits for the old instance's mutex, cleans up the swap journal, and starts normally. Every failure leaves the app running with a specific message.

## Dependencies and boundaries

- Depends on: T04, T05, T06
- Unblocks: —
- In scope: `UpdateViewModel` download/apply states (Downloading with progress + Cancel, Applying, Error variants), `App.Updates.cs` apply orchestration and `--updated` startup handling and `ApplicationInstanceMutex` acquisition at startup (`OnStartup` call site), TC-18 (apply part), MA-2, MA-3.
- Out of scope: installer script changes (done in T05); settings UI (T09).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| US-03, US-04 | `prd.md#stories-and-journeys` | Portable and installed apply |
| OBJ-02 | `prd.md#outcomes-and-metrics` | One confirmation, restart on new version |
| FR-07 | `prd.md#functional-requirements` | Architecture-matched asset; missing → error |
| FR-08 | `prd.md#functional-requirements` | Nothing applied before verification |
| FR-09 | `prd.md#functional-requirements` | Portable close/swap/restart, rollback |
| FR-10 | `prd.md#functional-requirements` | Silent installer + restart |
| FR-11 | `prd.md#functional-requirements` | Not-writable → message + release link, no download |
| NFR-04 | `prd.md#non-functional-requirements` | HUD responsive during download |
| UX-3 | `prd.md#user-experience` | Specific error messages, app stays open |
| DEC-06..DEC-11 | `techspec.md#technical-decisions` | Apply decisions |
| TC-18 | `techspec.md#test-approach` | View model apply transitions |
| MA-2, MA-3 | `techspec.md#test-approach` | Manual portable/installed scripts |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (launch via Windows MCP `App` tool for manual runs).
- Existing code: `App.xaml.cs:44-72` (`OnStartup`), `421-427` (`ShutdownAsync`), `ApplicationLifetime.ShutdownAsync:95-108`; T04/T05 components.
- Contract or integration: `techspec.md#components-and-flow` (flow paragraph), `#errors-security-and-recovery`.

## Work

- [x] T08.1 `UpdateViewModel`: Update now → Downloading (progress 0–100, Cancel) → Applying → shutdown request; error states for asset missing, not writable (with release link), integrity failure, download failure, apply failure; re-entry blocked.
- [x] T08.2 `App.Updates.cs` orchestration: `RuntimeInformation.ProcessArchitecture` → selector, detector, downloader, applier/launcher, then `ShutdownAsync()`.
- [x] T08.3 `--updated` handling at the top of `OnStartup`: wait up to 30 s for the instance mutex, run `UpdateSwapJournal` cleanup, log, continue normal startup.
- [x] T08.4 Tests TC-18 (apply transitions with fakes for downloader/applier/shutdown).
- [x] T08.5 Prepare MA-2/MA-3 inputs: document in the handoff how to build a low-version zip (`dotnet publish … -p:Version=0.0.1`) and an older setup (`installer/build-installer.ps1`) for the visual check.

## Acceptance criteria

- Portable happy path (fakes): download → verify → swap → start `--updated` → shutdown requested, in that order.
- Installed happy path (fakes): download → verify → launch setup → shutdown requested.
- Not writable → error with release link, downloader never called.
- Integrity or download failure → error, applier never called, app not shut down.
- Apply failure → error, app not shut down.
- Cancel during download → back to Available, partial file removed (T04 behavior).
- `--updated` start waits for the mutex and deletes the journaled `.old` files.

## Verification

- Unit: TC-18 (apply part).
- Integration: relies on TC-14 (T05) for real file swap.
- E2E: omitted by .NET desktop policy.
- Manual: MA-2 (portable, writable and read-only folder) and MA-3 (installed) at the visual check, owner human.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: a published GitHub release newer than the test build and Inno Setup 6 for MA-3; network access.
- Expected evidence: passing `UpdateViewModelTests`; MA-2/MA-3 results recorded at the visual check.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/UpdateViewModel.cs`, `src/TokenHound.App/UI/Windows/UpdateWindow.xaml`, `src/TokenHound.App/App.Updates.cs`, `src/TokenHound.App/App.xaml.cs` (startup call site), `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateViewModelTests.cs`.

## Observability and recovery

- Operational signal: `UpdateApplyStarted {Mode}`, `UpdateApplyFailed {Mode} {Error}`, `UpdateCleanupCompleted {Files}`.
- Recovery: portable rollback (T05); installed failure leaves the current install; manual reinstall from the release page.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `UpdateApplyCoordinator` (Infrastructure) sequences the portable writability probe (portable mode only), `UpdateAssetSelector`, download + verify, then the portable swap (`Task.Run`) or the installer launch, and returns `UpdateApplyResult` (`Applied`, `NotWritable`, `AssetMissing`, `DownloadFailed`, `IntegrityFailed`, `ApplyFailed`); cancellation propagates; nothing is applied before verification. `UpdateViewModel` (new partial `UpdateViewModel.Apply.cs`): Update now -> Downloading (progress 0-100; Cancel returns to the offer) -> Applying + one `RequestShutdown`; each failure status renders its own message with the release-page link and "Check again"; re-entry is blocked while busy; an unexpected exception becomes an error. `UpdateWindow` shows the download bar, Cancel, an indeterminate bar while applying, and the release link on errors. `App.Updates.cs` builds the real steps per apply (`RuntimeInformation.ProcessArchitecture`, `InstallModeDetector`, `UpdateDownloader`, `PortableUpdateApplier`, `InstallerUpdateLauncher`) and requests `ShutdownAsync()`. `App.Updates.Startup.cs` (one call at the top of `OnStartup`, after logging) acquires `ApplicationInstanceMutex` (waits up to 30 s with `--updated`, otherwise does not wait), holds it for the process lifetime (released by process exit; the installer and the relaunched process treat abandonment as released), and runs `UpdateSwapJournal.CleanupAfterUpdate()` when owned, logging `UpdateCleanupCompleted {Files}`; if the previous instance did not exit in time, cleanup is deferred to the next start.
- Changed files: created `src/TokenHound.Infrastructure/Updates/{UpdateApplyCoordinator,UpdateApplySteps,UpdateApplyResult,UpdateApplyStatus}.cs`, `src/TokenHound.App/ViewModels/UpdateViewModel.Apply.cs`, `src/TokenHound.App/{App.Updates.Startup,UpdateStartup}.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateViewModelApplyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/UpdateApplyCoordinatorTests.cs`; modified `src/TokenHound.App/ViewModels/{UpdateViewModel,UpdateDialogDependencies,UpdateMessages}.cs` (the `UpdateNow` action became `ApplyAsync` + `RequestShutdown`), `src/TokenHound.App/UI/Windows/{UpdateWindow.xaml,UpdateWindow.xaml.cs}`, `src/TokenHound.App/App.Updates.cs` (placeholder `OpenReleasePage` removed), `src/TokenHound.App/App.xaml.cs` (+1 call), the test csproj (+1 link), and `UpdateViewModelActionTests.cs` (placeholder test rewritten).
- Checks: `rtk dotnet build TokenHound.slnx --no-restore` -> 7 projects, 0 errors, 0 warnings; full Infrastructure suite -> 927 passed (TC-18 apply part: 7 view model tests incl. a 5-row theory; coordinator: 9 tests covering portable order probe -> download -> swap, installed order download -> launch, not-writable and missing asset never downloading, integrity and download failures never applying, apply failure, cancel propagation). The real swap, rollback, and journal cleanup stay covered by TC-14 (T05). Quality profile over the T08 files: QA-01..QA-05 and QA-07 empty; QA-06 largest `App.xaml.cs` 455 (baseline 449; the +6 across the feature are call sites), `UpdateViewModel.cs` 289; QA-08 hit `UpdateApplyCoordinator.cs:102` is a 4-argument structured log call (reservation, not a parameter list).
- Validated state: base `a8bd1bf` + T01..T07 + the files above; Debug.
- MA-2/MA-3 inputs (T08.5), from the repo root in PowerShell: portable build `dotnet publish src/TokenHound.App/TokenHound.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:Version=0.0.1 -o <folder>` (copy it to a writable folder, and to a read-only one for the not-writable case; no `TokenHound.installed` next to the exe); installed build `installer/build-installer.ps1 -Version 0.0.1` (Inno Setup 6), then install the produced setup. Both need a published GitHub release newer than 0.0.1 with the `TokenHound-{ver}-win-x64-fxdependent.zip` and `TokenHound-Setup-{ver}-win-x64.exe` assets, and network access.
- Open items: MA-2 and MA-3 (manual, visual check, owner human). The update window has no `DialogService.CloseAll` hook; on apply the app shutdown closes it (owned window). The UI thread waits up to 30 s at startup for `--updated`, before any window appears.

### ADR candidates

None - direct TechSpec implementation or local decision.
