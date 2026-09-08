# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T07 — Apply stored enablement at startup and close end-to-end acceptance

## Outcome

TokenHound reads the `"Providers"` section at startup and applies it to the engine *before* the HUD view model is built, so only enabled providers ever get a ring or a poll. The Settings dialog receives a live view model. The full journey works across a restart, and the manual script confirms the invariants that unit tests cannot reach.

## Dependencies and boundaries

- Depends on: T01, T02, T04, T06
- Unblocks: —
- In scope: `App.xaml.cs` startup sequencing, the view-model factory handed to `DialogService`, the default `"Providers"` section in the shipped `appsettings.json`, and the manual acceptance run.
- Out of scope: everything already delivered by T01 – T06. No new behavior is introduced here beyond wiring.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-07 | `prd.md#functional-requirements` | Startup reads enablement; missing entries default to enabled; only enabled providers get polling and rings |
| FR-10 | `prd.md#functional-requirements` | All-disabled leaves a clean standby capsule with no fallback mock |
| OBJ-04 | `prd.md#outcomes-and-metrics` | Toggles persist across restarts, preserving `Hud`, `Refresh`, `Log` |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Non-activating HUD behavior preserved across dialog use |
| NFR-03 | `prd.md#non-functional-requirements` | `WM_MOUSEACTIVATE` / `MA_NOACTIVATE` invariant survives opening, using, and closing Settings |
| NFR-04 | `prd.md#non-functional-requirements` | Toggle response under 50 ms; no measurable overhead for disabled providers |
| NFR-06 | `prd.md#non-functional-requirements` | No credential read or network call for a disabled provider |
| US-01, US-04, US-05 | `prd.md#stories-and-journeys` | Disable unused tools, re-enable on demand, retain configuration across restarts |
| DEC-08 | `techspec.md#technical-decisions` | Enablement applied between `RegisterProviders` and `InitializeUi` |
| CMP-15 | `techspec.md#components-and-flow` | `App.xaml.cs` wiring |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`
- Existing code: `src/TokenHound.App/App.xaml.cs` — `OnStartup` runs `CreateUsageStore` → `RegisterProviders` → `InitializeUi` → `ScheduleInitialRefresh`; `InitializeUi` constructs `DialogService`, `NotchViewModel`, `ApplicationLifetime`, and `HudActionsViewModel`
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStore.cs` — the constructor calls `LoadArchive()`, which populates `_snapshots` from disk; this is *why* the gate must be applied before `NotchViewModel` is constructed, or disabled rings flash on every restart
- Existing code: `src/TokenHound.App/appsettings.json` — the shipped file with `Hud`, `Refresh`, and `Log`; it is copied to output with `PreserveNewest`
- Contract or integration: `techspec.md#components-and-flow` — the Startup and Zero-enabled flow paragraphs

## Work

- [x] T07.1 Load `ProviderSettings` in `OnStartup` and call `SetProviderEnabled` for each registered id, placed after `RegisterProviders` and before `InitializeUi`.
- [x] T07.2 Log the resolved enablement per provider with structured arguments, matching the existing cadence log.
- [x] T07.3 Pass a `Func<SettingsViewModel>` factory into `DialogService` so `HudActionsViewModel`'s Settings action opens a live dialog.
- [x] T07.4 Add a default `"Providers"` section to the shipped `appsettings.json` with every current provider enabled.
- [x] T07.5 Execute MAN-03, MAN-04, and MAN-05 and record the results in the handoff. Executed by the orchestrator on the primary Windows 11 desktop; MAN-03 and MAN-04 pass, MAN-05 partially observed (see the manual-acceptance table appended to the Handoff).

## Acceptance criteria

- With `gemini` and `codex` disabled in `appsettings.json`, a fresh launch shows rings only for the remaining providers — no flash of a disabled ring during startup.
- A settings file with no `"Providers"` section launches with every provider enabled.
- Opening Settings from the HUD context menu shows the populated dialog; toggling a provider updates the HUD immediately and the file on disk.
- After a restart, the toggles and rings match what was left; `Hud`, `Refresh`, and `Log` are intact.
- With every provider disabled, the capsule stays visible, draggable, and right-clickable, and no mock ring appears.
- No fetch or fault log entry is emitted for a disabled provider across two full polling cycles.
- HUD click and drag still never steal focus from an active editor after Settings has been opened and closed.

## Verification

