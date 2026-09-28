# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T05 — Apply mechanisms: instance mutex, portable swap, installer launch, and installer script

## Outcome

Given a verified download, the app can apply it in both modes. Portable: the zip is extracted, existing files are renamed to `.old`, new files are moved in, a swap journal is written, and the new exe is started with `--updated`; a failed start restores everything. Installed: the setup is started with the silent arguments. A named instance mutex lets the relaunched exe and the installer wait for the old process to exit. The Inno script installs the marker, waits for the mutex, and relaunches after a silent update.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T08
- In scope: CMP-12 (`PortableUpdateApplier`, `UpdateSwapJournal` with startup cleanup), CMP-13 (`InstallerUpdateLauncher`), CMP-14 (`ApplicationInstanceMutex`), CMP-20 (`installer/TokenHound.iss`, `installer/TokenHound.installed`), tests TC-14..TC-16.
- Out of scope: calling these from the App and shutting down (T08); the downloader (T04).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Installer installs the marker; zip does not |
| FR-09 | `prd.md#functional-requirements` | Swap, keep previous until new starts, rollback |
| FR-10 | `prd.md#functional-requirements` | Silent per-user installer, relaunch |
| NFR-05 | `prd.md#non-functional-requirements` | Per-user, no elevation |
| CON-3 | `prd.md#constraints-and-dependencies` | `TokenHound.iss` change |
| DEC-05, DEC-08, DEC-09, DEC-10 | `techspec.md#technical-decisions` | Marker, portable swap, installer args + `[Code]`, mutex |
| CMP-12..CMP-14, CMP-20 | `techspec.md#components-and-flow` | Components |
| TC-14..TC-16 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md`; `dotnet-efficient-validation`.
- Existing code: `installer/TokenHound.iss` (`[Files]` 83-84, `[Run]` 90-91), `installer/build-installer.ps1` (local installer build), `src/TokenHound.Infrastructure/System/` (OS interop placement).
- Contract or integration: `techspec.md#contracts-and-data` (`swap-journal.json`, `--updated`, `/RELAUNCH=1`); Inno Setup docs for `CheckForMutexes` and `{param:}` (apply jev `J0` if fetched from the web).

## Work

- [x] T05.1 `ApplicationInstanceMutex`: acquire `Local\TokenHound.App.Instance` without blocking at normal startup; `WaitForRelease(timeout)` for `--updated`; abandoned mutex counts as acquired; disposal releases.
- [x] T05.2 `UpdateSwapJournal` (write/read/delete `swap-journal.json`) and `CleanupAfterUpdate()` deleting journaled `.old` files, idempotent.
- [x] T05.3 `PortableUpdateApplier.Apply(zipPath, appDirectory, exePath, processStarter)`: extract to staging, require the exe in the archive, rename-then-move per file, journal, start `exePath --updated`; on start failure restore `.old` files and delete the journal. Process start behind an injectable seam for tests.
- [x] T05.4 `InstallerUpdateLauncher.Launch(setupPath)`: absolute path, `UseShellExecute=false`, args `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1`.
- [x] T05.5 `installer/TokenHound.installed` (one comment line) + `[Files]` entry; `[Code]` `InitializeSetup` waiting up to 30 s on `CheckForMutexes('TokenHound.App.Instance')`; `ShouldRelaunch` from `{param:RELAUNCH|0}`; `[Run]` `Flags: nowait; Check: ShouldRelaunch`.
- [x] T05.6 Tests TC-14..TC-16.

## Acceptance criteria

- Successful portable apply: new files in place, previous ones as `.old`, journal lists them, the starter received `--updated`.
- Start failure: every original file restored under its original name, no `.old` left, journal deleted, typed error returned.
- Cleanup deletes only journaled files and succeeds when run twice.
- Installer launch uses exactly the four arguments and the absolute setup path.
- Mutex: a second acquisition times out while the first is held and succeeds after release; abandoned counts as acquired.
- `installer/build-installer.ps1` builds the setup with the new script without errors.

## Verification

