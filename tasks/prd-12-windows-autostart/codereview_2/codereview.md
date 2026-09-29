# Code review report — Start with Windows setting (prd-12-windows-autostart)

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer (fresh-context subagent in the same worktree; did not read `jev-log.jsonl` or call jev; control group for J3)
- Git scope: `b9bffe618dcc206a323028b81981bb71e0f51cc9..worktree` (staged, unstaged, and new files on top of `b9bffe6`, which is `HEAD`; `b9bffe6` itself touches only `.agents/skills/**`)
- Previous review: `tasks/prd-12-windows-autostart/codereview_1/codereview.md` (REJECTED; corrections T03..T06 in `codereview_1/done/`)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-12-windows-autostart/prd.md` | read (sha256 prefix `604499210b05`, equals checkpoint `approved_sources`) |
| TechSpec | `tasks/prd-12-windows-autostart/techspec.md` | read (sha256 prefix `6cafef4b66a7`, equals checkpoint `approved_sources`) |
| Manifest | `tasks/prd-12-windows-autostart/tasks.md` | read (sha256 prefix `66fe30d9773c`; checkpoint records `ded10f4cc78c`, see limitations) |
| Handoffs | `done/task_01.md`, `done/task_02.md`; corrections `codereview_1/done/task_03.md`..`task_06.md` | read; both manifest links resolve; T03..T06 each trace one `codereview_1` finding and carry a handoff |
| Flow log | `workflow.md`, `checkpoint.json` | read only |
| Snapshot | `context-snapshot.md` | loaded through the independent-stage filter (header, next step brief, open threads, `on-run` L-01/L-03); header is behind (stage `tasks`, `covers_through` T02, `git_head` c744030 vs `HEAD` b9bffe6, O-01 answered in `workflow.md` Events), so the brief was treated as stale and entries as hints |
| Implementation | `git diff b9bffe6` (17 paths) | delimited |

Reviewable code set (unchanged from `codereview_1`): `installer/TokenHound.iss`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`, `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs`, `src/TokenHound.Infrastructure/Startup/{IStartupApprovalStore,IStartupLaunchService,RegistryStartupApprovalStore,ShellLink,StartupLaunchService}.cs`, `tests/TokenHound.Core.Tests/Policies/StartupApprovalPolicyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/StartupSettingsViewModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`. `tasks/triage-log.jsonl` is a flow artifact, not code.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Entry is `<Startup>\TokenHound.lnk`, same file as the installer | `StartupLaunchService.cs:18,46,60-61`; `TokenHound.iss:92` (`{userstartup}\{#MyAppName}`, `Tasks: startupicon`) | TC-03 (file name), `CreateDefault_ResolvesStartupFolderWithoutVerification` | conformant | tests passed in this review |
| FR-02 | State read from the OS on each open; shortcut exists and not disabled | `StartupLaunchService.cs:64-65,101-102`; VM ctor `StartupSettingsViewModel.cs:40`; VM built per open `App.xaml.cs:364-366` | TC-01, TC-02, TC-05 | conformant | tests passed; MA-2 recorded passed by the human (`workflow.md` Events) |
| FR-03 | Enable writes/overwrites shortcut to running exe, working dir = its folder, clears disabled flag | `StartupLaunchService.cs:68-88`, `ShellLink.cs:28-51` | TC-03, TC-04, TC-05 | conformant | tests passed; production wiring now resolves a missing folder (CR-01 resolved) |
| FR-04 | Disable deletes shortcut; absent is not an error | `StartupLaunchService.cs:91-99` | TC-06 | conformant | test passed |
| FR-05 | General tab first; Apply enabled only when differing | `SettingsWindow.xaml:255-339`; `StartupSettingsViewModel.cs` `IsDirty`/`CanApply`/`ApplyCommand` | TC-07, TC-08 | conformant | tests passed; MA-1 recorded passed |
| FR-06 | Failure keeps real state, inline error, app keeps running | `StartupSettingsViewModel.cs:85-134`; error TextBlock `SettingsWindow.xaml:319-335` | TC-09 (3 exception types) | conformant | tests passed; the Settings-open crash path of `codereview_1/CR-01` is closed |
| FR-07 | Silent setups keep the shortcut as it was | `TokenHound.iss:92,125-128,154` | TC-10 compile + MA-3 | conformant | ISCC compile exit 0 in this review; logic traced (silent without `/TASKS` creates only when it existed, never deletes); MA-3 recorded passed |
| FR-08 | Interactive setup pre-selects from state; unticked removes | `TokenHound.iss:131-148` | MA-4 | conformant | one-time guard `StartupTaskPreselected`; MA-4 recorded passed |
| FR-09 | Uninstall removes the shortcut even if app-created | `TokenHound.iss:94-96` | MA-4 | conformant | `[UninstallDelete] Type: files`; MA-4 recorded passed |
| NFR-01 | Core pure; seams; tests never touch the real profile | `StartupApprovalPolicy.cs:1` (only `using System`); `IStartupApprovalStore`, `IStartupLaunchService` | QA-04 empty; temp folder + `FakeApprovalStore` | conformant | the new `CreateDefault` test only resolves paths and constructs the registry store; it performs no file or registry I/O |
| NFR-02 | Per-user only | `StartupLaunchService.cs:61` (`SpecialFolder.Startup`), `RegistryStartupApprovalStore.cs:25,39` (`Registry.CurrentUser`) | grep | conformant | `rg "LocalMachine|CommonStartup|SpecialFolder\.\w+|Registry\.\w+" src/TokenHound.Infrastructure/Startup` shows only those three lines |
| NFR-03 | < 200 ms, no visible stall | synchronous API (DEC-11) | TC-03 Stopwatch guard | conformant | test passed; MA-1 recorded passed |
| NFR-04 | Automation name and help text | `SettingsWindow.xaml:255-258,290-294` and Apply/status/error names | TC-11 (manual) | conformant | markup present; visual check recorded passed |
| US-01..US-03, OBJ-01, OBJ-02 | Journeys and outcomes | as above | MA-1..MA-4 | conformant | human sign-off "Tudo OK" (`workflow.md` Events); still valid after corrections, see Executed validations |
| TechSpec Errors | Missing Startup folder is created on `Enable` | `StartupLaunchService.cs:53-61` (`DoNotVerify`), `:73` (`Directory.CreateDirectory`) | `CreateDefault_ResolvesStartupFolderWithoutVerification`, TC-03 (non-existent `<temp>\Startup`) | conformant | see `codereview_1/CR-01` below |
| TechSpec Errors | Null `Environment.ProcessPath` hides the tab | `App.xaml.cs:364-366`; `SettingsWindow.xaml:261`; `SettingsWindow.xaml.cs:129-130` | — | conformant | code trace |
| TechSpec Observability | `Log.Information(... {Operation} ...)` on success; `Log.Warning(ex, "Startup shortcut {Operation} failed", ...)` on failure | `StartupLaunchService.cs:87,98`; `StartupSettingsViewModel.cs:92-93,106,124-129` | TC-09 (path exercised) | conformant | template `Startup shortcut {Operation} failed for {ExecutablePath}` with distinct `nameof` operation per call |
| TechSpec Contracts | `SHORTCUT_FILE_NAME` = `{#MyAppName}.lnk`, comment on each side | `StartupLaunchService.cs:15-18`; `TokenHound.iss:91,95` | — | conformant | code |
| TC-01..TC-09 | Automated tests | test files above | 5 + 6 + 6 cases | conformant | filtered 5/5 and 12/12; full 166/166 and 951/951 |
| TC-10, TC-11 | Installer compile and manual scripts | `.iss`, XAML | ISCC + MA | conformant | ISCC exit 0 (this review); MA recorded by the human |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (`CLAUDE.md`) | OK | `StartupApprovalPolicy.cs:1` |
| One class per file, sealed by default | OK | new classes sealed or static; nested private types only |
| XML docs on public members | OK | including the new `ResolveStartupFolder` (`StartupLaunchService.cs:56-59`) |
| File-scoped namespaces, alphabetized usings | OK | all new files |
| Constants `UPPER_CASE`, `nameof` | OK | `SHORTCUT_FILE_NAME`, `APPLY_ERROR`, `nameof(IStartupLaunchService.Enable)` etc. |
| `=>` on next line, blank-line rules | OK | new and corrected members |
| Split calls with >= 4 arguments | OK | `StartupLaunchService.cs:77-82`, `ShellLink.cs:60-68`, `StartupSettingsViewModel.cs:124-129`; sweep `rg '\w\([^()]*,[^()]*,[^()]*,[^()]*\)'` over the feature `.cs` files hits only the `ShellLink.Save` declaration and `InlineData` array initializers (not calls) |
| Static lambdas when capture-free | OK | `ShellLink.cs:62,77`; other lambdas capture instance state |
| Structured logging | OK | Serilog templates with typed arguments |
| `dotnet-efficient-validation` | OK | runner identified (SDK 10.0.401, `global.json` `test.runner: Microsoft.Testing.Platform`, MTP executables); builds then `rtk dotnet run --no-build --no-restore -- --minimum-expected-tests N`, exit codes checked |
| SDD state consistency | OK | `tasks.md:65` and `done/task_02.md:115` now record the visual check; T03..T06 handoffs present; manifest hash drift noted in limitations |

