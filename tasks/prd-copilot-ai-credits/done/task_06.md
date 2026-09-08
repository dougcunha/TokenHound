# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T06: Show semantic credit and quota rows in the HUD

## Outcome

Copilot details show separate credit and premium-interaction rows with honest unavailable/freshness text; other providers show their actual windows. The existing ring and non-activating HUD behavior remain intact.

## Dependencies and boundaries

- Depends on: T02. Can be implemented against typed snapshots without waiting for external access.
- Unblocks: T07.
- In scope: WPF-free row projection, ViewModel notifications, tooltip bindings/templates, linked tests, and App build.
- Out of scope: backend/service composition owned by T04, new focusable controls, account picker, ring redesign, styles/animations unrelated to data rows.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-09, FR-13; NFR-03, NFR-05, NFR-06 | PRD requirements | Correct labels, quantities, provenance, dispatcher behavior, and compatibility. |
| DEC-08/09; CMP-09/10; TC-10/11/12; OI-04 | TechSpec | Row collection, existing host, unit/build and manual acceptance boundaries. |

## Context to recover on demand

- Skills: repository-cli-efficiency, dotnet-efficient-validation; use applicable UI skills during actual UI implementation.
- Existing code: ProviderRingViewModel and its Status partial, NotchViewModel, TooltipCard XAML/code-behind, ProviderRing.xaml.
- Contract: TechSpec Presentation; existing docs/design HUD source. No new visual direction is selected by this task.

## Work

- [x] T06.1 Add immutable ProviderUsageRow and pure ProviderUsageRowFactory with an injectable clock and exact decimal display.
- [x] T06.2 Generate one row per true quota window plus the independent Copilot AI-credit row, including scope, available dimensions, reset limitations, and freshness/coverage/error text.
- [x] T06.3 Expose and atomically replace the row list from ProviderRingViewModel through existing dispatcher updates. Preserve primary ring selection and real fallback category names.
- [x] T06.4 Replace fixed session/weekly TooltipCard sections/properties with data templates and update ProviderRing.xaml bindings together.
- [x] T06.5 Add text/automation labels and wrapping while preserving host width bounds, focus, click-through, and current animations.
- [x] T06.6 Link new WPF-free presentation files into Infrastructure.Tests; test credits, unknown values, decimals, source/fetch distinction, arbitrary windows, and other provider regressions.

## Acceptance criteria

- Usage-only credit data produces no percentage or 0/0. The verified 5,700/725 snapshot can show 4,975 remaining with correct provenance.
- Historical/partial/stale/prior-period data has distinct readable text. Missing reset is not borrowed from premium quota.
- Monthly credits never occupy a five-hour/weekly label; fallback non-premium quota keeps its actual category label.
- New rows do not require a WPF reference in unit tests or additional OS/UI dependencies in Core.
- XAML and App compile; runtime geometry/focus remain explicitly subject to T07.

## Verification

- Unit: TC-10 linked row-factory, ProviderRingViewModel, and NotchViewModel tests.
- Integration: App build verifies WPF bindings/types at compile time; it does not prove runtime geometry.
- E2E: omitted by .NET desktop policy.
- Manual: T07 owns M-01 through M-05 after backend integration; do not mark that evidence complete here.
- Commands: Follow the TechSpec validation profile: effective SDK 10.0.400, xUnit v3/MTP executable route, Release, restore only when needed, then build the affected test project with `--no-restore -c Release --nologo --verbosity:minimal`. Preserve each build exit code. Reuse a valid build for unchanged inputs; inspect actual execution counts and failures. Use `rtk proxy` only for hidden failure details. No publish or full solution validation is required. Build App Release with `--no-restore --nologo --verbosity:minimal` after UI edits and preserve its exit code.

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderUsageRow*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderRingViewModelTests*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
```

- Environment dependency: .NET SDK and Windows targeting assets; no GitHub account needed for row tests.
- Expected evidence: nonzero passing presentation tests, successful App build, and stated runtime acceptance gap.

## Affected files

- Create: src/TokenHound.App/ViewModels/ProviderUsageRow.cs and ProviderUsageRowFactory.cs.
- Modify: ProviderRingViewModel.cs, ProviderRingViewModel.Status.cs; UI/Controls/TooltipCard.xaml, TooltipCard.xaml.cs, ProviderRing.xaml.
- Modify: tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj for compile links.
- Create/modify: Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs, ProviderRingViewModelTests.cs, NotchViewModelTests.cs as needed.
- Read-only regression targets: App/Interop/WindowStyles.cs and existing HUD host/placement code.

## Observability and recovery

- Signal: readable source-specific status and metadata; no global badge misleadingly asserted to describe both sources.
- Recovery: revert row binding/control/ViewModel changes together. Keep independent billing state intact; no geometry or credential migration.

## Handoff

> Updated by `sdd-orchestrate-tasks` / `sdd-execute-task` during implementation.

- Produced result: Semantic quota and credit usage row architecture and presentation in the TokenHound HUD. Implemented WPF-free `ProviderUsageRow` immutable model and pure `ProviderUsageRowFactory` (with Copilot partial). Projects true operational quota windows (preserving 'Premium interactions' for finite Copilot and actual category labels for non-premium/other providers) and independent Copilot AI-credit rows. Direct billing with verified allowance renders signed remaining balance, total, and direct provenance; usage-only renders exact credits used with unmeasured/hidden progress fill and no 0/0 or percentage. Historical fallback renders estimated provenance, reporting cutoff date, and partial coverage breakdown without borrowing reset countdown from operational quota. Forwarded `Rows` collection on `ProviderRingViewModel` with atomic replacement and notification. Refactored `TooltipCard.xaml` and `TooltipCard.xaml.cs` to use dynamic `ItemsControl` with DataTemplate, `UsedFractionToBrushConverter`, flat progress bar, and WCAG AA compliant wrapping text blocks while keeping backward-compatible fallback for legacy bindings. Linked presentation files into `TokenHound.Infrastructure.Tests` without WPF dependencies.
- Changed files:
  - `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`
  - `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`
  - `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs`
  - `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`
  - `src/TokenHound.App/UI/Controls/UsedFractionToBrushConverter.cs`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml`
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotErrorTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs`
  - `tasks/prd-copilot-ai-credits/task_06.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderUsageRow*"` -> 19 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderRingViewModelTests*"` -> 18 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"` -> 7 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` -> 451 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` -> 77 passed, 0 failed.
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk git diff --check` -> Clean, 0 whitespace errors.
- Validated state: Release configuration, net10.0 and net10.0-windows, zero compile warnings/errors, all 451 Infrastructure tests and 77 Core tests passing. C# invariants verified (all files and classes <= 300 lines, methods <= 30 lines, nesting <= 3 levels, sealed classes, XML docs on public members, antislop Mode 1 rules applied with no em dashes in UI text).
- Open items:
  - `OI-04` remains open: manual desktop acceptance with visible Windows desktop and MCP tools for geometry/focus checks (to be validated in T07).

### ADR candidates

None - direct TechSpec implementation adhering to architectural boundaries, storage contracts, and UI design guidelines.