- Unit: none new. Startup sequencing is not unit-testable without launching the application, which the desktop policy excludes; the components it wires are already covered by TC-01 – TC-15 in T01 – T05.
- Integration: none new.
- E2E: omitted by .NET desktop policy. No full-application launch test, no WPF UI automation.
- Manual: MAN-03 (open Settings with an editor focused, toggle, close, then click and drag the HUD — NFR-03, OBJ-05), MAN-04 (disable two providers, inspect `appsettings.json`, restart, reopen — OBJ-04, US-05, FR-06, FR-07), MAN-05 (toggle off, watch the HUD and then the log for two polling cycles — OBJ-01, NFR-04, NFR-06). Owner: Douglas Cunha. Launch via Windows MCP `App` with `mode="launch_executable"` per `AGENTS.md`; capture with Windows MCP `Screenshot`, `display: [2]`.
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: a real Windows 11 desktop session with Windows MCP for MAN-03 – MAN-05, plus at least one authenticated provider to observe a non-`Checking` badge. Authorization exists — the owner runs these locally. MAN-05 is the only evidence that closes the substitution gap recorded in T02: NSubstitute proves the dispatch decision, not that a real adapter opens no socket or SQLite handle.
- Expected evidence: both test projects green with non-zero counts; the App project builds; MAN-03 – MAN-05 recorded with screenshots and the log excerpt showing no activity for the disabled providers.

## Affected files

- Modify: `src/TokenHound.App/App.xaml.cs`
- Modify: `src/TokenHound.App/appsettings.json`

## Observability and recovery

- Operational signal: a structured startup log line per provider with its resolved enablement, alongside the existing cadence line. The absence of `ProviderFaultLog` entries for a disabled provider is the MAN-05 evidence.
- Recovery: deleting the `"Providers"` section restores all-enabled behavior on the next launch. Reverting the feature leaves an inert section that older builds ignore.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: T07.1 – T07.4 implemented (code deliverable only). `App.OnStartup` now runs `CreateUsageStore` -> `RegisterProviders` -> **`ApplyProviderEnablement`** -> `InitializeUi` -> `ScheduleInitialRefresh`, satisfying DEC-08: the gate is applied after registration (so `RegisteredProviderIds` is populated) and before `NotchViewModel` is constructed (so no ring is built for a disabled provider restored from the archive). `ApplyProviderEnablement` loads `ProviderSettings` through a new `readonly ProviderSettingsStore _providerSettingsStore = new()` field, logs the resolved file path, then calls `UsageStore.SetProviderEnabled(id, settings.IsEnabled(id))` for every registered id and emits one structured line per provider, `"Provider enablement resolved: {ProviderId} monitored {IsEnabled}"` — an explicit line for every provider, not only for the ones whose state actually changed (`SetProviderEnabled` logs only on a change). The Settings action handed to `HudActionsViewModel` now passes the view-model factory as the second argument of `DialogService.ShowSettings`, closing the silent-failure gap recorded in the T06 -> T07 handoff. The shipped `appsettings.json` gained a `"Providers"` section listing all five registered ids enabled.
- Changed files:
  - `src/TokenHound.App/App.xaml.cs` (+37/-1) — `_providerSettingsStore` field; `ApplyProviderEnablement(UsageStore)`; `CreateSettingsViewModel(UsageStore)`; `OnStartup` sequencing; the `ShowSettings` call now supplies the factory.
  - `src/TokenHound.App/appsettings.json` (+17/-0) — new `"Providers"` section inserted between `Hud` and `Refresh`; `Hud`, `Refresh`, and `Log` untouched.
