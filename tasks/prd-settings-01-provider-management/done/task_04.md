# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T04 — Show HUD rings only for monitored providers, restoring them in place

## Outcome

`NotchViewModel` observes `ProviderEnablementChanged` and keeps the bound `Rings` collection limited to monitored providers. Disabling removes a ring immediately; re-enabling restores it at its original position in the capsule rather than appending it to the end. Disabling every provider empties the capsule without an exception and without waking the mock fallback.

## Dependencies and boundaries

- Depends on: T02
- Unblocks: T07
- In scope: ring bookkeeping, the enablement subscription and its disposal, and tests.
- Out of scope: `NotchWindow.xaml` and the `ProviderRing` control are untouched — only `Rings` membership changes. Ring re-ordering and capsule geometry are out of scope per the PRD.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | Disabled provider's ring is removed from the HUD immediately; re-enabling restores it |
| FR-10 | `prd.md#functional-requirements` | Zero enabled providers yields a clean standby capsule, no crash, no fallback mock unless configured |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Toggling off removes the ring from the HUD |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Effect is immediate, with no restart |
| A-05 | `prd.md#explicit-assumptions` | Empty capsule stays visible, draggable, and right-clickable |
| DEC-05 | `techspec.md#technical-decisions` | Dictionary plus first-seen order list; index-preserving re-insert; no `CollectionView` |
| CMP-11 | `techspec.md#components-and-flow` | `NotchViewModel` modification |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`
- Existing code: `src/TokenHound.App/ViewModels/NotchViewModel.cs` — `UpdateOrAddRing`, the `SnapshotUpdated`/`ActivityUpdated` subscription pattern, and `Dispose` unsubscription to mirror for the new event
- Existing code: `src/TokenHound.App/UI/Windows/NotchWindow.xaml:58` — `ItemsControl ItemsSource="{Binding Rings}"`; the window is `SizeToContent="WidthAndHeight"` with `MinWidth="180" MinHeight="48"`, which is what keeps an empty capsule visible and draggable
- Existing code: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs` — the existing store-plus-substitute setup to extend
- Contract or integration: `techspec.md#components-and-flow` — the "Zero enabled" flow paragraph, including why the mock fallback stays dormant

## Work

- [x] T04.1 Add a private ring dictionary keyed by provider id plus a first-seen order list; keep `Rings` as the filtered, bound projection.
- [x] T04.2 Subscribe to `UsageStore.ProviderEnablementChanged`, marshalling through the existing `_uiDispatcher`.
- [x] T04.3 Remove the ring on disable; on enable, rebuild from the cached snapshot when one exists and insert at the index implied by the order list.
- [x] T04.4 Unsubscribe in `Dispose` alongside the two existing handlers.
- [x] T04.5 Keep `UpdateOrAddRing` from re-adding a ring for a provider that is currently disabled.
- [x] T04.6 Add TC-10 and TC-11 to `NotchViewModelTests`.
- [x] T04.7 Condition evaluated and not met: `NotchViewModel.cs` ends at 266 lines, below the 300-line cap, so no `NotchViewModel.Rings.cs` partial was created.

## Acceptance criteria

- With rings for three providers, disabling the middle one removes it from `Rings`; re-enabling returns it at its original index, not at the end.
- Disabling every provider leaves `Rings` empty, raises no exception, and leaves `IsFallbackActive` false.
- A snapshot arriving for a disabled provider does not resurrect its ring.
- `Dispose` unsubscribes from all three store events; no handler leaks.
- Existing `NotchViewModelTests` cases still pass — default behavior with no gating is unchanged.

## Verification

- Unit: TC-10 remove-and-restore-at-index; TC-11 all-disabled empty state with fallback dormant; plus the snapshot-for-disabled-provider edge.
- Integration: exercised against a real `UsageStore` with NSubstitute providers, as the existing `NotchViewModelTests` already do — the event wiring is genuinely traversed rather than stubbed.
- E2E: omitted by .NET desktop policy.
- Manual: none here. The visual reflow of the capsule is covered by MAN-05 in T07.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"`
  - Regression before handoff: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: none.
