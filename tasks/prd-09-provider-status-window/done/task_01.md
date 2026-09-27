# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/prd.md`
2. `tasks/prd-09-provider-status-window/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Status projection and live view model

## Outcome

`ProviderStatusViewModel`, a type with no WPF dependency, exposes `Groups` for every registered and enabled provider. Groups are ordered by family and carry a count. Each account has the HUD's display name and status message, plus one column per HUD usage row: label, used %, colour level, and reset line. Exhausted accounts show `back in …`. Pending and empty states are set. The view model stays current on store events and on a 60-second tick until it is disposed.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope:
  - CMP-01..CMP-06: new view model, projection, formatter, and records.
  - `ResetTimeUtc` on `ProviderUsageRow` and its factory sites.
  - `ProviderCatalog.ResolveFamilyName`.
  - `ResolveStatusMessage` made `internal`.
  - Test project `<Compile Include>` entries and unit tests.
- Out of scope: XAML, window, `DialogService`, menus, `App.xaml.cs` (T02); any Core or Infrastructure change.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03..FR-12 | `prd.md#functional-requirements` | Data, grouping, columns, formatting, exhausted, live, empty/pending |
| NFR-01..NFR-03, NFR-05 | `prd.md#non-functional-requirements` | No Core change, no invented %, HUD consistency, release on dispose |
| DEC-01, DEC-02, DEC-04..DEC-09 | `techspec.md#technical-decisions` | Projection rules |
| CMP-01..CMP-06 | `techspec.md#components-and-flow` | Components |
| TC-01..TC-09 | `techspec.md#test-approach` | Unit scenarios |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` C# rules; `dotnet-efficient-validation`; QA-01..QA-07 in the TechSpec.
- Existing code:
  - `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs:AddQuotaRows` (reset fallback to `ActiveBlock` for index 0).
  - `ProviderUsageRowFactory.Copilot.cs:32` and `ProviderUsageRowFactory.Cline.cs:62` (other `ResetText` sites).
  - `ProviderRingViewModel.Status.cs:ResolveStatusMessage`.
  - `ProviderCatalog.cs:ResolveDefaultName`.
  - `NotchViewModel.cs` constructor and `ApplyEnablement` (subscription and dispatcher pattern).
  - `UsageStore.Gating.cs:RegisteredProviderIds/IsProviderEnabled/SetProviderEnabled`.
- Tests to mirror: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs` (real `new UsageStore()`), `ProviderUsageRowFactoryTests.cs`.
- Test project linking: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` `<Compile Include>` list.

## Work

- [ ] T01.1 Add `ResetTimeUtc` to `ProviderUsageRow` and set it at the three `ResetText` sites; extend `ProviderUsageRowFactoryTests` (TC-09).
- [ ] T01.2 Add `ProviderCatalog.ResolveFamilyName`; make `ResolveStatusMessage` `internal static`.
- [ ] T01.3 Create `UsageLevel`, `ProviderStatusColumn`, `ProviderStatusAccount`, and `ProviderStatusGroup` records, and `ProviderStatusFormatter` (DEC-07, DEC-08) with `ProviderStatusFormatterTests` (TC-06, TC-07).
- [ ] T01.4 Create `ProviderStatusProjection` (DEC-01, DEC-05, DEC-06) with `ProviderStatusProjectionTests` (TC-04, TC-05, TC-08).
- [ ] T01.5 Create `ProviderStatusViewModel` (DEC-09): subscriptions, dispatcher, `TimeProvider` timer, idempotent `Dispose`; `ProviderStatusViewModelTests` (TC-01..TC-03).
- [ ] T01.6 Link the new files in the test csproj; build the App and test projects; run the focused tests, then the full Infrastructure and Core test projects.

## Acceptance criteria

- TC-01..TC-09 pass. Existing tests in `tests/TokenHound.Infrastructure.Tests` and `tests/TokenHound.Core.Tests` still pass.
- `ProviderStatus*` and `UsageLevel` files contain no `System.Windows` or `Dispatcher` references (QA-05).
- Columns for a snapshot equal `ProviderUsageRowFactory.CreateRows` in count, order, and label. A column without `UsedFraction` has no bar and shows the quantity text.
- After `Dispose`, raising store events or advancing the fake clock causes no rebuild and throws no exception.
- No file under `src/TokenHound.Core` or `src/TokenHound.Infrastructure` changes.
- Each new file is ≤ 300 lines and each method is ≤ 30 lines. No `DateTime.Now`/`UtcNow`.

## Verification

- Unit: TC-01..TC-09 as in the TechSpec.
- Integration: none beyond the real `UsageStore` instance used in view-model tests.
- E2E: omitted by .NET desktop policy.
- Manual: none (T02).
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatus*"`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: none; stop a running `TokenHound.App` if the build output is locked.
- Expected evidence: exit code 0 and test counts per command; QA profile output over the task files.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `ProviderUsageRowFactory.cs`, `ProviderUsageRowFactory.Copilot.cs`, `ProviderUsageRowFactory.Cline.cs`, `ProviderCatalog.cs`, `ProviderRingViewModel.Status.cs`; `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderStatusViewModel.cs`, `ProviderStatusProjection.cs`, `ProviderStatusFormatter.cs`, `ProviderStatusGroup.cs`, `ProviderStatusAccount.cs`, `ProviderStatusColumn.cs`, `UsageLevel.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusViewModelTests.cs`, `ProviderStatusProjectionTests.cs`, `ProviderStatusFormatterTests.cs`