- Factory code path (traced end to end, T07.3): `NotchWindow.xaml.cs:144` `_actionsViewModel?.ShowSettings()` -> `HudActionsViewModel.ShowSettings()` (`HudActionsViewModel.cs:186-192`) invokes the injected `_showSettings` (third constructor parameter) -> `App.xaml.cs:204` `() => _dialogService.ShowSettings(_notchWindow, () => CreateSettingsViewModel(usageStore))` -> `DialogService.ShowSettings(Window?, Func<SettingsViewModel>?)` (`DialogService.cs:48`) -> `CreateSettingsWindow(effectiveOwner, viewModelFactory)` (`DialogService.cs:71`) -> `viewModelFactory?.Invoke()` (`DialogService.cs:184`) -> `window.DataContext = viewModel` (`DialogService.cs:187`) -> `SettingsWindow.xaml:137` `ItemsSource="{Binding Providers}"`. The factory builds `new SettingsViewModel(usageStore, _providerSettingsStore.SaveAsync, DispatchUiAction)`, so toggles persist through the same store the startup gate reads and marshal to the WPF dispatcher.
- Provider ids confirmed against `RegisterProviders` and the adapters' `PROVIDER_ID` constants: `claude` (`ClaudeOAuthProvider.cs:21`), `gemini` (`AntigravityUsageProvider.cs:15` — canonical id, **not** `antigravity`, per DEC-02/R-2), `codex` (`CodexUsageProvider.cs:14`), `cursor` (`CursorUsageProvider.cs:15`), `copilot` (`CopilotUsageProvider.cs:19`).
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` -> `ok dotnet build: 4 projects, 0 errors, 0 warnings (00:00:03.26)`, exit code `0`.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> `ok dotnet test: 407 tests passed, 0 warnings in 1 projects (2.4 s)`, exit code `0`.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> `ok dotnet test: 45 tests passed, 0 warnings in 1 projects (592 ms)`, exit code `0`.
  - Encoding guard (T04 regression): `head -c 3` returns `efbbbf` for `appsettings.json` (BOM present at `HEAD`, preserved) and `757369` for `App.xaml.cs` (no BOM at `HEAD`, preserved). The diff shows no line-1 churn in either file.
  - `appsettings.json` re-parsed after the edit; top-level keys are `['Hud', 'Providers', 'Refresh', 'Log']`.
  - No new unit or integration tests: per the task's Verification section, startup sequencing is not unit-testable without launching the application, which the .NET desktop policy excludes. E2E omitted by the same policy. The wired components are already covered by TC-01 – TC-15 in T01 – T05.
  - No pre-existing failures observed in either suite.
- Validated state: code/diff of the two files above; configuration `Debug`, `net10.0-windows` (App) and `net10.0` (Core, Infrastructure); projects `TokenHound.App`, `TokenHound.Infrastructure.Tests`, `TokenHound.Core.Tests`; environment Windows 11 Pro 10.0.26200, worktree `settings` branch, restore assets already present (no `NETSDK1004`).
- Open items:
  - **T07.5 is NOT executed. MAN-03, MAN-04, and MAN-05 remain PENDING.** No application launch was performed by this executor; the caller runs the full manual acceptance script in one session together with T06's outstanding MAN-01 and MAN-02.
  - Acceptance still closed only by that manual pass: FR-07 no-flash startup, FR-10 all-disabled standby capsule, OBJ-04 restart persistence, OBJ-05 and NFR-03 non-activating HUD across dialog use, NFR-04 sub-50 ms toggle, NFR-06 no fetch or fault log entry for a disabled provider across two polling cycles.
  - OPEN-03 (`Disabled` pill contrast 3.11:1, below WCAG AA for normal text) is inherited from T06 and unresolved; decision owner Douglas Cunha. Not affected by this task.


### Manual acceptance executed (orchestrator, 2026-09-08, primary display 3440x1440 @ 100%)

Build under test: worktree `Debug/net10.0-windows`, launched via Windows MCP `App` (`mode="launch_executable"`), pids 25940 then 88824. The owner's installed instance at `D:\Apps\TokenHound` (pid 37172) was identified by window rect and process path and was never touched. The test instance was stopped afterwards.

| ID | Verdict | Evidence |
| --- | --- | --- |
| MAN-01 | **Pass** | Dialog opened from the HUD context menu, populated with five rows in name order (Antigravity, Claude Code, Codex, Copilot, Cursor). Space toggled the FIRST row without any prior Tab, confirming T06.7 initial focus. Two Tabs moved focus row-by-row to Codex with a visible focus ring; Space toggled it. Esc dismissed the dialog and the app stayed alive (only the HUD window remained). The Close button also dismissed it. Cursor rendered a live `Needs Auth` pill, not `Checking`, so the resolver reached a real snapshot. |
| MAN-02 | **Partial** | Dark Fluent surface rendered with no light-mode flash; glyphs, names, pills and toggles legible and unclipped at **100%** (display 3, the primary). **125%, 150% and 200% were NOT verified** — changing the owner's display scaling is invasive and was not done. NFR-01 stays partially open. |
| MAN-03 | **Pass** | `GetForegroundWindow` sampled before and after clicking the HUD capsule, both on a cold HUD and after a full open-toggle-close Settings cycle. Foreground pid never became the app's; it stayed on the unrelated editor process. `MA_NOACTIVATE` holds. |
| MAN-04 | **Pass** | Disabled Antigravity and Codex through the dialog; `appsettings.json` showed `gemini:false, codex:false` written under the **canonical** key with `Hud`, `Refresh` and `Log` structurally intact (25 `Log` keys preserved across four write cycles). Restart showed exactly three rings and the reopened dialog matched the persisted state. Startup log: `Provider enablement resolved: gemini monitored false` / `codex monitored false`. Re-enabling restored the ring at its ORIGINAL leading index, not appended, and the badge returned straight to `OK` rather than `Checking`, confirming DEC-09 snapshot retention. |
| MAN-05 | **Partial** | Ring disappeared with no perceptible delay on toggle-off. Across the post-restart window the only log entries for the disabled providers were T02's gating lines (`Provider gemini monitoring changed to false`); no fetch, fault or credential-read entry appeared for either. **However the observation window was roughly 3 minutes, not the two full 180 s polling cycles the script requires**, so NFR-04 and NFR-06 are supported but not fully closed. |

Additionally not directly observed: DEC-08's *no-flash* guarantee. The post-restart end state is correct (three rings, disabled providers absent) and the startup ordering is correct in code, but a transient flash during window construction would not be caught by a post-hoc screenshot.

### ADR candidates

None - direct TechSpec implementation. DEC-08 already fixes the startup ordering and its alternative (filtering inside `NotchViewModel`), and the factory parameter was decided in T06. The only local choices — holding `ProviderSettingsStore` as a readonly field so both the startup load and the dialog's `SaveAsync` share one resolved path, and passing the factory as a closure over the `usageStore` parameter rather than the nullable `_usageStore` field to avoid a null guard in a UI callback — are implementation details with no durable contract impact.