## Quality profile

`$files` = the 13 `.cs` files of the reviewable set.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Empty catch | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 | OK |
| QA-02 | `#nullable disable` / `#pragma warning disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-03 | Sync-over-async / `async void` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-04 | OS dependency in Core policy | blocking | `rg -n 'Microsoft.Win32\|InteropServices\|System.IO' src/TokenHound.Core/Policies/StartupApprovalPolicy.cs` | 0 | OK |
| QA-05 | COM release in `finally` (must hit) | blocking | `rg -n 'ReleaseComObject' src/TokenHound.Infrastructure/Startup/ShellLink.cs` | 2 (`:49`, `:100`, both in `finally`) | OK |
| QA-06 | `throw new Exception(` | reservation | `rg -n --type cs 'throw new Exception\(' $files` | 0 | OK |
| QA-07 | File > 300 lines / method > 30 lines | reservation | `rg -c '^' --type cs $files` | 1 aggravated of 1: `App.xaml.cs` 465 (baseline 461) | pre-existing, aggravated +4 — OI-03 |

All new `.cs` files are at most 161 lines; no new or changed method exceeds 30 lines (`OnDataContextChanged` in `SettingsWindow.xaml.cs:118-131` is 14 lines).

- Terrain baseline: applied from TechSpec (`App.xaml.cs` 461; `SettingsWindow.xaml` 732, already above 500).
- Hits discounted by baseline: 1 (`App.xaml.cs` above 300 before the feature; only +4 lines are the feature's). `SettingsWindow.xaml` (821) is baseline-excluded from the escalation trigger per TechSpec.
- Reservations accumulated in the feature: 1 (QA-07 aggravation), plus optional improvements OI-01, OI-02 outside the profile.
- Suggested escalation: no trigger fired (1 reservation < 8; no newly crossed 500-line file; no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 | YES | `StartupLaunchService.cs:53-61`; no JSON setting |
| DEC-02 | YES | `ShellLink.cs` `[ComImport] IShellLinkW` + `IPersistFile`; no package added |
| DEC-03 | YES | `StartupApprovalPolicy.cs:15-16`; `RegistryStartupApprovalStore.cs` read/delete only |
| DEC-04 | YES | seams, `CreateDefault()`, `OperatingSystem.IsWindows()` guards |
| DEC-05 | YES | `Enable` always saves; TC-04 |
| DEC-06 | YES | behavior per TC-07..TC-09; failure log now names the operation (`StartupSettingsViewModel.cs:124-129`) |
| DEC-07 | YES | first tab, `DataContext="{Binding Startup}"`, collapsed when null; local deviation (Cadence index → `CadenceTab.IsSelected`) recorded in the T02 handoff; `rg SelectedIndex src/TokenHound.App` empty |
| DEC-08 | YES | `TokenHound.iss:92,125-128,154` |
| DEC-09 | YES | `TokenHound.iss:131-148` |
| DEC-10 | YES | `TokenHound.iss:94-96` |
| DEC-11 | YES | no `Task`/async in service or VM |
| DEC-12 | YES | `rg InstallMode` over the service and VM is empty |
| CMP-02 "internal COM interop" | PARTIAL | `ShellLink.cs:13` is `public static class` (OI-01, optional) |
| Errors: missing Startup folder created on Enable | YES | `StartupLaunchService.cs:61,73` |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | CMP-01..06 present; TC-01..06 re-run and passing |
| T02 | `done/task_02.md` | COMPLETE | CMP-07..11 present; TC-07..09 re-run and passing; ISCC compile reproduced; MA-1..MA-4 recorded at `done/task_02.md:115` and `tasks.md:65` |
| T03 (CR-01) | `codereview_1/done/task_03.md` | COMPLETE | `DoNotVerify` at `StartupLaunchService.cs:61`; test `CreateDefault_ResolvesStartupFolderWithoutVerification` passing |
| T04 (CR-02) | `codereview_1/done/task_04.md` | COMPLETE | `{Operation}` template and three distinct `nameof` operation names (`StartupSettingsViewModel.cs:92,93,106,126`) |
| T05 (CR-03) | `codereview_1/done/task_05.md` | COMPLETE | split calls at `StartupLaunchService.cs:77-82`, `ShellLink.cs:60-68`; sweep clean |
| T06 (CR-04) | `codereview_1/done/task_06.md` | COMPLETE | acceptance `rg "pending at the visual check\|pending at the human visual check"` over `tasks.md` and `done/task_02.md` returns nothing (exit 1) |

## Executed validations

- Profile and exclusions: .NET 10 (SDK 10.0.401, `global.json` `test.runner: Microsoft.Testing.Platform`), MTP executables with xUnit v3; E2E omitted by .NET desktop policy (neither a defect nor approved testing).
- Validated state: worktree on `b9bffe6` plus the feature diff above (including corrections T03..T05), Debug configuration, Windows 11, .NET 10.0.12, rebuilt with `--no-incremental` in this review.
- Reused evidence: MA-1..MA-4 human sign-off from before correction round 1. It remains valid: T04 and T05 change only a log template and call formatting; T03 changes only the folder-resolution option, and on this machine `GetFolderPath(Startup)` and `GetFolderPath(Startup, DoNotVerify)` return the same existing path, so behavior with an existing Startup folder (the state the visual check used) is unchanged. `.iss` and XAML are unchanged by the corrections.
- Manual acceptance: MA-1..MA-4 recorded as passed by the human (`workflow.md` Events, "Tudo OK"); not re-executed by this reviewer.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App --no-restore --no-incremental --nologo --verbosity:minimal` | passed, 4 projects, 0 warnings, 0 errors | build gate, CMP-07..10 |
| `rtk dotnet build tests/TokenHound.Core.Tests --no-restore --no-incremental --nologo --verbosity:minimal` | passed, 0 warnings, 0 errors | TC-01 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests --no-restore --no-incremental --nologo --verbosity:minimal` | passed, 0 warnings, 0 errors | TC-02..09 |
| `rtk dotnet run --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed 166/166, exit 0 | regression, TC-01 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed 951/951, exit 0 | regression, TC-02..09, CR-01 test |
| `rtk dotnet run --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 5 --filter-class "*StartupApproval*"` | passed 5/5 | TC-01 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 12 --filter-class "*Startup*"` | passed 12/12 | TC-02..09, CR-01 test |
| `ISCC.exe /DPublishDir=<repo>\publish\win-x64 /DOutputDir=<scratchpad>\iscc installer\TokenHound.iss` | passed, exit 0, "Successful compile", no warning/error lines | TC-10 (compile) |
| PowerShell `[Environment]::GetFolderPath(<folder>[, 'DoNotVerify'])` for `Startup`, `CommonOemLinks`, `LocalizedResources` (.NET 10.0.12) | `Startup` same path both ways; the two absent folders return `''` by default and a path with `DoNotVerify` | CR-01 fix mechanism |
| QA-01..QA-07 greps, NFR-01/NFR-02 greps, 4-argument call sweep, T06 acceptance grep | as tabulated | quality profile, NFR-01, NFR-02, CR-03, CR-04 |

## Findings

No actionable findings in this review.

Optional improvements (not blocking):

| ID | Source | Evidence | Suggestion |
| --- | --- | --- | --- |
| OI-01 | CMP-02 ("internal COM interop") | `src/TokenHound.Infrastructure/Startup/ShellLink.cs:13` is `public`; T03 also added the public `StartupLaunchService.ResolveStartupFolder()` (`StartupLaunchService.cs:60`), both used by tests | Consider `internal` plus `InternalsVisibleTo` for the test assembly (no `InternalsVisibleTo` exists in the Infrastructure project today) |
| OI-02 | TC-03 / NFR-03 guard | `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs:79` asserts `< 200 ms` including the first COM activation | Can flake on a loaded CI runner; keep as a coarse guard or relax |
| OI-03 | QA-07 (reservation) | `src/TokenHound.App/App.xaml.cs` 461 → 465 lines (pre-existing above 300) | Recorded; no action required by this feature |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/CR-01 | resolved | `StartupLaunchService.cs:53-61` — `CreateDefault` uses `ResolveStartupFolder()` = `GetFolderPath(SpecialFolder.Startup, SpecialFolderOption.DoNotVerify)`, which returns a path for absent known folders (reproduced), so the constructor guard at `:35` no longer throws and `Enable` reaches `Directory.CreateDirectory` at `:73`; test `CreateDefault_ResolvesStartupFolderWithoutVerification` passes; TC-03 still covers creation of a missing folder |
| codereview_1/CR-02 | resolved | `StartupSettingsViewModel.cs:111-134` — `TryRun(string operationName, ...)` logs `"Startup shortcut {Operation} failed for {ExecutablePath}"`; callers pass `IsEnabled`, `Enable`, `Disable` (`:92,93,106`) |
| codereview_1/CR-03 | resolved | `StartupLaunchService.cs:77-82`, `ShellLink.cs:60-68` split one argument per line; sweep clean |
| codereview_1/CR-04 | resolved | `tasks.md:65` and `done/task_02.md:115` record MA-1..MA-4 passed (2026-09-29, "Tudo OK", `workflow.md` Events) |
| codereview_1/OI-01 | persistent (optional) | `ShellLink.cs:13` still `public` — carried as OI-01 |
| codereview_1/OI-02 | persistent (optional) | `StartupLaunchServiceTests.cs:79` — carried as OI-02 |
| codereview_1/OI-03 | persistent (optional) | `App.xaml.cs` 465 lines — carried as OI-03 |

## Limitations and open items

- Manual acceptance MA-1..MA-4 (FR-02 Windows flag format, FR-07..FR-09, NFR-03, NFR-04) was not re-executed by this reviewer: the Windows MCP server failed to connect in this session, and the contract forbids `build-installer.ps1`. The review relies on the human sign-off in `workflow.md` Events ("Tudo OK", no per-script observations), which the corrections did not invalidate (see Executed validations).
- The missing-Startup-folder path of CR-01 was not exercised end to end against the real profile (NFR-01 forbids touching it). The fix is proven by the `DoNotVerify` mechanism on other absent known folders plus the unit test on path resolution.
- `tasks.md` sha256 prefix is now `66fe30d9773c`, while `checkpoint.json` `approved_sources` still records `ded10f4cc78c` (already flagged in `codereview_1`). The folder is untracked, so the change cannot be diffed; the visible content matches the approved plan plus state-only updates (T01/T02 done, the T06 visual-check note). For `workflow.md` (which this reviewer may not edit): the flow should refresh the recorded manifest hash.
- The TechSpec NFR-01 review command `rg -n "Microsoft.Win32|System.Runtime.InteropServices" src/TokenHound.Core` is not empty: it hits pre-existing `src/TokenHound.Core/Policies/UpdateAssetSelector.cs:3`, untouched by this feature. The feature file is clean (QA-04).
- The installer compile used the existing `publish/win-x64` output (2026-09-29 09:12), not a fresh publish; it validates the `.iss` script, not the packaged binaries.
- The snapshot header is behind (stage `tasks`, `git_head` c744030); only the filtered tiers were used, as hints.

## Conclusion

All obligations of the PRD (FR-01..FR-09, NFR-01..NFR-04, US-01..US-03) and TechSpec (DEC-01..DEC-12, error and observability contracts, TC-01..TC-11) are conformant. The four findings of `codereview_1` are resolved: the Startup folder is resolved with `DoNotVerify`, so Settings no longer crashes when the folder is missing, the failure log names the operation, the four-argument calls are split, and the manifest and handoff record the visual check. The builds have 0 warnings, all automated tests pass (Core 166/166, Infrastructure 951/951, 17 feature tests), the installer script compiles, and no blocking quality-profile rule fires. The status is APPROVED WITH RESERVATIONS: three optional improvements remain (public `ShellLink`/`ResolveStartupFolder`, a timing assertion that may flake, and the pre-existing `App.xaml.cs` size, +4 lines). Manual acceptance rests on the recorded human sign-off, and the stale manifest hash in the checkpoint still needs refreshing.
