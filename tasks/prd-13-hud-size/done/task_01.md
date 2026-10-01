# Stable execution context

Load in this exact order:

1. `tasks/prd-13-hud-size/prd.md`
2. `tasks/prd-13-hud-size/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — HUD size setting and ViewModel

## Outcome

The HUD size can be persisted and edited without any UI wiring: a `HudSize` settings section with clamped bounds, a shared `HudScale` holder, and a ViewModel that supports the slider, the three presets, and Apply, all verified by unit tests.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: `HudSizeSettings`, `HudSizeStore`, the `UserSettings.HudSize` property, `HudScale`, `HudSizeSettingsViewModel`, `SettingsViewModel.HudSize`, the test csproj links, and the unit tests.
- Out of scope: any XAML, `NotchWindow`, `App.xaml.cs` wiring, and placement.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Bounds, step, default, clamp |
| FR-02, FR-03 | `prd.md#functional-requirements` | Presets, label, Apply pattern |
| FR-07 | `prd.md#functional-requirements` | Own settings section |
| DEC-01, DEC-02, DEC-03, DEC-07, DEC-08 | `techspec.md#technical-decisions` | Section, bounds, `HudScale`, ViewModel, exposure |
| CMP-01..CMP-06 | `techspec.md#components-and-flow` | Components |
| TC-01..TC-07 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `CLAUDE.md` C# style, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `RefreshSettings.cs` and `RefreshSettingsStore.cs` (record and `SectionStore` pattern), `UserSettings.cs` (section registration), `StartupSettingsViewModel.cs` and `StartupSettingsViewModelTests.cs` (Apply pattern and test shape), `RefreshSettingsStoreTests.cs` (temp settings file).
- Contract or integration: `techspec.md#contracts-and-data`.

## Work

- [ ] T01.1 Add `HudSizeSettings` (bounds, `ResolvedPercent`, `Factor`) and `HudSizeStore`; register `UserSettings.HudSize`.
- [ ] T01.2 Add `HudScale` with `Percent`, `Factor`, change notification, and static `Current`.
- [ ] T01.3 Add `HudSizeSettingsViewModel` and the `SettingsViewModel.HudSize` init property.
- [ ] T01.4 Link the new App files in `TokenHound.Infrastructure.Tests.csproj` and write TC-01..TC-07.

## Acceptance criteria

- Percent values outside 50–150 clamp, off-step values round to a multiple of 5, and null or invalid reads as 100.
- A saved size round-trips through a temp settings file, other sections survive, and saving the HUD position does not change `HudSize`.
- Presets set 75, 100, and 125; Apply is enabled only when the value differs from the baseline; a failed save keeps the baseline and sets `ApplyError`.
- `HudScale` raises `PropertyChanged` for `Percent` and `Factor`.
- `TokenHound.Core` has no diff, and new public members have XML docs.

## Verification

- Unit: TC-01..TC-07 as in the TechSpec.
- Integration: temp settings file through `UserSettingsFile` (no double).
- E2E: omitted by .NET desktop policy.
- Manual: none in this task.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests --no-restore`, then `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`, then the QA-01..QA-05 greps over the diff files.
- Environment dependency: none.
- Expected evidence: build with 0 warnings, the tests passing with a non-zero count, and clean QA greps.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create: `src/TokenHound.Infrastructure/Configuration/HudSizeSettings.cs`, `src/TokenHound.Infrastructure/Configuration/HudSizeStore.cs`, `src/TokenHound.App/Presentation/HudScale.cs`, `src/TokenHound.App/ViewModels/HudSizeSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/HudSizeSettingsTests.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/HudSizeStoreTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/HudScaleTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/HudSizeSettingsViewModelTests.cs`

## Observability and recovery

- Operational signal: `Log.Warning` when the save fails, `Log.Debug` for the applied percentage.
- Recovery: revert the diff; the settings section is additive and ignored by older builds.

## Handoff

- Produced result: `HudSizeSettings` (50–150, step 5, default 100, `Normalize`/`ResolvedPercent`/`Factor`), `HudSizeStore` (own `HudSize` section), `UserSettings.HudSize`, `HudScale` (shared observable with static `Current`), `HudSizeSettingsViewModel` (slider `Percent`, `Label`, presets 75/100/125 through `PresetCommand`/`SetPreset`, Apply pattern, `ApplyError`), and `SettingsViewModel.HudSize`. TC-01..TC-07 are covered; TC-08 and the UI belong to T02.
- Changed files: created `HudSizeSettings.cs`, `HudSizeStore.cs`, `HudScale.cs`, `HudSizeSettingsViewModel.cs` and the tests `HudSizeSettingsTests.cs`, `HudSizeStoreTests.cs`, `HudScaleTests.cs`, `HudSizeSettingsViewModelTests.cs`; modified `UserSettings.cs`, `UserSettingsFile.cs` (one line, see deviations), `SettingsViewModel.cs`, `TokenHound.Infrastructure.Tests.csproj` (two `Compile Include` links).
- Checks: `dotnet build tests/TokenHound.Infrastructure.Tests` 0 errors, 0 warnings; Infrastructure.Tests 977 passed, 0 failed; Core.Tests 166 passed; `dotnet build src/TokenHound.App` 0 errors. QA-01, QA-02, QA-03 clean; QA-04 all new files ≤ 172 lines; QA-05 one false positive (a 3-parameter constructor whose generic `Func<HudSizeSettings, bool>` contains a comma).
- Validated state: base `2fe5515` plus the uncommitted working tree of this feature; projects Infrastructure, App, Infrastructure.Tests, Core.Tests; no environment dependency.
- Deviations (within the approved intent, recorded in `workflow.md`):
  1. Step is 5, not 10. The approved PRD and TechSpec said step 10, but the presets Small 75 and Large 125 are not multiples of 10, so a step of 10 would have rounded them to 80 and 130. PRD FR-01, TechSpec DEC-02 and the TC-01 values, and the task and manifest wording were updated to 5.
  2. `UserSettingsFile.MergeWithDefaults` copies each known section by hand, so a persisted `HudSize` was dropped on every `Load`. One line was added to carry `HudSize` (found by the `HudSizeStoreTests` round-trip). It is the only change in that file and is required for FR-07.
- Open items: none. Reservation hits: none. `RelayCommand` is now nested in four ViewModels (three pre-existing); the duplication is pre-existing and outside this diff.

### ADR candidates

None - direct TechSpec implementation or local decision.
