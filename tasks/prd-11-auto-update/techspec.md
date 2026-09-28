# TechSpec — Auto-update from GitHub Releases

## Sources and traceability

- PRD: `tasks/prd-11-auto-update/prd.md`
- Applicable instructions, rules, and skills: `AGENTS.md`/`CLAUDE.md` (pure Core, 429 persistence invariant, JSON settings, C# style), `ARCHITECTURE.md` §2 (layers, rate-limit resilience), `dotnet-efficient-validation`, `sdd-create-techspec` references (`dotnet.md`, `quality-dotnet.md`, `preparatory-refactoring.md`).
- Specs and design: no `docs/specs/` entry covers self-update; the dialog reuses `UI/Styles/DialogResources.xaml` (not HUD geometry, so `docs/design/` does not apply).
- Evidence in existing code:
  - Version source: `src/TokenHound.App/Presentation/ApplicationInfo.cs:67-95` reads `AssemblyInformationalVersionAttribute`; the release build passes `-p:Version=<tag without v>` (`.github/workflows/release.yml` step "Publish single-file").
  - Release assets: `release.yml` "Pack artifact" (`TokenHound-{ver}-{rid}-fxdependent.zip`), `installer/TokenHound.iss:58` (`TokenHound-Setup-{ver}-{rid}.exe`), RIDs `win-x64`/`win-arm64`, release upload `dist/*.zip` + `dist/*Setup*.exe`, tags `v*`.
  - Installer: `installer/TokenHound.iss` — `PrivilegesRequired=lowest` (56), `DefaultDirName={autopf}\TokenHound` (52), `[Files]` (83-84), `[Run]` with `skipifsilent` (91).
  - Rate limits: `src/TokenHound.Core/Policies/RateLimitPolicy.cs` (`CanDispatch` 58-65, `CalculateDeadline` 78-109, Retry-After floor), persisted-gate pattern `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs:17-278` + `Engine/CopilotHttpArchive.cs`, atomic writes `Engine/AtomicJsonFile.cs`.
  - Settings: `Configuration/SectionStore.cs`, `RefreshSettingsStore.cs:93-99` (section factory), `UserSettings.cs:11-39`.
  - Tray: `UI/Tray/TrayMenuItemKey.cs`, `TrayMenuModel.cs:41-54`, `TrayIconViewModel.Invoke:80-112`, `ITrayIcon.cs`, `TaskbarIconAdapter.cs:44-110`, `TrayIconHost.cs:59-138`.
  - Composition and shutdown: `App.xaml.cs:44-72` (`OnStartup`), `359-448` (`InitializeUi`, `InitializeTray`, `ShutdownAsync`), `App.Mcp.cs` (partial-class precedent), `ApplicationLifetime.ShutdownAsync:95-108`.
  - Dialog pattern: `UI/Windows/ProviderStatusDialog.cs` (activate-or-create, owned view model).
  - Tests link App sources: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj:41-72` (`Compile Include … Link=`).

## Solution summary

A pure Core layer decides *whether* to update (version comparison, prerelease filtering, skip-version, schedule due-ness, asset selection, digest parsing). Infrastructure owns *how*: an unauthenticated GitHub client for `releases/latest`, a persisted rate-limit gate for that endpoint, a downloader that verifies size and SHA-256 digest, an install-mode detector (installer marker + writability probe), and two appliers — portable in-place swap with a journal and rollback, and a silent Inno Setup launch. The App wires a scheduler, a tray entry, a balloon notification, and a single update dialog whose view model drives check → prompt → download → apply.

Restart safety uses a named per-session mutex held for the process lifetime: the relaunched portable exe (`--updated`) and the Inno installer (`[Code]`) both wait for it to be released before touching files or initializing. Nothing is applied before integrity passes; a failed portable start restores the previous files.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, NFR-01 | Running version = `AssemblyInformationalVersion` with any `+metadata` suffix removed, parsed by a Core `ReleaseVersion` (`MAJOR.MINOR.PATCH[-pre]`). An unparseable current version (e.g. "Version unavailable") disables update checks and logs once. | `ApplicationInfo.cs:67-95`; release passes `-p:Version`; .NET SDK appends `+<commit>` to informational version. | `Assembly.GetName().Version` loses prerelease suffix; rejected. |
| DEC-02 | FR-01, FR-02 | Tag `v1.2.3` → `ReleaseVersion`; comparison is numeric on MAJOR.MINOR.PATCH, a prerelease suffix sorts below the same release; `prerelease: true` or `draft: true` releases are ignored. | PRD FR-02 decision; `/releases/latest` already excludes drafts/prereleases, the filter is defensive. | Full SemVer library: new dependency for three numbers; rejected. |
| DEC-03 | FR-03, NFR-03 | `UpdateSettings` section `Update` in `settings.json`: `Enabled` (default true), `CheckIntervalHours` (default 24, accepted 1–720; 0 = periodic off), `SkippedVersion` (string?). Due-ness: `now - lastCheckUtc >= interval` with `lastCheckUtc` persisted, first evaluation 60 s after startup, scheduler tick 5 min; the scheduler reloads settings each tick so edits apply without restart. | `SectionStore` pattern; persisted `lastCheckUtc` keeps restarts from spending the 60/h budget. | Fixed timer per interval: loses the due time across restarts; rejected. |
| DEC-04 | FR-12, NFR-03 | `UpdateRequestGate` mirrors `CopilotRequestGate`: persisted `deadlineUtc` + `consecutiveFailures` in `%LOCALAPPDATA%\TokenHound\update-state.json` (with `lastCheckUtc`), `RateLimitPolicy.CanDispatch` before every call, deadline = max(`RateLimitPolicy.CalculateDeadline(now, Retry-After, failures)`, `X-RateLimit-Reset`) on 429 or on 403 with `X-RateLimit-Remaining: 0`. Persistence failure blocks further checks for the process. Success resets failures. | `CopilotRequestGate.cs:180-236`; `RateLimitPolicy` already floors `Retry-After: 0`. Separate file avoids growing `UsageArchive` (240 lines) and its shared state file. | Reuse `UsageArchive.SaveBackoffDeadlineAsync`: couples a non-provider to provider state; rejected. |
| DEC-05 | FR-06 | Install mode = presence of `TokenHound.installed` next to the exe (`AppContext.BaseDirectory`); installed by `TokenHound.iss` `[Files]` from a new repo file `installer/TokenHound.installed`, so the uninstaller removes it; the zip never contains it. | Marker approach decided at HIL 0 (`workflow.md#DEC-01`). | Registry uninstall key lookup: OS coupling and user-hive ambiguity; rejected. |
| DEC-06 | FR-07 | Asset selection by `RuntimeInformation.ProcessArchitecture` (`X64`→`win-x64`, `Arm64`→`win-arm64`, other → no asset): portable `^TokenHound-.+-{rid}-fxdependent\.zip$`, installed `^TokenHound-Setup-.+-{rid}\.exe$` (case-insensitive). No match → `UpdateAssetMissing` error, no download. | `release.yml` / `TokenHound.iss` names. | OS architecture instead of process: an x64 process under emulation would switch arch; rejected (keep what runs). |
| DEC-07 | FR-08, NFR-02 | Integrity: downloaded length must equal asset `size`; when the asset has `digest` = `sha256:<hex>`, the SHA-256 of the file must match. Mismatch deletes the file and reports; nothing is applied. Download URL must be `https://github.com/{owner}/{repo}/releases/download/...` from the API response; redirects are followed only over HTTPS. | PRD FR-08; GitHub release asset `digest` field, when present; `HttpClient` default redirect handling. | Separate checksum file in the release: out of scope (PRD). |
| DEC-08 | FR-09 | Portable apply in process: extract zip to a staging folder; for each staged file, rename the existing file to `<name>.old` (a running exe can be renamed on NTFS), move the staged file in, write a swap journal (`%LOCALAPPDATA%\TokenHound\updates\swap-journal.json`); start the new exe with `--updated`; if the start throws, restore all `.old` files and report. The relaunched process waits up to 30 s for the instance mutex, then deletes the journal's `.old` files and the journal. | No helper script or second binary; rollback path is local and testable. | PowerShell/cmd helper after exit: shell quoting and AV noise; rejected. |
| DEC-09 | FR-10, NFR-05 | Installed apply: launch the verified setup with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1`, then shut down gracefully. `TokenHound.iss` gains `[Code]`: `InitializeSetup` waits up to 30 s while `CheckForMutexes('TokenHound.App.Instance')`; a `[Run]` entry `Flags: nowait; Check: ShouldRelaunch` (`{param:RELAUNCH|0} = '1'`) restarts the app. The existing interactive `postinstall skipifsilent` entry is unchanged. | `PrivilegesRequired=lowest` keeps it per-user without UAC; `skipifsilent` (iss:91) prevents relaunch otherwise. | `/CLOSEAPPLICATIONS`: Restart Manager close is intercepted by the HUD's hide-on-close; rejected. |
| DEC-10 | FR-09, FR-10 | `ApplicationInstanceMutex` (Infrastructure/System): named mutex `Local\TokenHound.App.Instance` acquired at startup and held until exit; does not enforce single instance (current behavior preserved); `--updated` makes startup wait for it (30 s) before initializing. `AbandonedMutexException` counts as acquired. | Needed by DEC-08/DEC-09; no existing mutex (`grep Mutex` empty). | Wait on PID: installer cannot receive it without extra plumbing; rejected. |
| DEC-11 | FR-11 | Before downloading in portable mode, probe writability by creating and deleting `~update-probe.tmp` in the exe folder; failure → message with the release page URL, no download, app keeps running. | PRD FR-11. | Check ACLs: complex and misses read-only media; rejected. |
| DEC-12 | FR-04, FR-05, FR-13, UX | One `UpdateWindow` (states: Checking, UpToDate, Available, Downloading with progress + Cancel, Applying, Error) driven by `UpdateViewModel`. Manual check opens it directly; a periodic hit shows a tray balloon (no focus steal) whose click opens it. Actions: **Update now**, **Later** (close; periodic check asks again), **Skip this version** (persist `SkippedVersion`). `UpdateCheckService` shares one in-flight check task between manual and periodic callers. | `ITrayIcon` boundary + Hardcodet `ShowBalloonTip`/`TrayBalloonTipClicked`; `ProviderStatusDialog` pattern. | Modal MessageBox: cannot show progress/cancel and steals focus; rejected. |
| DEC-13 | Quality | New App wiring goes into partial `App.Updates.cs` (as `App.Mcp.cs`), keeping `App.xaml.cs` growth to call sites; interval editing goes to a new `UpdateSettingsViewModel` + "Updates" tab rather than `CadenceSettingsViewModel` (299 lines). | Baseline: `App.xaml.cs` 449 lines, `CadenceSettingsViewModel.cs` 299 lines vs the 300-line rule. | Extend existing classes: breaks the file-size rule; rejected. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Core/Models/ReleaseVersion.cs` | New | Parse tag/informational version, compare | — |
| CMP-02 | `src/TokenHound.Core/Models/ReleaseInfo.cs`, `ReleaseAsset.cs`, `InstallMode.cs`, `UpdateCheckOutcome.cs` | New | Immutable release/asset DTOs (tag, version, prerelease, draft, html URL; name, size, download URL, digest), install mode enum, check outcome (UpToDate, Available, Skipped, Unavailable + reason) | CMP-01 |
| CMP-03 | `src/TokenHound.Core/Policies/UpdatePolicy.cs` | New | `Evaluate(current, release, skippedVersion)` → outcome; `IsCheckDue(now, lastCheckUtc, interval, enabled)` | CMP-01, CMP-02 |
| CMP-04 | `src/TokenHound.Core/Policies/UpdateAssetSelector.cs` | New | Select asset per mode × architecture (DEC-06); parse `sha256:` digest | CMP-02 |
| CMP-05 | `src/TokenHound.Infrastructure/Configuration/UpdateSettings.cs`, `UpdateSettingsStore.cs`, `UserSettings.cs` | New / modified | `Update` section with clamping (DEC-03) | `SectionStore` |
| CMP-06 | `src/TokenHound.Infrastructure/Updates/UpdateStateStore.cs` | New | `update-state.json`: `lastCheckUtc`, `deadlineUtc`, `consecutiveFailures` via `AtomicJsonFile` | — |
| CMP-07 | `src/TokenHound.Infrastructure/Updates/UpdateRequestGate.cs` | New | Persisted 429/403 gate (DEC-04) | CMP-06, `RateLimitPolicy` |
| CMP-08 | `src/TokenHound.Infrastructure/Updates/GitHubReleaseClient.cs` | New | `GET https://api.github.com/repos/dougcunha/TokenHound/releases/latest` with `User-Agent`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`, 15 s timeout; map JSON → `ReleaseInfo`; 404 → no release; 429/403-limit → `UpdateRateLimitedException(retryAfter, resetUtc)` | CMP-02, `HttpClient` |
| CMP-09 | `src/TokenHound.Infrastructure/Updates/UpdateCheckService.cs` | New | Gate → client → policy; single in-flight task; updates `lastCheckUtc`; structured logs | CMP-03, CMP-05, CMP-07, CMP-08 |
| CMP-10 | `src/TokenHound.Infrastructure/Updates/UpdateDownloader.cs` | New | Stream asset to `%LOCALAPPDATA%\TokenHound\updates\<ver>\`, `IProgress<double>`, cancellation, verify (DEC-07) | CMP-02, CMP-04 |
| CMP-11 | `src/TokenHound.Infrastructure/Updates/InstallModeDetector.cs` | New | Marker detection (DEC-05) and writability probe (DEC-11) | CMP-02 |
| CMP-12 | `src/TokenHound.Infrastructure/Updates/PortableUpdateApplier.cs`, `UpdateSwapJournal.cs` | New | Extract, swap, journal, rollback, startup cleanup (DEC-08) | CMP-10 |
| CMP-13 | `src/TokenHound.Infrastructure/Updates/InstallerUpdateLauncher.cs` | New | Start setup with silent args (DEC-09) | CMP-10 |
| CMP-14 | `src/TokenHound.Infrastructure/System/ApplicationInstanceMutex.cs` | New | Named mutex acquire/wait/release (DEC-10) | — |
| CMP-15 | `src/TokenHound.Infrastructure/Updates/UpdateCheckScheduler.cs` | New | `TimeProvider`-driven loop: 60 s initial delay, 5 min tick, reload settings, raise `UpdateAvailable` | CMP-03, CMP-05, CMP-09 |
| CMP-16 | `src/TokenHound.App/ViewModels/UpdateViewModel.cs`, `UpdateSettingsViewModel.cs` | New | Dialog state machine and commands; interval/enable editing with validation | CMP-09..CMP-13 via delegates |
| CMP-17 | `src/TokenHound.App/UI/Windows/UpdateWindow.xaml(.cs)`, `UpdateDialog.cs` | New | Dialog view, activate-or-create | CMP-16 |
| CMP-18 | `src/TokenHound.App/UI/Tray/*` (`TrayMenuItemKey`, `TrayMenuModel`, `TrayIconViewModel`, `ITrayIcon`, `TaskbarIconAdapter`, `TrayIconHost`) | Modified | `CheckForUpdates` entry ("Check for Updates…", before About); `ShowNotification(title, text)` + `NotificationClicked` | CMP-16 |
| CMP-19 | `src/TokenHound.App/App.Updates.cs`, `App.xaml.cs`, `SettingsViewModel.cs`, `SettingsWindow.xaml` | New / modified | Composition, `--updated` startup wait + cleanup, scheduler lifetime, "Updates" settings tab | all above |
| CMP-20 | `installer/TokenHound.iss`, `installer/TokenHound.installed` | Modified / new | Marker file, mutex wait `[Code]`, silent relaunch `[Run]` (DEC-05, DEC-09) | — |

Flow: startup → mutex (wait if `--updated`) → swap-journal cleanup → scheduler starts. Tick or tray click → `UpdateCheckService.CheckAsync` (gate → GitHub → policy). `Available` → balloon (periodic) or dialog (manual). **Update now** → mode detect → (portable) writability probe → download + verify → apply (portable swap + start `--updated`, or setup launch) → `ShutdownAsync`. Any failure → dialog Error state, app keeps running.

## Contracts and data

- `settings.json` section `Update` (new, optional; missing = defaults):
  ```json
  "Update": { "Enabled": true, "CheckIntervalHours": 24, "SkippedVersion": "1.4.0" }
  ```
  `CheckIntervalHours`: int, 0 disables periodic checks, 1–720 accepted, other values clamp to the nearest bound; `SkippedVersion`: normalized version without `v`, null when none. Unknown sections are preserved by `UserSettings.ExtensionData`.
- `%LOCALAPPDATA%\TokenHound\update-state.json` (new): `{ "lastCheckUtc": ISO-8601?, "deadlineUtc": ISO-8601?, "consecutiveFailures": int }`; unreadable file = empty state for `lastCheckUtc`, but a corrupt deadline field is treated conservatively like `CopilotHttpArchive` (keep any parseable deadline).
- `%LOCALAPPDATA%\TokenHound\updates\swap-journal.json` (new): `{ "appDirectory": string, "files": [string], "createdUtc": ISO-8601 }`.
- GitHub response fields read: `tag_name`, `prerelease`, `draft`, `html_url`, `assets[].name|size|browser_download_url|digest`.
- Command line: `--updated` (App), setup `/RELAUNCH=1` (installer).

## Integrations and interfaces

- GitHub REST `releases/latest`: unauthenticated, 60 req/h per IP; 200 → release; 404 → no published release (UpToDate); 429 / 403 with `X-RateLimit-Remaining: 0` → gate deadline; other non-success → error, no deadline; timeout 15 s. Download: `browser_download_url`, no timeout on the body beyond cancellation; progress from `Content-Length`/asset size.
- Tray: new menu entry and balloon notification through `ITrayIcon`.
- File system: exe folder (swap, probe), `%LOCALAPPDATA%\TokenHound\updates\`.
- Processes: new exe (`--updated`) or setup exe; both started with `UseShellExecute = false` and absolute paths.

## Errors, security, and recovery

- Errors and edges: rate limited → dialog shows local time when checks resume, no call; offline/timeout → Error with retry; no matching asset → Error with release page link; digest/size mismatch → file deleted, Error; non-writable portable folder → Error with release page link; setup start failure → Error, app keeps running; portable start failure → rollback then Error.
- Security: HTTPS only; download URL prefix check (DEC-07); integrity before any execution; no token stored; per-user install, no elevation.
- Concurrency and idempotency: one in-flight check; Update now disabled while downloading; the swap journal makes cleanup idempotent; a second `--updated` start after cleanup is a no-op.
- Rollback or reversal: portable failure restores `.old` files; installed failure leaves the current install (Inno is transactional per file); user can reinstall an older release manually.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Core models and policies (CMP-01..04) | — | Core unit tests TC-01..TC-06 pass |
| 2. Settings + state store + gate + GitHub client + check service (CMP-05..09) | 1 | Infrastructure tests TC-07..TC-11 pass |
| 3. Downloader, mode detector, appliers, mutex, journal (CMP-10..14) | 1 | TC-12..TC-16 pass |
| 4. Scheduler (CMP-15) | 2 | TC-17 passes |
| 5. App: view models, dialog, tray, settings tab, composition (CMP-16..19) | 2, 3, 4 | TC-18..TC-21 pass; app builds |
| 6. Installer script + marker (CMP-20) | 3 | `installer/build-installer.ps1` builds; MA-3 |

## Test approach

- Profile: Core `net10.0` (`tests/TokenHound.Core.Tests`), Infrastructure `net10.0` (`tests/TokenHound.Infrastructure.Tests`, also compiles linked App view models/tray types), App `net10.0-windows` WPF (`UseWPF`, `App.xaml.cs`) — desktop. Runner: Microsoft.Testing.Platform with xUnit v3 (`UseMicrosoftTestingPlatformRunner`, `xunit.v3.mtp-v2`). Commands (per `AGENTS.md`):
  - `rtk dotnet build TokenHound.slnx --no-restore`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- E2E: omitted by .NET desktop policy.
- Command prerequisites and exclusions: no network in tests (mocked `HttpMessageHandler`, pattern `AntigravityCloudCodeClient.cs:24-49`); temp directories for file tests; `TimeProvider` fakes (`ManualTimeProvider` in tests). New App files that tests exercise are linked in the test csproj.
- Manual acceptance (owner: human, at the visual check and HIL 3; requires a published GitHub release newer than the running build, or a local build with a lowered `-p:Version`):
  - **MA-1** Tray → "Check for Updates…" on the latest version → dialog shows "up to date". With network off → specific error; app keeps running.
  - **MA-2** Portable: run a zip build with `-p:Version=0.0.1` from a writable folder → check → Update now → progress → app closes and reopens on the latest version; `.old` files gone after restart. Repeat from a read-only folder → not-writable message, no download.
  - **MA-3** Installed: install an older setup → check → Update now → no wizard pages, app restarts on the new version; Apps & Features shows the new version; uninstall removes `TokenHound.installed`.
  - **MA-4** Periodic: set interval 1 h, clear `lastCheckUtc` → within ~1 min of start a balloon appears without taking focus; clicking it opens the dialog; **Skip this version** → no balloon on next due check.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01, DEC-01 | unit | Parse `v1.2.3`, `1.2.3+abc`, `1.2.3-beta.1`, garbage | Parsed values; garbage → false | Core.Tests |
| TC-02 | FR-01, DEC-02 | unit | Compare newer/equal/older/prerelease-vs-release | Correct ordering | Core.Tests |
| TC-03 | FR-01, FR-02, FR-05 | unit | `UpdatePolicy.Evaluate` with newer, equal, older, prerelease, draft, skipped tag, newer-than-skipped | Available only for newer non-prerelease non-skipped | Core.Tests |
| TC-04 | FR-03 | unit | `IsCheckDue`: null last check, before/after interval, disabled, interval 0 | Due exactly when enabled and elapsed | Core.Tests |
| TC-05 | FR-07, DEC-06 | unit | Asset selection for {portable, installed} × {x64, arm64}, missing asset, unsupported arch | Correct asset or none | Core.Tests |
| TC-06 | FR-08 | unit | Digest parse `sha256:<hex>`, other algorithm, malformed | Hex or null | Core.Tests |
| TC-07 | FR-03, NFR-06 | unit | `UpdateSettingsStore` round-trip, defaults, clamping, other sections preserved | JSON as in Contracts | Infrastructure.Tests |
| TC-08 | FR-12 | unit | Gate: 429 Retry-After 120, 429 Retry-After 0, 403 remaining 0 with reset, deadline persisted and reloaded, dispatch refused before deadline, persistence failure blocks process | No call before deadline; `Retry-After: 0` never immediate | Infrastructure.Tests |
| TC-09 | FR-01, FR-02, NFR-02 | unit | `GitHubReleaseClient` maps JSON; sends User-Agent/API headers; 404 → none; 500 → error | Mapped `ReleaseInfo` / errors | Infrastructure.Tests |
| TC-10 | FR-13 | unit | Two concurrent `CheckAsync` calls | One HTTP request, same result | Infrastructure.Tests |
| TC-11 | FR-03, FR-12 | unit | Check service updates `lastCheckUtc` on success and on up-to-date; not on rate limit | State file values | Infrastructure.Tests |
| TC-12 | FR-08, NFR-02 | unit | Downloader: size mismatch, digest mismatch, digest absent + size ok, non-github URL, cancellation | Mismatch deletes file; bad URL refused; cancel cleans up | Infrastructure.Tests |
| TC-13 | FR-06, FR-11 | unit | Detector with/without marker; probe on writable and read-only temp dir | Mode and writability as expected | Infrastructure.Tests |
| TC-14 | FR-09 | integration (temp dir) | Portable swap success, start failure → rollback, journal cleanup idempotent | Files and journal in expected state | Infrastructure.Tests |
| TC-15 | FR-10 | unit | Installer launcher builds exact args and absolute path; start failure surfaces | `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RELAUNCH=1` | Infrastructure.Tests |
| TC-16 | DEC-10 | integration | Mutex acquire, second acquire times out, release lets waiter proceed, abandoned counts as acquired | As described | Infrastructure.Tests |
| TC-17 | FR-03 | unit | Scheduler with fake `TimeProvider`: initial delay, tick, settings change applies next tick, disabled | Checks only when due | Infrastructure.Tests |
| TC-18 | FR-04, FR-05, FR-13, UX | unit | `UpdateViewModel` state transitions: check → up to date / available / error / rate limited; Later; Skip persists; Update now disables re-entry; cancel download | States and persisted skip | Infrastructure.Tests (linked) |
| TC-19 | FR-03, UX | unit | `UpdateSettingsViewModel` validation and apply | Invalid input blocked; valid persisted | Infrastructure.Tests (linked) |
| TC-20 | FR-04, UX | unit | Tray menu contains "Check for Updates…" before About; `Invoke(CheckForUpdates)` calls the action | Order and dispatch | Infrastructure.Tests |
| TC-21 | FR-05, UX | unit | `TrayIconHost` forwards `ShowNotification` and `NotificationClicked` | Fake tray receives calls | Infrastructure.Tests |
| MA-1..MA-4 | FR-04, FR-05, FR-06, FR-09, FR-10, FR-11, OBJ-02, UX | manual | See script above | See script | human |

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No `async void` outside event handlers; no `.Result`/`.Wait()`/`GetAwaiter().GetResult()` | blocking | `rtk rg -n --type cs @src 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | — (mutex `WaitOne` is a sync kernel wait, not a task wait) |
| QA-02 | No empty `catch` | blocking | `rtk rg -n --type cs @src 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | — |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rtk rg -n --type cs @src '#nullable disable\|#pragma warning disable' $files` | — |
| QA-04 | Uninjected clock in logic (`DateTime.Now/UtcNow`) — use `TimeProvider` | reservation | `rtk rg -n --type cs @src 'DateTime\.(Now\|UtcNow)' $files` | — |
| QA-05 | Generic `throw new Exception(` | reservation | `rtk rg -n --type cs @src 'throw new Exception\(' $files` | — |
| QA-06 | Files ≤ 300 lines (repo rule), methods ≤ 30 lines | reservation | `rtk rg -c '^' --type cs @src $files \| Sort-Object { [int]($_ -split ':')[-1] } -Descending \| Select-Object -First 5` | `App.xaml.cs` pre-existing (baseline) |
| QA-07 | `.ConfigureAwait(false)` on awaits in Core/Infrastructure | reservation | `rtk rg -n --type cs @src 'await (?!.*ConfigureAwait)' $files` (Infrastructure files only) | — |
| QA-08 | Parameter lists of 4+ → record | reservation | `rtk rg -n --type cs @src '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | — |

- Verification scope: files in the task diff.
- Escalation trigger: 8+ reservations, a touched file above 500 lines, or duplication in 3+ places.

### Terrain baseline

Measured at `a8bd1bf` (public-member count by the reference regex, which undercounts properties).

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/App.xaml.cs` | 449 | 0 | 0 | 0 | `QA-06: file 449 lines > 300` | absorbed in `DEC-13` (new partial `App.Updates.cs`) |
| `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs` | 25 | 0 | — | 0 | none | recorded |
| `src/TokenHound.App/UI/Tray/TrayMenuModel.cs` | 85 | 2 | — | 0 | none | recorded |
| `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs` | 180 | 8 | 4 | 6 | none | recorded |
| `src/TokenHound.App/UI/Tray/TrayIconHost.cs` | 142 | 2 | 5 | 0 | none | recorded |
| `src/TokenHound.App/UI/Tray/ITrayIcon.cs` | 41 | 0 | — | 0 | none | recorded |
| `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs` | 137 | 3 | 0 | 0 | none | recorded |
| `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | 173 | 1 | — | 0 | none | recorded |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 623 | — | — | — | XAML above 500 lines | recorded (one new `TabItem`) |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs` | 179 | 0 | — | 0 | none | recorded |
| `src/TokenHound.Infrastructure/Configuration/UserSettings.cs` | 108 | 1 | — | 0 | none | recorded |
| `installer/TokenHound.iss` | 92 | — | — | — | none | recorded |
| `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` | — | — | — | — | none | recorded (new `Compile Include` links) |

- Preparatory refactoring: not recommended. The only structural hit (`App.xaml.cs` over the 300-line repo rule; below the 500-line threshold) is avoided by a partial file (DEC-13); every other target is small and gets one or two contact points.

## Observability and rollout

- Signals: Serilog structured events `UpdateCheckStarted {Trigger}`, `UpdateCheckCompleted {Outcome} {CurrentVersion} {LatestVersion}`, `UpdateRateLimited {DeadlineUtc} {Failures}`, `UpdateDownloadCompleted {Asset} {Bytes} {DigestVerified}`, `UpdateApplyStarted {Mode}`, `UpdateApplyFailed {Mode} {Error}`, `UpdateCleanupCompleted {Files}`.
- Migration and compatibility: new optional settings section and new state files; older builds ignore them. Users on versions without this feature update manually once.
- Rollout and rollback: ships in the next `v*` release; the first auto-update is exercised by MA-2/MA-3 against a later release. Disable by `Update.Enabled=false`.

## Risks and open items

- Risk (medium probability, high impact): renaming the running single-file exe fails on some setups (AV lock, non-NTFS) → swap aborts before any change and the user sees an Error with the release link (FR-09 rollback path); MA-2 verifies.
- Risk (low, medium): Inno `CheckForMutexes` wait times out if shutdown hangs > 30 s → setup proceeds and may hit files in use; the app's shutdown drain already bounds work; MA-3 verifies.
- Risk (low, low): GitHub may omit `digest` on older assets → size-only verification (accepted by PRD FR-08).
- Open item: confirm at the merged HIL the PRD defaults (prereleases ignored, 24 h, size + optional SHA-256) and DEC-08/DEC-09 mechanisms — owner: human; affects FR-02, FR-03, FR-08, FR-09, FR-10.

## Relevant files

- Modify: `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`, `ITrayIcon.cs`, `TaskbarIconAdapter.cs`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`, `installer/TokenHound.iss`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, existing tray tests (`TrayMenuModelTests`, `TrayIconViewModelTests`, `TrayIconHostTests`).
- Create: `src/TokenHound.Core/Models/{ReleaseVersion,ReleaseInfo,ReleaseAsset,InstallMode,UpdateCheckOutcome}.cs`, `src/TokenHound.Core/Policies/{UpdatePolicy,UpdateAssetSelector}.cs`, `src/TokenHound.Infrastructure/Configuration/{UpdateSettings,UpdateSettingsStore}.cs`, `src/TokenHound.Infrastructure/Updates/*.cs` (CMP-06..13, CMP-15), `src/TokenHound.Infrastructure/System/ApplicationInstanceMutex.cs`, `src/TokenHound.App/App.Updates.cs`, `src/TokenHound.App/ViewModels/{UpdateViewModel,UpdateSettingsViewModel}.cs`, `src/TokenHound.App/UI/Windows/{UpdateWindow.xaml,UpdateWindow.xaml.cs,UpdateDialog.cs}`, `installer/TokenHound.installed`, matching test files.
