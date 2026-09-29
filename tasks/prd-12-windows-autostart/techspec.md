# TechSpec — Start with Windows setting

## Sources and traceability

- PRD: `tasks/prd-12-windows-autostart/prd.md`
- Applicable instructions: `CLAUDE.md`/`AGENTS.md` (Core purity, C# style, MTP test commands), skills `dotnet-efficient-validation`, `repository-cli-efficiency`
- Specs and design: none apply. No provider, credential, or rate-limit change. The Settings window reuses its own styles.
- Evidence in existing code:
  - `installer/TokenHound.iss:79-97`: `[Tasks] startupicon` (unchecked), `[Icons] {userstartup}\TokenHound` with `Tasks: startupicon`, and `[Code]` with `ShouldRelaunch`/`InitializeSetup`
  - `src/TokenHound.Infrastructure/Updates/InstallerUpdateLauncher.cs:18`: the self-update runs setup with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1` and no `/TASKS`
  - `src/TokenHound.App/ViewModels/UpdateSettingsViewModel.cs`: the tab ViewModel pattern (`IsDirty`, `CanApply`, `ApplyCommand`, `ApplyError`, `IsApplied`, nested `RelayCommand`)
  - `src/TokenHound.App/ViewModels/SettingsViewModel.cs:62`: optional tab ViewModels exposed as `init` properties (`Updates`)
  - `src/TokenHound.App/App.xaml.cs:349-363`: `CreateSettingsViewModel` builds the ViewModel each time Settings opens
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml:255,287,497`: tabs Providers, Cadence & Rate Limits, and Updates. The Updates tab markup (`:497-560`) is the template for a card with a checkbox, an error, and Apply
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj:60-80`: App ViewModels are compiled into the Infrastructure test project through `<Compile Include ... Link>` (that project is `net10.0`, without WPF)
  - `.github/workflows/ci.yml:19`: CI runs on `windows-latest`, so COM shell-link tests can run there

## Solution summary

A small Infrastructure service owns the startup entry: the `TokenHound.lnk` shortcut in the user's Startup folder, which is the same file the installer's `startupicon` task creates. It reads the state from the OS each time: the shortcut must exist, and its Windows "Startup apps" approval value must not be marked disabled. Enabling creates or overwrites the shortcut, pointing it at the running exe, and deletes a disabled approval value. Disabling deletes the shortcut. A new "General" tab in Settings hosts a checkbox bound to a new `StartupSettingsViewModel`. It follows the Apply pattern of `UpdateSettingsViewModel`, but its baseline is the OS state instead of a JSON section.

The installer keeps the `startupicon` task and the `{userstartup}\TokenHound` icon for existing installs. New `[Code]` makes silent setups without an explicit `/TASKS` leave the shortcut as it was. Interactive setups pre-select the task from the shortcut's presence and remove the shortcut when the task is left unchecked. An `[UninstallDelete]` entry removes the shortcut on uninstall.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-02 | The startup entry is `Environment.GetFolderPath(SpecialFolder.Startup)\TokenHound.lnk`, with no JSON setting | This is the path Inno's `{userstartup}\TokenHound` resolves to (`TokenHound.iss:92`). With a single source of truth, the app cannot drift from the installer or from Windows | `HKCU\...\Run`: a second mechanism, and the installer entry would stay behind. A stored setting would drift |
| DEC-02 | FR-03, NFR-01 | Create and read shortcuts through built-in COM interop: `[ComImport]` `IShellLinkW` and `IPersistFile` on `CLSID_ShellLink`, in Infrastructure | Infrastructure is `net10.0` and already uses P/Invoke. Built-in COM works in framework-dependent Windows apps, the app is not trimmed or AOT (`TokenHound.App.csproj` has no `PublishTrimmed`/`PublishAot`), and no package is added | `WScript.Shell` via `dynamic` needs `Microsoft.CSharp`. Writing the `.lnk` binary by hand is fragile |
| DEC-03 | FR-02, FR-03 | The "Startup apps" state is read from `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder`, value `TokenHound.lnk`. An odd first byte means disabled. Enabling deletes the value, which Explorer treats as enabled; the app never writes bytes there | Deleting is the least intrusive way to clear the flag. Decoding is pure logic, so it goes to Core as `StartupApprovalPolicy.IsDisabled(ReadOnlySpan<byte>)`, and Core keeps zero OS dependencies | Writing `02 00..` bytes would copy Explorer's format. Ignoring the flag would show "on" for an entry Windows has disabled |
| DEC-04 | FR-02..FR-04, NFR-01 | Seams: `IStartupLaunchService` (`IsEnabled()`, `Enable(string executablePath)`, `Disable()`) and `IStartupApprovalStore` (`ReadValue(string name)`, `DeleteValue(string name)`). `StartupLaunchService` takes the Startup folder path and an `IStartupApprovalStore`. `StartupLaunchService.CreateDefault()` wires the real folder and `RegistryStartupApprovalStore` | Tests point the service at a temp folder with a fake approval store (NFR-01), and the ViewModel tests use a fake service. The registry store is Windows-only, guarded by `OperatingSystem.IsWindows()`, so there is no CA1416 warning | A single class with hard-coded paths could not be tested without touching the user's profile |
| DEC-05 | FR-03 | `Enable` always writes the shortcut, even when one exists: target `executablePath`, working directory its folder, no arguments, description `TokenHound`. `Environment.ProcessPath` supplies the path | This rewrites a stale target (a portable copy that moved, or the installed copy while a portable one runs) with no extra target-comparison state | Comparing targets first adds reads for no visible gain |
| DEC-06 | FR-05, FR-06 | `StartupSettingsViewModel` reads `IsEnabled()` in its constructor as the baseline. `Apply()` calls `Enable`/`Disable`, then reads the state again as the new baseline. `IOException`, `UnauthorizedAccessException`, and `COMException` set `ApplyError` and are logged, and the checkbox is reset to the state read back. It is a separate file, with no WPF types, so the test project can link it | This mirrors `UpdateSettingsViewModel`, whose Apply behavior users already know. Reading back makes the checkbox reflect reality after a partial failure | Applying on checkbox change would break the pattern the other tabs use |
| DEC-07 | FR-05, UX | New `TabItem Header="General"` as the first tab of `SettingsWindow.xaml`, with a "Startup" card copied from the Updates card layout (checkbox, hint, error text, Apply and "Saved" feedback), `DataContext="{Binding Startup}"`. `SettingsViewModel.Startup` is an `init` property that may be null (hidden tab when null, like `Updates`) | The pattern exists at `SettingsWindow.xaml:497-560` and `SettingsViewModel.cs:62` | A `UserControl` would keep the XAML smaller but introduce a pattern the window does not use yet (see baseline) |
| DEC-08 | FR-07 | In `[Code]`, `InitializeSetup` records `StartupShortcutExisted := FileExists(ExpandConstant('{userstartup}\TokenHound.lnk'))`. The `[Icons]` startup entry gets `Check: ShouldCreateStartupShortcut`, which returns `True` when `not WizardSilent`, or when an explicit `/TASKS` parameter was passed, or when `StartupShortcutExisted`. `CurStepChanged(ssPostInstall)` never deletes in silent mode | The self-update passes no `/TASKS` (`InstallerUpdateLauncher.cs:18`). Inno's default `UsePreviousTasks=yes` would otherwise recreate the shortcut from the first install's choice. An explicit `/TASKS` (for example a scripted install) is still honored | `UsePreviousTasks=no` alone would stop the recreation but also stop remembering the other task (desktop icon) and would not cover interactive pre-selection |
| DEC-09 | FR-08 | In `CurPageChanged(wpSelectTasks)`, applied once (guarded by a flag), call `WizardSelectTasks('startupicon')` or `WizardSelectTasks('!startupicon')` from `StartupShortcutExisted`. In `CurStepChanged(ssPostInstall)`, when not silent and `not WizardIsTaskSelected('startupicon')`, delete the shortcut | The wizard then shows the real state, and unticking means off. Inno never deletes an icon for an unticked task by itself | Leaving it to `UsePreviousTasks` shows the first install's choice, not the current state |
| DEC-10 | FR-09 | `[UninstallDelete] Type: files; Name: "{userstartup}\TokenHound.lnk"` | Inno removes only icons it logged. This entry also covers a shortcut the app created | A `[Code]` uninstall step does the same thing with more code |
| DEC-11 | NFR-03 | All calls are synchronous on the UI thread: one `FileExists`, one registry read, and at most one shortcut write | These are local profile operations, each well under the 200 ms budget. There is no network and no polling | Async wrappers add dispatcher code with no measurable benefit |
| DEC-12 | Constraints (same behavior in both modes), US-01 | Neither the service nor the ViewModel branches on `InstallMode`: `InstallModeDetector` is not called for this feature, and the tab is shown in installed and portable copies alike | The shortcut path and the Apply logic are identical in both modes. Only the installer side (DEC-08..DEC-10) is specific to installed copies. MA-1 checks portable, and MA-3 checks the toggle in an installed copy | Hiding the option in one mode would leave portable users without the feature, or installed users without a way to change the installer's choice |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs` | New | `IsDisabled(ReadOnlySpan<byte> value)`: an empty value is not disabled, and an odd first byte is disabled | DEC-03 |
| CMP-02 | `src/TokenHound.Infrastructure/Startup/ShellLink.cs` | New | Internal COM interop: `Save(string linkPath, string targetPath, string workingDirectory, string description)` and `ReadTarget(string linkPath)`, releasing COM objects in `finally` | DEC-02 |
| CMP-03 | `src/TokenHound.Infrastructure/Startup/IStartupApprovalStore.cs` | New | `byte[]? ReadValue(string name)`, `void DeleteValue(string name)` | DEC-04 |
| CMP-04 | `src/TokenHound.Infrastructure/Startup/RegistryStartupApprovalStore.cs` | New | HKCU `StartupApproved\StartupFolder` access. A missing key or value returns `null`, delete is a no-op when the value is absent, and non-Windows returns `null` | CMP-03, DEC-03 |
| CMP-05 | `src/TokenHound.Infrastructure/Startup/IStartupLaunchService.cs` | New | `bool IsEnabled()`, `void Enable(string executablePath)`, `void Disable()` | DEC-04 |
| CMP-06 | `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs` | New | Constants `SHORTCUT_FILE_NAME = "TokenHound.lnk"`. Implements CMP-05 over a Startup folder path and CMP-03, with `CreateDefault()`. `Enable` creates the folder if missing, writes the shortcut, and clears a disabled approval value. `Disable` deletes the shortcut when present | CMP-01..05 |
| CMP-07 | `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs` | New | `IsEnabled`, `IsDirty`, `CanApply`, `ApplyCommand`, `ApplyError`, `IsApplied`, `Apply()` (DEC-06) | CMP-05 |
| CMP-08 | `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | Modified | Adds the `Startup` `init` property | CMP-07 |
| CMP-09 | `src/TokenHound.App/App.xaml.cs` | Modified | `CreateSettingsViewModel` sets `Startup = new StartupSettingsViewModel(StartupLaunchService.CreateDefault(), Environment.ProcessPath)` | CMP-06, CMP-07 |
| CMP-10 | `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | Modified | "General" tab first, with the Startup card (DEC-07) | CMP-08 |
| CMP-11 | `installer/TokenHound.iss` | Modified | DEC-08, DEC-09, DEC-10 | — |
| CMP-12 | `tests/TokenHound.Core.Tests/Policies/StartupApprovalPolicyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/StartupSettingsViewModelTests.cs`, and a `<Compile Include ... Link>` for CMP-07 in the Infrastructure test project | New and modified | TC-01..TC-09 | CMP-01..07 |

Flow: when Settings opens, `CreateSettingsViewModel` builds `StartupSettingsViewModel`, which calls `IsEnabled()`. That call checks that the file exists and that the approval value, read and decoded by `StartupApprovalPolicy`, is not disabled. When the user toggles and presses Apply, the ViewModel calls `Enable(processPath)` or `Disable()` and then `IsEnabled()` again. Nothing is kept between openings. The installer reads and writes the same file independently.

## Contracts and data

- File contract with the installer: `{userstartup}\TokenHound.lnk` (Inno `[Icons] Name: "{userstartup}\{#MyAppName}"`, with `MyAppName = "TokenHound"`). `StartupLaunchService.SHORTCUT_FILE_NAME` must stay equal to `{#MyAppName}.lnk`. A comment on each side names the other.
- Registry, read and delete only: `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder`, REG_BINARY value `TokenHound.lnk`. It is owned by Explorer, so the app never creates or writes the key.
- No JSON settings change and no `UserSettingsFile` section.

## Errors, security, and recovery

- Errors and edges: a missing Startup folder is created on `Enable`. Deleting an absent shortcut, or clearing an absent approval value, does nothing. An `IOException`, `UnauthorizedAccessException`, or `COMException` from the service goes to `ApplyError` ("Failed to update the Windows startup shortcut.") and a `Log.Warning` with the operation and exception. A null `Environment.ProcessPath` hides the tab (`Startup = null`), so a shortcut without a target is never written.
- Two copies (installed and portable): both use the same shortcut name, and the last copy that enables it becomes the target (DEC-05). Both copies show "on" while it exists. This is accepted and recorded as a risk.
- Security: per-user locations only (NFR-02). The shortcut target is the process's own path. No credentials are involved.
- Concurrency: operations are synchronous and single-threaded on the UI thread. The installer and the app do not run at the same time: setup waits for the app's instance mutex (`TokenHound.iss:107-118`).
- Rollback: `git revert` of the feature. Existing installer behavior only changes for users who removed the shortcut, whose choice is now respected. A shortcut created by the app is plain user state and can be deleted by hand.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Core policy and Infrastructure startup service (CMP-01..06) with tests | — | TC-01..TC-06 pass in Core.Tests and Infrastructure.Tests, and the build has 0 warnings |
| 2. ViewModel, Settings tab, wiring, installer (CMP-07..11) with tests | 1 | TC-07..TC-09 pass, the app build has 0 warnings, and `installer/build-installer.ps1` compiles the script. MA-1..MA-4 are pending for the visual check |

## Test approach

- Profile: `TokenHound.Core` and `TokenHound.Infrastructure` target `net10.0`, and `TokenHound.App` targets `net10.0-windows` with `UseWPF`. The tests are MTP executables, `tests/TokenHound.Core.Tests` and `tests/TokenHound.Infrastructure.Tests` (`net10.0`, with App ViewModels linked). Commands: `rtk dotnet build <project>`, then `rtk dotnet run --project <test project> --no-build --no-restore -- --minimum-expected-tests 1`, filtered with `--filter-class "*Startup*"`, and a full run of each test project before the task closes.
- E2E: omitted by .NET desktop policy.
- Prerequisites: Windows, for the COM shell-link tests (CI is `windows-latest`). Inno Setup 6 for compiling the installer through `installer/build-installer.ps1`. It is present locally at `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`.
- Manual acceptance (owner: human, at the visual check):
  - **MA-1**: Portable build → Settings → General → tick → Apply → `shell:startup` contains `TokenHound.lnk` pointing to the running exe. Sign out and back in → TokenHound starts. Untick → Apply → the shortcut is gone.
  - **MA-2**: Disable TokenHound in Windows Settings → Apps → Startup. Reopen TokenHound Settings → the option shows off. Tick → Apply → Windows Startup apps shows it enabled.
  - **MA-3**: Install with the task ticked → Settings in the installed copy shows the option on (same tab and behavior as portable, DEC-12) → untick → Apply → run a silent setup the way the self-update does (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1`) → the shortcut is still absent. Reverse case: tick in the app with the task never selected → silent setup → the shortcut is still present.
  - **MA-4**: With the shortcut present, run the setup interactively → the task is ticked. Untick and finish → the shortcut is removed. Enable in the app → uninstall → the shortcut is removed.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-02, DEC-03 | unit | `StartupApprovalPolicy.IsDisabled` with an empty value, `02 00…`, `03 00…`, `06…`, `07…` | Only odd first bytes are disabled | Core.Tests |
| TC-02 | FR-01, FR-02 | unit | Service over an empty temp folder with a fake approval store | `IsEnabled()` is false | Infrastructure.Tests |
| TC-03 | FR-01, FR-03 | integration (COM) | `Enable(exePath)` on a temp folder | `<temp>\TokenHound.lnk` exists, `ShellLink.ReadTarget` equals `exePath`, and `IsEnabled()` is true | Infrastructure.Tests |
| TC-04 | FR-03, DEC-05 | integration (COM) | A shortcut exists with target A, then `Enable(B)` | The target is B | Infrastructure.Tests |
| TC-05 | FR-02, FR-03 | unit | The fake approval store holds `03 00…`, with the shortcut present | `IsEnabled()` is false. After `Enable`, the value was deleted and `IsEnabled()` is true | Infrastructure.Tests |
| TC-06 | FR-04 | unit | `Disable()` with the shortcut present, and again with it absent | The file is gone, `IsEnabled()` is false, and no exception is thrown | Infrastructure.Tests |
| TC-07 | FR-05 | unit | ViewModel over a fake service that reports off | `CanApply` is false at load, true after `IsEnabled = true`, and false again after toggling back | Infrastructure.Tests |
| TC-08 | FR-05 | unit | Apply with the fake set to on | `Enable` is called with the given exe path, the baseline is read again, `IsApplied` is true, and `CanApply` is false | Infrastructure.Tests |
| TC-09 | FR-06 | unit | The fake's `Enable` throws `UnauthorizedAccessException`, and in a second case `IOException` | `ApplyError` is set, `IsEnabled` equals the state read back (off), and nothing propagates | Infrastructure.Tests |
| TC-10 | FR-07..FR-09, FR-01 (installer side) | manual | MA-3, MA-4, and `build-installer.ps1` compiling the script | As in the scripts | `installer/build-installer.ps1` and manual |
| TC-11 | FR-05 UX, NFR-04, FR-02 (Windows flag), DEC-12 | manual | MA-1 (portable), MA-2, MA-3 (installed copy), and the General tab with its automation names | As in the scripts | manual |

NFR-01 is verified by review: `rtk rg -n "Microsoft.Win32|System.Runtime.InteropServices" src/TokenHound.Core` returns nothing, and the tests use temp folders and fakes. NFR-02 is verified by review: the only paths are `SpecialFolder.Startup` and `HKCU`. NFR-03 is verified by MA-1 (no visible stall) and DEC-11.

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | Empty `catch` or `catch (Exception)` without handling | blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | — |
| QA-02 | `#nullable disable` / `#pragma warning disable` | blocking | `rtk rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | — |
| QA-03 | `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, or `async void` | blocking | `rtk rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | — |
| QA-04 | OS dependency in Core | blocking | `rtk rg -n 'Microsoft.Win32\|InteropServices\|System.IO' src/TokenHound.Core/Policies/StartupApprovalPolicy.cs` | — |
| QA-05 | COM object not released (`Marshal.ReleaseComObject`/`FinalReleaseComObject` in `finally`) | blocking | `rtk rg -n 'ReleaseComObject' src/TokenHound.Infrastructure/Startup/ShellLink.cs` must hit | — |
| QA-06 | `throw new Exception(` | reservation | `rtk rg -n --type cs 'throw new Exception\(' $files` | — |
| QA-07 | File above 300 lines or method above 30 lines (`CLAUDE.md`) | reservation | `rtk rg -c '^' --type cs $files` | — |

- Verification scope: the files in each task diff.
- Escalation trigger: 8 or more reservations, a touched file above 500 lines, or duplication in 3 or more places. `SettingsWindow.xaml` is already above 500 lines (baseline) and does not count as newly crossed.

### Terrain baseline

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | 176 | 6 | 4 | 0 | none | recorded |
| `src/TokenHound.App/App.xaml.cs` | 461 | 0 (partial class, private members) | 0 | 0 | none (QA-01..03 grep empty) | recorded |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 732 | n/a (XAML) | n/a | n/a | file size above 500 (pre-existing, from PRD 11) | recorded. The contact is one inserted `TabItem` (one place, about 60 lines), so no preparatory refactoring |
| `installer/TokenHound.iss` | 121 | n/a | n/a | n/a | none | recorded |
| `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` | 82 | n/a | n/a | n/a | none | recorded |

- Preparatory refactoring: not recommended. Only `SettingsWindow.xaml` crosses a structural threshold (a), and the feature touches it in one place, so there is no contact (b).

## Observability and rollout

- Signals: `Log.Information("Startup shortcut {Operation} for {ExecutablePath}", ...)` on success and `Log.Warning(ex, "Startup shortcut {Operation} failed", ...)` on failure. The registry value bytes are not logged.
- Migration and compatibility: existing installs keep their shortcut, and the app reads it as "on". No settings migration is needed.
- Rollout: ships in the next release. The new installer logic takes effect on the first self-update to this version. The setup that runs is the new one, so its `[Code]` already applies DEC-08.

## Risks and open items

- Risk: the `StartupApproved` format differs from the odd-byte assumption on some Windows build. Probability low, impact medium: the option would show on while Windows has it disabled. Mitigated by MA-2 on the dev machine. Clearing works whatever the format, because the value is deleted.
- Risk: an installed and a portable copy on one account share the shortcut (DEC-05). Probability low, impact low. Accepted.
- Risk: the installer `[Code]` behavior (silent vs interactive) cannot be unit-tested. Mitigated by compiling with the local ISCC (`%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`) through `installer/build-installer.ps1`, the same compile in `release.yml:61-66`, and MA-3/MA-4.
- Open item: none. The product decisions in the PRD Assumptions are presented at the merged HIL.

## Relevant files

- Create: `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs`, `src/TokenHound.Infrastructure/Startup/{ShellLink,IStartupApprovalStore,RegistryStartupApprovalStore,IStartupLaunchService,StartupLaunchService}.cs`, `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`, and the tests in CMP-12
- Modify: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `installer/TokenHound.iss`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
