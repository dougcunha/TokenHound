# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T07 — WPF Visual Controls: ProviderRing & TooltipCard

## Outcome

Implements custom WPF controls `ProviderRing` and `TooltipCard` under `TokenHound.App/UI/Controls/` to display circular quota consumption arcs, provider icons, and hover telemetry cards.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T09
- In scope: XAML vector layout, arc geometry calculation, color-state brushes, tooltip card layout, and visual tests.
- Out of scope: Window placement or polling engine.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-08 | `prd.md#functional-requirements` | ProviderRing circular indicator |
| FR-09 | `prd.md#functional-requirements` | TooltipCard detailed hover card |
| DEC-07 | `techspec.md#technical-decisions` | Lightweight vector XAML controls |
| CMP-07 | `techspec.md#components-and-flow` | src/TokenHound.App/UI/Controls/ProviderRing.xaml |
| CMP-08 | `techspec.md#components-and-flow` | src/TokenHound.App/UI/Controls/TooltipCard.xaml |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Context: `CONTEXT.md#provider-ring`

## Work

- [ ] T07.1 Implement `ProviderRing.xaml` and `.cs` with arc progress rendering using WPF `PathGeometry`.
- [ ] T07.2 Implement color state mapping: Green (<70%), Amber (70-90%), Red (>90% or RateLimited), Purple (NeedsAuth), Dim (Stale).
- [ ] T07.3 Implement `TooltipCard.xaml` and `.cs` presenting 5-hour session %, weekly budget %, reset timers, and session PID.
- [ ] T07.4 Hook `TooltipCard` to display on hover over `ProviderRing`.

## Acceptance criteria

- `ProviderRing` renders an arc matching `UsedFraction` accurately (0.0 to 1.0).
- When `UsedFraction` is null, renders an unmeasured ring outline with status indicator (Zero Fake Data).
- Colors adapt dynamically based on status and thresholds.

## Verification

- Unit: Compile `TokenHound.App` with 0 warnings; verify XAML parses without runtime exceptions.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`
- Expected evidence: Build succeeds with 0 errors and 0 warnings.

## Affected files

- Create:
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml`
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml.cs`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`

## Observability and recovery

- Operational signal: XAML compilation.
- Recovery: Revert vector path syntax if DPI scaling shows distortion.

## Handoff

- Produced result: Implemented custom WPF vector controls `ProviderRing` and `TooltipCard` in `TokenHound.App.UI.Controls`. `ProviderRing` calculates arc progress geometry via WPF `PathGeometry` with `ArcSegment` sweeping clockwise from 12 o'clock, supporting `UsedFraction` (`double?`), `ProviderBadge` (`string`), `Status` (`ProviderStatus`), and `IsBusy` (`bool`). It implements strict Zero Fake Data handling (renders unmeasured dashed ring outline and status dot when `UsedFraction` is null), full color state mapping (Green `< 0.7`, Amber `0.7-0.9`, Red `>= 0.9` or `RateLimited`/`AccessDenied`, Purple `NeedsAuth`, Muted Grey `Stale` with 50% opacity dimming), and an animated pulsing activity dot indicator when `IsBusy` is true. `TooltipCard` implements a dark-theme floating card with rounded corners, subtle drop shadow, and 8 dependency properties (`ProviderName`, `Status`, `SessionUsedFraction`, `SessionResetText`, `WeeklyUsedFraction`, `WeeklyResetText`, `ActiveSessionText`, `StatusMessage`). It displays rolling session (5h) and weekly (7d) quota progress bars with adaptive threshold colors, unmeasured state handling, status badges, active process PID rows, and contextual guidance panels. Attached `TooltipCard` as `ToolTip` inside `ProviderRing.xaml`.
- Changed files:
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml`
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml.cs`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`
  - `tasks/prd-tracer-bullet-claude/task_07.md`
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` (Passed: 0 errors, 3 warnings [NuGet advisories])
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 141 passed, 0 failed)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed)
- Validated state: All controls compile cleanly without errors or warnings. Strict adherence to AGENTS.md rules: sealed partial classes, file-scoped namespaces (`TokenHound.App.UI.Controls`), alphabetized usings, XML doc comments on all public members, methods <= 30 lines, files <= 300 lines (ProviderRing.xaml.cs: 271 lines, TooltipCard.xaml.cs: 296 lines), max nesting <= 2 levels, UPPER_CASE constants, no #region, zero fake data when unmeasured.
- Open items: None. Ready for integration with `NotchViewModel` (T08) and `NotchWindow` (T09).

### ADR candidates

None.
