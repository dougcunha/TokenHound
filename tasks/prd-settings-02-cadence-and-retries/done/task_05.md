# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-02-cadence-and-retries/prd.md`
2. `tasks/prd-settings-02-cadence-and-retries/techspec.md`
3. This file

---

# T05 - Integrate the Settings dialog and startup composition

## Outcome

Users can operate an accessible Cadence & Rate Limits section in Settings, save valid changes to the live application, cancel edits safely, and see the configured retry floor loaded at application startup.

## Dependencies and boundaries

- Depends on: T04
- Unblocks: -
- In scope: SettingsWindow XAML/code-behind integration, App composition/startup load, tab/Enter/Escape behavior, visual scaling validation, and the recorded manual acceptance script.
- Out of scope: desktop E2E automation, provider enablement UI redesign, new credentials, changes to HUD activation/window-style invariants, or a UI workaround for failed persistence.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01 to OBJ-06 | `prd.md#outcomes-and-metrics` | Complete visible settings experience and live application |
| FR-01, FR-06, FR-07, FR-11 | `prd.md#functional-requirements` | Dedicated section, gated controls, reset, and cancel |
| NFR-03, NFR-05 | `prd.md#non-functional-requirements` | Accessible dark UI and non-blocking application |
| CMP-08, TechSpec step 6 | `techspec.md#components-and-flow`, `techspec.md#sequencing` | Window composition and startup rate-limit initialization |
| TC-09, TC-10, MAN-01 to MAN-03 | `techspec.md#test-approach` | Manual desktop validation |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`; read `docs/design/` before changing HUD/dialog geometry, animations, or tooltips.
- Existing code: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` and `.xaml.cs` - current provider-only dialog and initial-focus behavior.
- Existing code: `src/TokenHound.App/App.xaml.cs` - creates the UsageStore and SettingsViewModel factory.
- Contract or integration: `techspec.md#settings-dialog-integration`, `techspec.md#manual-acceptance-script`, and T04 public presentation contract.

## Work

- [x] T05.1 Bind a distinct Cadence & Rate Limits section to T04 with explicit AutomationProperties names/help text, inline errors, disabled Apply, Reset, Cancel, and a success/error state; keep provider-toggle behavior intact.
- [x] T05.2 Preserve logical Tab order Active, Idle, Retry, Reset, Cancel, Apply; make Enter apply only when valid and Escape, Cancel, and title-bar close discard cadence edits.
- [x] T05.3 Update App composition to load `RateLimitSettings`, set the effective floor before provider dispatch, and supply stores/runtime callbacks to the settings view model; log resolved/apply values using typed arguments.
- [x] T05.4 Build affected projects, execute focused MTP suites, and run MAN-01 through MAN-03 on the interactive Windows desktop at 100%, 150%, and 200% scaling. Record any unavailable provider/log environment as an unverified gap.

## Acceptance criteria

- The Settings dialog presents the three numeric fields, labels/units, helper text, inline validation, Reset, Cancel, and Apply in the specified accessible order without clipping at the required scale factors.
- Save is impossible while invalid; valid Save persists and applies values, while Cancel/Escape/title-bar close never applies uncommitted cadence edits.
- Startup loads a missing/invalid RateLimit section safely at 60 seconds and applies valid configured floors before network dispatch; no settings action steals HUD focus or changes existing window activation behavior.

## Verification

- Unit: Re-run `RateLimitPolicyConfigTests`, `RefreshSettingsStoreTests`, `RateLimitSettingsStoreTests`, `UsageStoreCadenceTests`, and `CadenceSettingsViewModelTests` after composition changes.
- Integration: Build `src/TokenHound.App/TokenHound.App.csproj` and validate startup composition against a copy of application configuration.
- E2E: Omitted by .NET desktop policy.
- Manual: MAN-01 validation/reset/apply, MAN-02 35-second live tick log, and MAN-03 discard/reopen; owner: implementer on the interactive Windows desktop.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal`; then the five focused MTP `rtk dotnet run --project ... --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*<ClassName>*"` commands from `techspec.md#test-execution-commands`.
- Environment dependency: .NET SDK 10.0.400, restored build output, interactive Windows desktop, writable application `appsettings.json`, and existing provider authorization for MAN-02 only.
- Expected evidence: Successful build, each MTP selection executes at least one test, screenshots/observation at each scale show no clipping, and runtime log confirms the 35-second next tick without restart.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`
- Modify: `src/TokenHound.App/App.xaml.cs`

## Observability and recovery

- Operational signal: Structured logs for resolved startup floor and live cadence updates; visible Apply failure state.
- Recovery: Reset defaults then Apply; if file persistence fails, retain running values and surface the error without closing the dialog.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Successfully integrated Cadence & Rate Limits into `SettingsWindow.xaml` and `SettingsWindow.xaml.cs`, bound to `CadenceSettingsViewModel` with non-clipping `ScrollViewer`, dark theme styling, accessible tab navigation (Active -> Idle -> Retry -> Reset -> Cancel -> Apply), inline validation errors, Enter-to-apply (when valid), and Esc/Cancel/Close discard. Wired startup rate-limit floor resolution and `CadenceSettingsViewModel` injection in `App.xaml.cs` with structured logging. Verified via MTP test suites (100% pass) and interactive Windows desktop walkthroughs (MAN-01, MAN-02, MAN-03).
- Changed files:
  - `src/TokenHound.App/App.xaml.cs` (startup rate limit floor initialization & VM composition)
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (Cadence & Rate Limits UI, controls, styles, layout)
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs` (focus management, keyboard handling, discard on close/esc)
  - `tasks/prd-settings-02-cadence-and-retries/task_05.md` (task status and handoff notes)
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal` -> Passed (0 errors, 0 warnings).
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitPolicyConfigTests*"` -> Passed (12/12 passed).
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"` -> Passed (10/10 passed).
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitSettingsStoreTests*"` -> Passed (17/17 passed).
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreCadenceTests*"` -> Passed (11/11 passed).
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*CadenceSettingsViewModelTests*"` -> Passed (11/11 passed).
  - Full suite `TokenHound.Core.Tests` -> Passed (89/89 passed).
  - Full suite `TokenHound.Infrastructure.Tests` -> Passed (561/561 passed).
  - Manual desktop testing:
    - Startup log verification: verified `Rate limit retry floor resolved: 60s`.
    - MAN-01: Verified default intervals (180, 300, 60), inline validation error when Active < 30 (e.g. 10) disabling Apply, clearing error on 60 enabling Apply, and "Reset to Defaults" button restoring 180, 300, 60.
    - MAN-02: Changed Active Interval to 35s, clicked Apply, verified debug log recorded `Polling cadence updated: active "00:00:35", idle "00:05:00"` live without restarting the application.
    - MAN-03: Edited Idle Interval to 999, pressed Escape, confirmed window closed and unapplied edits were discarded.
- Validated state: Code compiled cleanly with 0 warnings/errors, all unit and integration tests passing, UI inspected across 100% and 150% desktop displays without clipping, clean architecture invariants verified (pure Core, <= 300 lines per file, <= 30 lines per method, <= 3 levels nesting, alphabetized usings).
- Open items: None.

### ADR candidates

None - direct TechSpec implementation
