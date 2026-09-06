# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T08 — NotchViewModel & Mock Fallback Wiring

## Outcome

Implements `NotchViewModel` under `TokenHound.App/ViewModels/` connecting `UsageStore` updates to WPF observables, with automatic fallback to `MockUsageProvider` when live Claude credentials are not found.

## Dependencies and boundaries

- Depends on: T05
- Unblocks: T09
- In scope: MVVM ViewModel implementation, observable collections, UI thread dispatching, and fallback logic.
- Out of scope: Window placement or native interop.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-10 | `prd.md#functional-requirements` | NotchViewModel with Mock fallback |
| OBJ-04 | `prd.md#outcomes-and-metrics` | Graceful offline fallback rendering |
| DEC-08 | `techspec.md#technical-decisions` | NotchViewModel with MockUsageProvider fallback |
| CMP-10 | `techspec.md#components-and-flow` | src/TokenHound.App/ViewModels/NotchViewModel.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Engine: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
- Mock Provider: `src/TokenHound.Infrastructure/Providers/Mock/MockUsageProvider.cs`

## Work

- [x] T08.1 Implement `NotchViewModel.cs` under `TokenHound.App/ViewModels/`.
- [x] T08.2 Expose `ObservableCollection<ProviderRingViewModel>` for active provider indicators.
- [x] T08.3 Subscribe to `UsageStore.SnapshotUpdated` and marshal updates to the UI thread via `Dispatcher`.
- [x] T08.4 Detect if Claude Code credentials exist; if missing, register and load `MockUsageProvider` to guarantee offline visibility.
- [x] T08.5 Implement `NotchViewModelTests.cs` verifying snapshot ingestion and mock fallback activation.

## Acceptance criteria

- Binds snapshot changes to observable properties cleanly.
- Falls back to `MockUsageProvider` without throwing unhandled exceptions.
- Fully testable in memory without opening a physical window.

## Verification

- Unit: Test ViewModel initialization with live vs mock providers.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert ViewModel wiring if property change notifications fail.

## Handoff

- Produced result:
  - Implemented `ProviderRingViewModel` (288 lines) exposing observable properties for quota fractions, dynamic badge and name resolution, rolling session (5h) and weekly (7d) countdown timers, active session process badges, and contextual status guidance messages.
  - Implemented `NotchViewModel` (188 lines) binding to `UsageStore.SnapshotUpdated` and maintaining an `ObservableCollection<ProviderRingViewModel>`. Implemented automatic offline fallback to `MockUsageProvider` when credentials or rings are missing in development mode, ensuring the floating HUD is visible immediately.
  - Implemented `NotchViewModelTests` (189 lines) covering ring addition on snapshot update, in-place updates of existing rings, Zero Fake Data preservation when capacity is unknown, mock fallback activation on construction, explicit fallback registration, and clean unsubscription on disposal.
- Changed files:
  - `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_08.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"` (Passed: 6 passed, 0 failed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 147 passed, 0 failed)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed)
- Validated state: Strict adherence to `AGENTS.md`: sealed classes, file-scoped namespaces (`TokenHound.App.ViewModels`, `TokenHound.Infrastructure.Tests.ViewModels`), alphabetized usings, XML doc comments on all public members, methods <= 30 lines, files <= 300 lines (`ProviderRingViewModel.cs`: 288 lines, `NotchViewModel.cs`: 188 lines, `NotchViewModelTests.cs`: 189 lines), max nesting <= 3 levels, UPPER_CASE constants, no `#region`, zero fake data when unmeasured, and fully testable in memory via injectable `Action<Action>? uiDispatcher`.
- Open items: None. Ready for final Tracer Bullet shell assembly in T09 (`NotchWindow` and `App.xaml`).

### ADR candidates

None.
