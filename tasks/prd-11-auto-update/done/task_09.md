# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T09 — "Updates" settings tab for enable and interval

## Outcome

The Settings window has an "Updates" tab next to "Cadence & Rate Limits" where the user can enable or disable automatic checks and set the interval in hours (0 disables), with validation and Apply, persisted to the `Update` section. The next scheduler tick uses the new value without a restart.

## Dependencies and boundaries

- Depends on: T02
- Unblocks: —
- In scope: `UpdateSettingsViewModel`, `SettingsViewModel.Updates`, `SettingsWindow.xaml` tab, factory change in `App.xaml.cs` `CreateSettingsViewModel` (or `App.Updates.cs`), test csproj link, TC-19.
- Out of scope: skipped-version management UI (cleared automatically by a newer tag).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | Configurable interval, 0/disabled, live effect |
| US-05 | `prd.md#stories-and-journeys` | Turn off periodic checks |
| UX-4 | `prd.md#user-experience` | Interval visible and editable with other refresh settings |
| NFR-06 | `prd.md#non-functional-requirements` | JSON persistence |
| DEC-03, DEC-13 | `techspec.md#technical-decisions` | Ranges, separate view model |
| TC-19 | `techspec.md#test-approach` | View model test |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md`.
- Existing code: `src/TokenHound.App/ViewModels/CadenceSettingsViewModel.cs` (validation/apply pattern to mirror, not extend), `SettingsViewModel.cs:38-62`, `SettingsWindow.xaml:287-493` (existing tab layout), `App.xaml.cs:346-357` (`CreateSettingsViewModel`); `tests/.../ViewModels/CadenceSettingsViewModelTests.cs`.
- Contract or integration: `techspec.md#contracts-and-data` (`Update` section).

## Work

- [x] T09.1 `UpdateSettingsViewModel`: `IsEnabled`, `IntervalHoursText`, validation (integer 0–720), `IsDirty`, `CanApply`, `ApplyCommand`, apply error surface; persists through `UpdateSettingsStore`, preserving `SkippedVersion`.
- [x] T09.2 Expose it as `SettingsViewModel.Updates`; construct it in the settings view model factory.
- [x] T09.3 "Updates" `TabItem` in `SettingsWindow.xaml` reusing existing styles.
- [x] T09.4 Link into tests; TC-19.

## Acceptance criteria

- Invalid text (non-integer, negative, > 720) shows an error and disables Apply.
- Apply writes `Enabled` and `CheckIntervalHours` and keeps `SkippedVersion`.
- Reopening Settings shows the persisted values.
- The next scheduler tick after Apply uses the new interval (covered by TC-17's settings-reload case).

## Verification

- Unit: TC-19.
- Integration: settings file round-trip through the real store in a temp file.
- E2E: omitted by .NET desktop policy.
- Manual: included in the visual check (open Settings → Updates, change and apply).
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: passing `UpdateSettingsViewModelTests`; app builds.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/App.xaml.cs` or `App.Updates.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`.
- Create: `src/TokenHound.App/ViewModels/UpdateSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateSettingsViewModelTests.cs`.

## Observability and recovery

- Operational signal: settings save failures surfaced in the tab like the cadence tab.
- Recovery: editing `settings.json` directly remains possible.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `UpdateSettingsViewModel` (`IsEnabled`, `IntervalHoursText`, validation of a whole number 0-720, `IsDirty`, `CanApply`, `ApplyCommand`, `IsApplied`, `ApplyError`) persisting through `UpdateSettingsStore`; Apply loads the current section, writes `Enabled` and `CheckIntervalHours`, and keeps `SkippedVersion`; a failed save shows an error and stays dirty. `SettingsViewModel.Updates` is an `init` property (not a fifth constructor parameter) set by the `CreateSettingsViewModel` factory through `App.Updates.CreateUpdateSettingsViewModel`. `SettingsWindow.xaml` has a third "Updates" tab with the enable checkbox, the interval box with its error text, and its own Apply button with an applied/error notice; the shared footer is untouched (it belongs to the Cadence tab, index 1). The scheduler re-reads settings each tick (T07), so the new values apply without a restart.
- Changed files: created `src/TokenHound.App/ViewModels/UpdateSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateSettingsViewModelTests.cs`; modified `src/TokenHound.App/ViewModels/SettingsViewModel.cs` (+3 lines), `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (+109 lines, one `TabItem`), `src/TokenHound.App/App.xaml.cs` (factory initializer), `src/TokenHound.App/App.Updates.cs` (+`CreateUpdateSettingsViewModel`), the test csproj (+1 link).
- Checks: `rtk dotnet build TokenHound.slnx --no-restore` -> 7 projects, 0 errors, 0 warnings; full Infrastructure suite -> 938 passed (TC-19: 11 rows covering defaults, invalid text, bounds 0 and 720, apply with skipped-version preservation and reload from a real temp settings file, edit after apply, failing save). Quality profile over the T09 files: QA-01..QA-05 empty; QA-08 hit `UpdateSettingsViewModel.cs:141` is a 4-argument `int.TryParse` call (not a parameter list).
- Validated state: base `a8bd1bf` + T01..T08 + the files above; Debug.
- Open items: reservation QA-06 `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` is 732 lines (623 at the baseline, already above 500; the one new tab was the recorded destination), so the escalation trigger "touched file above 500 lines" applies to the feature; a private `RelayCommand` now exists in two view models (`CadenceSettingsViewModel`, `UpdateSettingsViewModel`), below the 3-place duplication trigger. Manual check of the tab (open Settings, Updates, change and Apply) is part of the visual check.

### ADR candidates

None - direct TechSpec implementation or local decision.