## Observability and recovery

- Operational signal: none in T01 (logs are added with the window in T02).
- Recovery: revert the task diff; no persisted state.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result:
  - `ProviderStatusViewModel` (WPF-free) exposes `Groups` and `IsEmpty` for registered and enabled providers.
  - It rebuilds on `SnapshotUpdated`, `ProviderEnablementChanged`, and a 60 s `TimeProvider` timer, all through the injected dispatcher. Disposal is idempotent and unsubscribes both events and disposes the timer.
  - `ProviderStatusProjection` creates accounts (columns mirror `ProviderUsageRowFactory.CreateRows`), pending accounts, exhausted `back in` text, and groups by family with the default Claude profile first.
  - `ProviderStatusFormatter` handles percent, colour level, reset line, and back-in.
  - `ProviderUsageRow.ResetTimeUtc` is set at the three reset sites. `ProviderCatalog.ResolveFamilyName` and `IsDefaultClaudeProfile` were added. `ProviderRingViewModel.ResolveStatusMessage` is now `internal`.
- Changed files:
  - Modified: `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `ProviderUsageRowFactory.cs`, `ProviderUsageRowFactory.Copilot.cs`, `ProviderUsageRowFactory.Cline.cs`, `ProviderCatalog.cs`, `ProviderRingViewModel.Status.cs`; `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs`.
  - Created: `src/TokenHound.App/ViewModels/ProviderStatusViewModel.cs`, `ProviderStatusProjection.cs`, `ProviderStatusFormatter.cs`, `ProviderStatusGroup.cs`, `ProviderStatusAccount.cs`, `ProviderStatusColumn.cs`, `UsageLevel.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/ManualTimeProvider.cs`, `ProviderStatusFormatterTests.cs`, `ProviderStatusProjectionTests.cs`, `ProviderStatusViewModelTests.cs`.
- Checks (final state):
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`: 0 errors, 0 warnings.
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/...csproj`: 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatus*"`: 33 passed. Earlier run on the same test code, before the two review refactors; the full run below covers the final state.
  - TC-09 via `--filter-method "*CreateRows_WhenQuotaWindowsHaveResets_ExposesResetInstant*"`: 1 passed.
  - Full `tests/TokenHound.Infrastructure.Tests`: 816 passed, exit 0.
  - `tests/TokenHound.Core.Tests`: 92 passed, exit 0. Core is unchanged by this task.
- Validated state: git base `53da181` plus this task's working-tree diff; Debug configuration; net10.0-windows (App) and net10.0 (tests); SDK per `global.json` 10.0.400 with the MTP runner. No Core or Infrastructure file changed (`git status` on both folders is empty).
- Quality profile:
  - QA-01..QA-04: no hits.
  - QA-05: 5 lines match `Dispatcher` only as the `_uiDispatcher` delegate name (`Action<Action>`, same pattern as `NotchViewModel`), not a WPF type. False positive; WPF independence is proven by the `net10.0` test build.
  - QA-06: largest touched file is 177 lines. One method was extracted (`CreatePendingAccount`) to keep `CreateAccount` ≤ 30 lines.
  - QA-07: the one reservation hit (`Trim` with 4 characters) was fixed with a `DATE_SEPARATORS` array. No reservation hits remain.
- Decisions within TechSpec authority:
  - TechSpec risk "culture month/day pattern": the absolute date is the culture's `ShortDatePattern` with the year removed and month and day zero-padded, and the time is the culture's `ShortTimePattern`.
  - TC-07 pins `InvariantCulture`, which reproduces the reference `09/27, 13:00` exactly. `pt-BR` gives `27/09, 13:00`. `en-US` users will see a 12-hour time (e.g. `09/27, 1:00 PM`).
- Open items: none for T01. Visual rendering, entry points, and manual acceptance belong to T02.

### ADR candidates

None - direct TechSpec implementation or local decision.
