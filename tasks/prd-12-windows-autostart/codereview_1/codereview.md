# Code review report — Start with Windows setting (prd-12-windows-autostart)

## Summary

- Status: REJECTED
- Execution: delegated reviewer (fresh-context subagent, same worktree; did not read `jev-log.jsonl` or call jev; control group for J3)
- Git scope: `b9bffe618dcc206a323028b81981bb71e0f51cc9..worktree` (unstaged and untracked changes on top of `b9bffe6`; `b9bffe6` itself touches only `.agents/skills/**`, verified with `git show --stat b9bffe6`)
- Previous review: `—`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-12-windows-autostart/prd.md` | read (sha256 prefix `604499210b05`, equals checkpoint `approved_sources`) |
| TechSpec | `tasks/prd-12-windows-autostart/techspec.md` | read (sha256 prefix `6cafef4b66a7`, equals checkpoint `approved_sources`) |
| Manifest | `tasks/prd-12-windows-autostart/tasks.md` | read (sha256 prefix `b958172c615d`; checkpoint records `ded10f4cc78c`, see limitations) |
| Handoffs | `done/task_01.md`, `done/task_02.md` | read; both links from `tasks.md` resolve |
| Flow log | `workflow.md`, `checkpoint.json` | read only |
| Snapshot | `context-snapshot.md` | loaded through the independent-stage filter (header, next step brief, open threads, `on-run` L-01/L-03 only); header is behind (stage `tasks`, `git_head` c744030, O-01 answered in `workflow.md`), used as hints only |
| Implementation | `git diff b9bffe6` over 12 code/test/installer paths + 8 new files; `tasks/triage-log.jsonl` is a flow artifact, not code | delimited |

Reviewable code set: `installer/TokenHound.iss`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`, `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs`, `src/TokenHound.Infrastructure/Startup/{IStartupApprovalStore,IStartupLaunchService,RegistryStartupApprovalStore,ShellLink,StartupLaunchService}.cs`, `tests/TokenHound.Core.Tests/Policies/StartupApprovalPolicyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/StartupSettingsViewModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Entry is `<Startup>\TokenHound.lnk`, same file as installer | `StartupLaunchService.cs:18,46,53`; `TokenHound.iss:92` unchanged name + `Check` | TC-03 `Enable_CreatesShortcutToExecutable` asserts file name | conformant | test passed; `.iss` icon still `{userstartup}\{#MyAppName}` with `Tasks: startupicon` |
| FR-02 | State read from OS each open; shortcut exists and not disabled | `StartupLaunchService.cs:56-57,88-89`; VM ctor `StartupSettingsViewModel.cs:40`; VM built per open `App.xaml.cs:364` via factory `:381` | TC-01, TC-02, TC-05 | conformant | tests passed; MA-2 recorded passed by human (`workflow.md` Events) |
| FR-03 | Enable writes/overwrites shortcut to running exe, working dir = folder, clears disabled flag | `StartupLaunchService.cs:60-75`, `ShellLink.cs:28-51` | TC-03, TC-04, TC-05 | conformant (seam) | tests passed; see CR-01 for the production wiring of the missing-folder edge |
| FR-04 | Disable deletes shortcut; absent is not an error | `StartupLaunchService.cs:78-86` | TC-06 | conformant | test passed |
| FR-05 | General tab first, Apply enabled only when differing | `SettingsWindow.xaml:255-340`; `StartupSettingsViewModel.cs` `IsDirty`/`CanApply`/`ApplyCommand` | TC-07, TC-08 | conformant | tests passed; MA-1 recorded passed |
| FR-06 | Failure keeps real state, inline error, app keeps running | `StartupSettingsViewModel.cs:85-127`; error TextBlock `SettingsWindow.xaml:319-335` | TC-09 (3 exception types) | conformant for Apply failures | tests passed; the Settings-open crash in CR-01 is outside the create/delete path FR-06 names but contradicts its "app keeps running" intent |
| FR-07 | Silent setups keep the shortcut as it was | `TokenHound.iss:92,125-128,144-148,154` | TC-10 (compile) + MA-3 | conformant | ISCC compile exit 0 in this review; MA-3 recorded passed by human; logic traced: silent + no `/TASKS` creates only when it existed, never deletes |
| FR-08 | Interactive pre-selects from state; unticked removes | `TokenHound.iss:131-141,144-148` | MA-4 | conformant | MA-4 recorded passed; one-time guard `StartupTaskPreselected` preserves user edits on Back/Next |
| FR-09 | Uninstall removes shortcut even if app-created | `TokenHound.iss:94-96` | MA-4 | conformant | `[UninstallDelete] Type: files` present; MA-4 recorded passed |
| NFR-01 | Core pure; seams; tests never touch real profile | `StartupApprovalPolicy.cs` (only `using System`); `IStartupApprovalStore`, `IStartupLaunchService` | QA-04 grep empty; tests use temp folder + `FakeApprovalStore` | conformant | see Quality profile; TechSpec's broader Core grep hits pre-existing `UpdateAssetSelector.cs:3` only |
| NFR-02 | Per-user only | `StartupLaunchService.cs:53` (`SpecialFolder.Startup`), `RegistryStartupApprovalStore.cs:25,39` (`Registry.CurrentUser`) | grep | conformant | `rg "LocalMachine|CommonStartup|SpecialFolder\.\w+|Registry\.\w+" src/TokenHound.Infrastructure/Startup` shows only those |
| NFR-03 | < 200 ms, no visible stall | synchronous API (DEC-11) | TC-03 Stopwatch guard | conformant | test passed; MA-1 recorded passed |
| NFR-04 | Automation name and help text | `SettingsWindow.xaml:255-258,290-294` and Apply/status/error names | manual TC-11 | conformant | markup present; visual check recorded passed |
| US-01..US-03 | Journeys | as above | MA-1..MA-4 | conformant | human sign-off "Tudo OK" in `workflow.md` Events (reviewer did not re-execute; see limitations) |
| OBJ-01, OBJ-02 | Outcomes | as above | unit + MA | conformant | as above |
| TechSpec Errors | Missing Startup folder is created on `Enable` | `StartupLaunchService.cs:65` reachable only when the path is non-empty; `CreateDefault` `:53` | none for real wiring | non-conformant | CR-01 |
| TechSpec Errors | Null `Environment.ProcessPath` hides the tab | `App.xaml.cs:364-366`; `SettingsWindow.xaml:261`; `SettingsWindow.xaml.cs:129-130` | — | conformant | code trace |
| TechSpec Observability | `Log.Warning(ex, "Startup shortcut {Operation} failed", ...)` | `StartupSettingsViewModel.cs:122` | — | non-conformant (minor) | CR-02 |
| TechSpec Contracts | `SHORTCUT_FILE_NAME` = `{#MyAppName}.lnk`, comment on each side | `StartupLaunchService.cs:15-18`; `TokenHound.iss:91,95` | — | conformant | code |
| TC-01..TC-09 | Automated tests | test files above | 5 + 5 + 6 test cases | conformant | filtered runs 5/5 and 11/11, full runs 166/166 and 950/950 |
| TC-10, TC-11 | Installer compile and manual scripts | `.iss`, XAML | ISCC + MA | conformant | ISCC exit 0 no warnings (this review); MA recorded by human |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (`CLAUDE.md`) | OK | `StartupApprovalPolicy.cs:1` only `using System` |
| One class per file, sealed by default | OK | new classes sealed or static; nested private types only |
| XML docs on public members | OK | all public members in new files documented |
| File-scoped namespaces, alphabetized usings | OK | all new files |
| Constants `UPPER_CASE`, `nameof` | OK | `SHORTCUT_FILE_NAME`, `APPLY_ERROR`, `nameof(Enable)` |
| `=>` on next line, blank-line rules | OK | new files |
| Split calls with >= 4 arguments | NOT OK | `StartupLaunchService.cs:69`, `ShellLink.cs:60` — CR-03 |
| Static lambdas when capture-free | OK | `ShellLink.cs:60,69` |
| Structured logging | OK | Serilog templates with typed args |
| `dotnet-efficient-validation` | OK | MTP executables via `rtk dotnet run --project ... --no-build --no-restore -- --minimum-expected-tests N` |
| SDD state consistency | NOT OK | `tasks.md:65`, `done/task_02.md` Handoff "Open items" vs `workflow.md` Events visual-check approval — CR-04 |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Empty catch | blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 | OK |
| QA-02 | `#nullable disable` / `#pragma warning disable` | blocking | `rtk rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-03 | Sync-over-async / `async void` | blocking | `rtk rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-04 | OS dependency in Core policy | blocking | `rtk rg -n 'Microsoft.Win32\|InteropServices\|System.IO' src/TokenHound.Core/Policies/StartupApprovalPolicy.cs` | 0 | OK |
| QA-05 | COM release in `finally` (must hit) | blocking | `rtk rg -n 'ReleaseComObject' src/TokenHound.Infrastructure/Startup/ShellLink.cs` | 2 (`:49`, `:92`, both in `finally`) | OK |
| QA-06 | `throw new Exception(` | reservation | `rtk rg -n --type cs 'throw new Exception\(' $files` | 0 | OK |
| QA-07 | File > 300 lines / method > 30 lines | reservation | `rtk rg -c '^' --type cs $files` | 1 aggravated of 1: `App.xaml.cs` 465 (baseline 461) | pre-existing, aggravated +4 — OI-03 |

`$files` = the 13 `.cs` files of the reviewable set. All new files are at most 150 lines; no new method exceeds 30 lines.

- Terrain baseline: applied from TechSpec (`App.xaml.cs` 461, `SettingsWindow.xaml` 732 already above 500).
- Hits discounted by baseline: 1 (`App.xaml.cs` above 300 existed before; only the +4 lines are the feature's). `SettingsWindow.xaml` (821) is baseline-excluded from the escalation trigger per TechSpec.
- Reservations accumulated in the feature: 1 (QA-07 aggravation), plus optional improvements OI-01, OI-02 outside the profile.
- Suggested escalation: no trigger fired (1 reservation < 8; no newly crossed 500-line file; no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 | YES | `StartupLaunchService.cs:53`; no JSON setting added |
| DEC-02 | YES | `ShellLink.cs` `[ComImport] IShellLinkW` + `IPersistFile`, no package |
| DEC-03 | YES | `StartupApprovalPolicy.cs:15-16`; `RegistryStartupApprovalStore.cs` read/delete only, never writes bytes |
| DEC-04 | YES | seams and `CreateDefault()`; `OperatingSystem.IsWindows()` guards in the store |
| DEC-05 | YES | `Enable` always saves; TC-04 |
| DEC-06 | PARTIAL | behavior matches; failure log lacks `{Operation}` (CR-02) |
| DEC-07 | YES | first tab, `DataContext="{Binding Startup}"`, collapsed when null; local deviation (Cadence index → `CadenceTab.IsSelected`) recorded in T02 handoff and verified: no remaining `SelectedIndex` reference in `src/TokenHound.App` |
| DEC-08 | YES | `TokenHound.iss:92,125-128,154` |
| DEC-09 | YES | `TokenHound.iss:131-148` |
| DEC-10 | YES | `TokenHound.iss:94-96` |
| DEC-11 | YES | no `Task`/async in service or VM |
| DEC-12 | YES | no `InstallMode` branch in service, VM, or wiring |
| CMP-02 "internal COM interop" | PARTIAL | `ShellLink.cs:13` is `public static class` (OI-01) |
| Errors: missing Startup folder created on Enable | NO | CR-01 |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | CMP-01..06 present; TC-01..06 re-run and passing in this review; QA greps reproduced |
| T02 | `done/task_02.md` | COMPLETE (code); state record stale | CMP-07..11 present; TC-07..09 re-run and passing; ISCC compile reproduced; MA-1..MA-4 approved per `workflow.md` but manifest and handoff still say pending (CR-04) |

## Executed validations

- Profile and exclusions: .NET 10 (SDK 10.0.401, `global.json` `test.runner: Microsoft.Testing.Platform`), MTP executables with xUnit v3; E2E omitted by .NET desktop policy.
- Validated state: worktree on `b9bffe6` plus the feature diff listed above, Debug configuration, Windows 11, rebuilt with `--no-incremental` in this review.
- Reused evidence: the `build-installer.ps1` publish/compile in the T02 handoff was not rerun (contract); replaced by a direct ISCC compile of the current `.iss` into the scratchpad against the existing `publish/win-x64` (dated 2026-09-29 09:12).
- Manual acceptance: MA-1..MA-4 recorded as passed by the human in `workflow.md` Events ("Tudo OK"); not re-executed by this reviewer.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App --no-restore --no-incremental --nologo --verbosity:minimal` | passed, 0 warnings, 0 errors | build gate, CMP-07..10 |
| `rtk dotnet build tests/TokenHound.Core.Tests --no-restore --no-incremental ...` | passed, 0 warnings | TC-01 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests --no-restore --no-incremental ...` | passed, 0 warnings | TC-02..09 |
| `rtk dotnet run --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed 166/166 | regression, TC-01 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed 950/950 | regression, TC-02..09 |
| `rtk dotnet run --project tests/TokenHound.Core.Tests ... -- --minimum-expected-tests 5 --filter-class "*StartupApproval*"` | passed 5/5 | TC-01 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests ... -- --minimum-expected-tests 11 --filter-class "*Startup*"` | passed 11/11 | TC-02..09 |
| `ISCC.exe /O<scratchpad> /DPublishDir=<repo>\publish\win-x64 installer\TokenHound.iss` | passed, exit 0, no warning/error lines | TC-10 (compile) |
| QA-01..QA-07 greps (above) | as tabulated | quality profile |
| PowerShell `[Environment]::GetFolderPath` default vs `DoNotVerify` over all `SpecialFolder` values (.NET 10.0.12) | `LocalizedResources` and `CommonOemLinks` (absent folders) return `''` by default and a path with `DoNotVerify` | CR-01 cause |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | TechSpec "Errors, security, and recovery" (missing Startup folder is created on `Enable`); FR-06 intent (app keeps running) | `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs:53` — `CreateDefault` calls `Environment.GetFolderPath(Environment.SpecialFolder.Startup)` with the default option, which returns `""` when the folder does not exist (reproduced on .NET 10.0.12 for other absent known folders). The constructor then throws `ArgumentException` at `StartupLaunchService.cs:35`. It is called from `App.CreateSettingsViewModel` (`src/TokenHound.App/App.xaml.cs:364-366`) with no catch on the path `HudActionsViewModel.ShowSettings` → `DialogService.ShowSettings` → factory, and `OnDispatcherUnhandledException` (`App.xaml.cs:110-114`) only logs without setting `Handled`. The `Directory.CreateDirectory` at `:65` is never reached in production for this case; TC-03 covers it only through a custom folder path | If the user's Startup folder is missing (deleted by the user or a cleanup tool), opening Settings terminates the app and every Settings tab becomes unreachable. Probability low, impact high | Resolve the folder with `Environment.GetFolderPath(Environment.SpecialFolder.Startup, Environment.SpecialFolderOption.DoNotVerify)` in `CreateDefault`, so the path is returned even when absent and `Enable` creates it as specified; cover `CreateDefault` resolution or the empty-path case with a test if feasible |
| CR-02 | Low | TechSpec "Observability and rollout" signals; DEC-06 | `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs:122` — `LOGGER.Warning(ex, "Startup shortcut update failed for {ExecutablePath}", _executablePath)`; the same message is logged for read failures from `ReadState` (`:99-106`) | Failure logs cannot distinguish Enable, Disable, or state-read failures, and the template differs from the specified `{Operation}` signal | Pass the operation name into `TryRun` and log `"Startup shortcut {Operation} failed"` with it |
| CR-03 | Low | `CLAUDE.md` "Split calls with >= 4 arguments across lines" | `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs:69` — `ShellLink.Save(ShortcutPath, executablePath, workingDirectory, SHORTCUT_DESCRIPTION);`; `src/TokenHound.Infrastructure/Startup/ShellLink.cs:60` — `link.GetPath(buffer, buffer.Capacity, IntPtr.Zero, 0)` | Style rule violation in new code | Split both calls one argument per line, per the repository rule |
| CR-04 | Low | SDD state consistency (skill status rule "inconsistent state") | `tasks/prd-12-windows-autostart/tasks.md:65` — "T02 — done (MA-1..MA-4 pending at the visual check)"; `done/task_02.md` Handoff "Open items: MA-1..MA-4 ... pending"; `workflow.md` Events — "Visual check approved by the human: MA-1..MA-4 all passed" | The manifest and handoff do not reflect the recorded manual acceptance, so the only proof of MA-1..MA-4 lives in the flow log | Record the visual-check result (date, human text, reference to `workflow.md` Events) in the manifest State and the T02 handoff |

Optional improvements (not blocking):

- OI-01 — CMP-02 specifies an internal COM interop; `src/TokenHound.Infrastructure/Startup/ShellLink.cs:13` is `public`, widening the Infrastructure API to allow tests to call `ReadTarget`/`ReadWorkingDirectory`. Consider `internal` plus `InternalsVisibleTo` for the test assembly.
- OI-02 — `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs:65` asserts `< 200 ms` including the first ShellLink COM activation; this is the coarse guard T01 required, but it can flake on a loaded CI runner.
- OI-03 (QA-07 reservation) — `src/TokenHound.App/App.xaml.cs` grows from 461 to 465 lines (pre-existing above 300).

## Limitations and open items

- Manual acceptance MA-1..MA-4 (FR-02 Windows flag format, FR-07..FR-09, NFR-03, NFR-04) was not re-executed by this reviewer: the Windows MCP server failed to connect in this session and the contract forbids `build-installer.ps1`. The review relies on the human sign-off recorded in `workflow.md` Events, which is a single "Tudo OK" without per-script observations. The per-script results should be recorded in the manifest and handoff (CR-04).
- `tasks.md` sha256 prefix is `b958172c615d`, while `checkpoint.json` `approved_sources` records `ded10f4cc78c`. `tasks/prd-12-windows-autostart/` is untracked, so the change cannot be diffed. The visible content matches the approved plan (T01, T02, State marked done), consistent with a state-only update. The flow should refresh the recorded hash (recorded here in place of `workflow.md`, which this reviewer may not edit).
- The TechSpec NFR-01 review command `rg -n "Microsoft.Win32|System.Runtime.InteropServices" src/TokenHound.Core` is not empty. It hits `src/TokenHound.Core/Policies/UpdateAssetSelector.cs:3`, a pre-existing file untouched by this feature. The feature file is clean (QA-04).
- The snapshot header is behind: stage `tasks`, `git_head` c744030 vs HEAD `b9bffe6`, and O-01 answered per `workflow.md`. Only the filtered tiers were used, as hints.
- The installer compile used the existing `publish/win-x64` output (09:12), not a fresh publish. The compile validates the `.iss` script, not the packaged binaries.

## Conclusion

The feature is broadly conformant. The service, policy, ViewModel, General tab, and installer logic implement FR-01..FR-09 and DEC-01..DEC-12. The builds have 0 warnings, all automated tests pass (Core 166/166, Infrastructure 950/950, including the 16 feature tests), the installer script compiles, and no blocking quality-profile rule fires. The status is REJECTED because of CR-01. The TechSpec's "missing Startup folder is created on Enable" obligation is not met in the production wiring: with a missing Startup folder, `CreateDefault` throws while Settings opens, and the unhandled dispatcher exception terminates the app. The fix is a one-argument change with a proven cause. CR-02..CR-04 are low-severity corrections to the log signal, a style rule, and the SDD state record.