- Expected evidence: non-zero executed test count with exit code 0.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/NotchViewModel.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`
- Create (conditional, T04.7): `src/TokenHound.App/ViewModels/NotchViewModel.Rings.cs`

## Observability and recovery

- Operational signal: none beyond the enablement log already emitted by T02.
- Recovery: ring state is in-memory and rebuilt from `CurrentSnapshots` on the next launch.

## Handoff

- Produced result: `NotchViewModel` now tracks every known ring in a private `_ringsByProvider` dictionary (`StringComparer.OrdinalIgnoreCase`) plus a first-seen `_ringOrder` list, while the bound `Rings` collection is the filtered projection holding only monitored providers. It subscribes to `UsageStore.ProviderEnablementChanged` and marshals the handler through the existing `_uiDispatcher`. Disabling removes the ring from `Rings`; re-enabling refreshes the cached ring from `UsageStore.CurrentSnapshots` when a snapshot exists and re-inserts it at the index implied by `_ringOrder` (`ResolveInsertIndex` counts only currently visible predecessors), so a ring returns to its original slot instead of the end of the capsule. `UpdateOrAddRing` now consults `IsProviderEnabled` before making a ring visible, so a snapshot for a disabled provider updates the cached ring but never resurrects it in `Rings`. `Dispose` unsubscribes all three store events. No WPF type was introduced: the only new `using` is `System.Collections.Generic`, so the file still compiles into the `net10.0` test project through its `Compile Include ... Link` entry. The mock fallback stays dormant with zero enabled providers because every `LoadMockFallback` trigger remains guarded by `_mockProvider is not null`, and `App.xaml.cs` passes `mockProvider: null`.

- Changed files:
  - Modified: `src/TokenHound.App/ViewModels/NotchViewModel.cs` (+67 / -4; 266 lines total, below the 300-line cap)
  - Modified: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs` (+108 / -0: TC-10, TC-11, the disabled-provider snapshot edge, the `Dispose` enablement-unsubscribe case, and a `RegisterAndRefreshAsync` helper)
  - Not created: `src/TokenHound.App/ViewModels/NotchViewModel.Rings.cs` — the 300-line cap was not crossed (T04.7 condition false), so `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` was left untouched.

- Checks (final run, after the review fix):
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings; exit 0.
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` -> 4 projects, 0 errors, 0 warnings; exit 0 (the WPF consumer still compiles).
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"` -> 11 tests passed, 0 failed; exit 0.
  - Regression before handoff: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> 399 tests passed, 0 failed; exit 0.
  - Mutation check on DEC-05 (temporary, reverted): replacing the index-preserving `Rings.Insert(ResolveInsertIndex(providerId), ring)` with `Rings.Add(ring)` made TC-10 fail with `{"claude", "cursor", "codex"} differs at index 1` (10 passed, 1 failed; exit 2). This proves the index assertion is load-bearing rather than a membership-only check.
  - E2E omitted by the .NET desktop policy in `techspec.md#test-strategy`; no manual script belongs to T04 (the capsule reflow is MAN-05 in T07).

- Review corrections applied:
  - Encoding: both edited files had picked up a spurious UTF-8 BOM (`EF BB BF`) that was absent at `HEAD`. Both were rewritten as BOM-less UTF-8, matching every sibling file. `using System;` and the test file's line 1 dropped out of the diff, which is why the diffstat fell from +73/-5 to +67/-4 on the source and from 110 changed lines to a pure +108 insertion on the tests.
  - `using System.Linq;` was reviewed and deliberately kept. The review premise that it became dead is not supported: three LINQ call sites remain in the file — `Rings.FirstOrDefault` in `OnActivityUpdated` (line 120) and `Rings.Any` / `Rings.FirstOrDefault` in `ShouldFallbackToMock` (lines 210, 213). `Rings.Contains`, `Rings.Insert`, `Rings.Remove`, and `Rings.Count` are `Collection<T>` members, not LINQ, so only the `FirstOrDefault`/`Any` sites matter, and they were untouched by T04. The directive is technically redundant under `ImplicitUsings=enable`, but that was already true at `HEAD` and applies repo-wide: all 4 LINQ-using files in `src/TokenHound.App/ViewModels/` declare it explicitly and all 3 non-LINQ files omit it, so removing it would make this the only LINQ-using file in the folder without it.

- Validated state:
  - Code/diff: working tree at branch `settings` with the pre-existing T01/T02/T03 changes preserved; only the two files listed above were modified by T04.
  - Configuration: no configuration or project file changed; `Directory.Build.*` does not exist, and `global.json` (SDK `10.0.400`, `test.runner: Microsoft.Testing.Platform`) and the test `.csproj` are untouched.
  - Projects: `TokenHound.Core`, `TokenHound.Infrastructure`, `TokenHound.App`, `TokenHound.Infrastructure.Tests`.
  - Environment: Windows 11, native MTP through `dotnet test --project`; no external dependency required.

- Open items: one item for the caller's decision — the review's second finding (remove `using System.Linq;`) was not applied, for the evidence recorded above. If the intent is a repo-wide cleanup of directives made redundant by `ImplicitUsings`, that is a separate change outside T04's contract and should not land in this task's diff.

### ADR candidates

None - direct TechSpec implementation (DEC-05) with no new alternatives or trade-offs beyond those already recorded there.
