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

- [ ] T05.1 Bind a distinct Cadence & Rate Limits section to T04 with explicit AutomationProperties names/help text, inline errors, disabled Apply, Reset, Cancel, and a success/error state; keep provider-toggle behavior intact.
- [ ] T05.2 Preserve logical Tab order Active, Idle, Retry, Reset, Cancel, Apply; make Enter apply only when valid and Escape, Cancel, and title-bar close discard cadence edits.
- [ ] T05.3 Update App composition to load `RateLimitSettings`, set the effective floor before provider dispatch, and supply stores/runtime callbacks to the settings view model; log resolved/apply values using typed arguments.
- [ ] T05.4 Build affected projects, execute focused MTP suites, and run MAN-01 through MAN-03 on the interactive Windows desktop at 100%, 150%, and 200% scaling. Record any unavailable provider/log environment as an unverified gap.

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

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: P-01 must have been resolved and T01 through T04 complete.

### ADR candidates

Pending execution. `sdd-execute-task` replaces this text with structured candidates or `None - direct TechSpec implementation or local decision`.
