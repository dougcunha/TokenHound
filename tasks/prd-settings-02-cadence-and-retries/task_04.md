# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-02-cadence-and-retries/prd.md`
2. `tasks/prd-settings-02-cadence-and-retries/techspec.md`
3. This file

---

# T04 - Apply validated cadence settings through the view model

## Outcome

The Settings presentation model loads active values, validates integer cadence/retry input as it changes, resets to policy defaults, applies only a valid set atomically in application order, and discards un-applied edits.

## Dependencies and boundaries

- Depends on: T01, T02, T03
- Unblocks: T05
- In scope: `CadenceSettingsViewModel`, its composition with `SettingsViewModel`, headless test linking, validation/error state, reset, apply, discard, and persistence-failure feedback contract.
- Out of scope: XAML layout, window focus behavior, startup loading, provider-toggle semantics, and silently rolling back a successfully persisted configuration.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01 to OBJ-03, OBJ-06 | `prd.md#outcomes-and-metrics` | Editing, safety feedback, and defaults |
| FR-01 to FR-07, FR-11 | `prd.md#functional-requirements` | Section state, immediate validation, save gating, reset, and discard |
| NFR-02, NFR-05, NFR-06 | `prd.md#non-functional-requirements` | Floors, non-blocking flow, and size/complexity limits |
| DEC-01, DEC-02, DEC-06, CMP-06, CMP-07, CMP-13 | `techspec.md#technical-decisions`, `techspec.md#components-and-flow` | Child VM and test strategy |
| TC-05 to TC-08 | `techspec.md#test-approach` | Validation, relationship, reset, and discard tests |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.App/ViewModels/SettingsViewModel.cs` - provider toggles apply immediately; cadence apply/discard must not alter that contract.
- Existing code: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` - links App view models for headless testing.
- Contract or integration: `techspec.md#cadencesettingsviewmodel-interface` and the completed contracts from T01 to T03.

## Work

- [ ] T04.1 Create a framework-independent `CadenceSettingsViewModel` with string/int input handling that treats empty and non-numeric values as errors, reports the specified accessible messages, and exposes `HasErrors`, dirty state, and Apply eligibility.
- [ ] T04.2 Implement cross-field validation: active/idle must be at least 30 seconds, retry floor at least 60 seconds, and idle must not be shorter than active; recompute all affected errors on every edit.
- [ ] T04.3 Implement Reset to policy constants, Discard to the most recently active persisted values, and Apply in failure-safe order: persist the complete valid settings, then update runtime cadence and effective floor only after persistence succeeds.
- [ ] T04.4 Compose the child VM without growing `SettingsViewModel` past repository limits; link the new file into Infrastructure tests and add headless unit coverage for validation, reset, apply, discard, and persistence failure.

## Acceptance criteria

- Invalid, empty, non-numeric, sub-floor, or relationally inconsistent values immediately produce the specified field error and prevent Apply.
- Reset sets 180/300/60 from policy constants, and Discard restores the current persisted/active snapshot without changing runtime state.
- Apply never changes `UsageStore` or `RateLimitPolicy` when either persistence operation fails; successful apply updates both runtime targets without UI-thread blocking.

## Verification

- Unit: `CadenceSettingsViewModelTests` proves the TechSpec cases plus persistence-failure non-application.
- Integration: Use real T01/T02/T03 contracts with isolated stores; do not substitute a fake for the file-write atomicity contract.
- E2E: Omitted by .NET desktop policy.
- Manual: MAN-01 and MAN-03 are completed in T05; owner: implementer.
- Commands: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*CadenceSettingsViewModelTests*"`
- Environment dependency: T01, T02, T03 complete; .NET SDK 10.0.400 and restored Infrastructure test output.
- Expected evidence: MTP runs at least one selected test and passes; tests show no runtime mutation after persistence failure.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create: `src/TokenHound.App/ViewModels/CadenceSettingsViewModel.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/CadenceSettingsViewModelTests.cs`

## Observability and recovery

- Operational signal: Expose a presentation error state for failed Apply; the application boundary can log the underlying persistence failure once.
- Recovery: Correct the input or filesystem failure and retry Apply; Cancel/Discard restores the captured active values.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: P-01/T03 completion is required before execution.

### ADR candidates

Pending execution. `sdd-execute-task` replaces this text with structured candidates or `None - direct TechSpec implementation or local decision`.