- Unit: TC-15, TC-16.
- Integration: TC-14 on a temp directory with real files and a fake process starter.
- E2E: omitted by .NET desktop policy.
- Manual: installer compile via `installer/build-installer.ps1` (requires Inno Setup 6 locally; if absent, record as open item for MA-3).
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`; `pwsh installer/build-installer.ps1` (when Inno Setup is installed).
- Environment dependency: Inno Setup 6 for the script compile; otherwise none.
- Expected evidence: passing `PortableUpdateApplierTests`, `InstallerUpdateLauncherTests`, `ApplicationInstanceMutexTests`, `UpdateSwapJournalTests`; ISCC output or recorded gap.

## Affected files

- Modify: `installer/TokenHound.iss`.
- Create: `installer/TokenHound.installed`, `src/TokenHound.Infrastructure/System/ApplicationInstanceMutex.cs`, `src/TokenHound.Infrastructure/Updates/{PortableUpdateApplier,UpdateSwapJournal,InstallerUpdateLauncher,UpdateApplyException}.cs`, matching tests.

## Observability and recovery

- Operational signal: `UpdateApplyStarted {Mode}`, `UpdateApplyFailed {Mode} {Error}`, `UpdateCleanupCompleted {Files}`.
- Recovery: rollback path in T05.3; leftover `.old` files are removed by the journal cleanup at next start.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `ApplicationInstanceMutex` (`TryAcquire(timeout)`, abandoned = acquired, released on `Dispose`; name `TokenHound.App.Instance`, unprefixed = session namespace, same name the installer checks — equivalent to the `Local\` form in DEC-10). `UpdateSwapJournal` (`swap-journal.json` in `%LOCALAPPDATA%\TokenHound\updates`; `Write`/`Read`/`Delete`; `CleanupAfterUpdate` deletes only journaled `.old` files, keeps the journal while a file is still locked, logs `UpdateCleanupCompleted {Files}`). `PortableUpdateApplier.Apply(zip, appDirectory, exeName)` — extracts to `updates\staging`, requires the exe, journals, renames each existing file to `.old` and moves the new one in, starts `<exe> --updated` (`UseShellExecute=false`, working dir = app dir); any failure restores originals, deletes added files and the journal, logs `UpdateApplyFailed {Mode} {Error}`, throws `UpdateApplyException`. `InstallerUpdateLauncher.Launch(setup)` — absolute path, `UseShellExecute=false`, `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1`; missing file or start failure → `UpdateApplyException`. `installer/TokenHound.installed` + `[Files]` entry; `[Run]` relaunch entry with `Check: ShouldRelaunch`; `[Code]` `InitializeSetup` waits up to 30 s on `CheckForMutexes('TokenHound.App.Instance')` only when `/RELAUNCH=1` (manual installs never wait).
- Changed files: modified `installer/TokenHound.iss`; created `installer/TokenHound.installed`, `src/TokenHound.Infrastructure/System/ApplicationInstanceMutex.cs`, `src/TokenHound.Infrastructure/Updates/{UpdateApplyException,SwapJournalEntry,UpdateSwapJournal,PortableUpdateApplier,ProcessStarter,InstallerUpdateLauncher}.cs`, `tests/TokenHound.Infrastructure.Tests/System/ApplicationInstanceMutexTests.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{PortableUpdateApplierTests,InstallerUpdateLauncherTests}.cs`.
- Checks: build 0 errors, 0 warnings; filtered `ApplicationInstanceMutexTests` + `PortableUpdateApplierTests` + `InstallerUpdateLauncherTests` → 9 passed (TC-14, TC-15, TC-16); full Infrastructure suite → 882 passed. ISCC 6 (`%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`) compiled `installer/TokenHound.iss` with a stub publish folder → "Successful compile", `TokenHound-Setup-0.0.1-win-x64.exe` produced (proves `[Code]`, `CheckForMutexes`, `{param:}` and the marker source resolve). Quality profile: QA-01..QA-05, QA-07, QA-08 empty; largest file 174 lines; longest method 31 lines incl. signature.
- Validated state: base `a8bd1bf` + T01..T04 + the files above; Debug, net10.0; Inno Setup 6 local.
- Open items: `installer/build-installer.ps1 -Version 0.0.1` ran after the J3 gate flagged it: publish + ISCC succeeded (`dist/TokenHound-Setup-0.0.1-win-x64.exe`, exit 0) and the publish folder has no `TokenHound.installed`, so the zip never carries the marker. The 0.0.1 outputs were moved out of the repo to the session scratchpad (`ma-builds/`) for MA-2/MA-3. Real install/uninstall behavior (marker removed on uninstall, silent relaunch) remains for MA-3.

### ADR candidates

None - direct TechSpec implementation or local decision.
