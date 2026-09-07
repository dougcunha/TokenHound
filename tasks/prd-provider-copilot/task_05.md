# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. T02, T03, and T04 are the completed dependencies before execution.

---

# T05 - Integrate Copilot into the existing App and Notch

## Outcome

Copilot is registered through the existing composition root and uses the existing Snapshot, Provider Ring, status, activity, and non-activating Notch behavior without a Copilot-specific HUD surface or deferred product decision.

## Dependencies and boundaries

- Depends on: T02, T03, and T04
- Unblocks: T06
- In scope: App composition, lifetime disposal, ActivityUpdated subscription, generic Unsupported/ActiveBlock status mapping, existing Copilot catalog/glyph reuse, and App/ViewModel tests.
- Out of scope: new Copilot UI, new identity-only/metadata contract, launch metrics, exact fractional/status copy, archive expiry, changes to WM_MOUSEACTIVATE, or visual redesign.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-12 | `prd.md#functional-requirements` | Feed provider identity/data and all Copilot status/activity journeys into existing presentation. |
| NFR-02, NFR-06, NFR-07 | `prd.md#non-functional-requirements` | Preserve honest values, architecture boundaries, and HUD non-interference. |
| AC-01, AC-03, AC-08, AC-10 | `prd.md#acceptance-criteria` | Show finite/unsupported/overage/activity states through existing surfaces. |
| DEC-01, DEC-08, DEC-10 | `techspec.md#technical-decisions` | Reuse contracts, route activity, and preserve all HIL1 deferrals. |
| CMP-10 | `techspec.md#components-and-flow` | Own App and Notch integration. |
| TC-09, TC-10, TC-11 | `techspec.md#test-approach` | Prove event routing, Core compatibility, App build, and manual HUD behavior. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: `src/TokenHound.App/App.xaml.cs`, `NotchViewModel.cs`, `ProviderRingViewModel.cs`, `ProviderCatalog.cs`, `ProviderRing.xaml.cs`, and `TooltipCard.xaml.cs`.
- Contract or integration: TechSpec section `Existing Notch integration`; repository invariants for WM_MOUSEACTIVATE and SWP_NOZORDER.

## Work

- [x] T05.1 Register CopilotUsageProvider, CopilotActivityMonitor, archive, and lifetime resources in the existing App composition root with cancellation/disposal ownership.
- [x] T05.2 Subscribe NotchViewModel to ActivityUpdated and marshal updates through the existing UI dispatcher to `ProviderRingViewModel.UpdateActivity`.
- [x] T05.3 Map Unsupported, ActiveBlock, overage, stale, and sign-in outcomes through existing generic status/tooltip paths without inventing copy or a new identity-only state.
- [x] T05.4 Reuse existing Copilot ProviderCatalog/glyph entries and preserve existing Provider Ring fraction/reset conventions; do not add metadata fields or a provider-specific HUD.
- [x] T05.5 Add ViewModel/App integration tests for registration, event routing, generic statuses, cancellation, and source-compatible existing providers.
- [x] T05.6 Review NotchWindow focus/non-activation code and confirm no change to WM_MOUSEACTIVATE, WS_EX_NOACTIVATE, click-through geometry, or SWP_NOZORDER behavior.

## Acceptance criteria

- Copilot appears through the existing provider ring/catalog path with official fidelity and finite values supplied by T03.
- Busy/Idle changes reach the existing ring, and Unsupported, stale, auth, overage, and exhausted states remain explicit instead of becoming zero/current data.
- App startup and shutdown dispose provider, monitor, archive, timers, and subscriptions cleanly.
- No provider-specific surface, identity-only contract, launch metric, or deferred formatting/layout decision is added.
- Existing non-activating and click-through behavior remains unchanged and is ready for manual confirmation in T06.

## Verification

- Unit: ProviderRingViewModel and NotchViewModel event/status tests with fake snapshots/activity events and dispatcher.
- Integration: App composition/build compiles all registrations and existing provider paths; no live Copilot request required.
- E2E: omitted by .NET desktop policy.
- Manual: T06 owns launch, primary-monitor Screenshot display `[2]`, foreground-app interaction, and activity transitions.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`; focused existing ViewModel tests with `--minimum-expected-tests 1` after `--`.
- Environment dependency: Windows WPF target, .NET SDK 10.0.400, existing provider registrations, and no new package dependency.
- Expected evidence: test output, App build exit code 0, unchanged focus-hook diff, and an integration review showing only generic presentation changes.

## Affected files

- Modify: `src/TokenHound.App/App.xaml.cs`
- Modify: `src/TokenHound.App/ViewModels/NotchViewModel.cs`
- Modify: `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Reuse without modification: `src/TokenHound.App/UI/Controls/ProviderRing.xaml.cs`
- Reuse without modification: `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`

## Observability and recovery

- Operational signal: existing SnapshotUpdated, ActivityUpdated, ProviderRing state, and generic ErrorDescription/UsageBlock; no new product telemetry.
- Recovery: remove only Copilot registrations/subscriptions and ignore TokenHound-owned Copilot archive keys. Do not alter user-owned credential or session files.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: T05 complete. Copilot is registered through the existing App composition root and uses the existing snapshot, provider-ring, generic status, activity, and lifetime paths.
- Changed files: `src/TokenHound.App/App.xaml.cs`; `src/TokenHound.App/ViewModels/NotchViewModel.cs`; `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs`.
- Checks: Core build exit 0; Infrastructure build exit 0 with the pre-existing NU1903 SQLite advisory; App build exit 0 with the same advisory; Core tests passed with 43 tests; Copilot and UsageStore filters each passed with 24 tests; Infrastructure test project passed with 324 tests; `git diff --check` passed.
- Validated state: App creates the TokenHound-owned archive, registers and disposes Copilot provider/monitor resources, starts both store timers after composition, and routes activity changes through the existing UI dispatcher. Copilot reuses the existing catalog/glyph and Provider Ring status paths. `NotchWindow.xaml.cs` and `WindowStyles.cs` are unchanged, preserving WM_MOUSEACTIVATE, WS_EX_NOACTIVATE, click-through geometry, and SWP_NOZORDER behavior. Desktop E2E was omitted under the .NET desktop policy.
- Open items: T06 still requires Windows MCP launch, primary-monitor Screenshot display `[2]`, live approved-session activity transition, and foreground focus/drag confirmation. IP-01 remains the explicit plaintext-config fallback gap recorded by T03; HIL1-01 through HIL1-06 remain deferred.

### ADR candidates

None - direct TechSpec implementation or local decision.
