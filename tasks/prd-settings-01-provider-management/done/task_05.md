# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T05 — Build the Settings view models that list providers and apply toggles live

## Outcome

`SettingsViewModel` enumerates every provider registered in `UsageStore` — five today, including Copilot — into one `ProviderToggleViewModel` per row carrying a display name, glyph key, live badge, and a two-way `IsMonitored` property. Setting `IsMonitored` gates the engine, refreshes the badge, and persists to disk without blocking the caller. There is no Apply or Save step.

## Dependencies and boundaries

- Depends on: T01, T02, T03
- Unblocks: T06
- In scope: both view models, their tests, and their `Compile Include` links.
- Out of scope: XAML, brushes, window sizing, and dialog lifecycle (T06); startup wiring (T07). No WPF type may appear — the test project is `net10.0` without `UseWPF`, and the repository has no `ICommand` anywhere.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Every registered provider is listed with its display name and glyph from `ProviderCatalog` |
| FR-02 | `prd.md#functional-requirements` | One toggle per provider, defaulting to enabled when no preference is stored |
| FR-05 | `prd.md#functional-requirements` | Badge updates reactively when snapshots arrive |
| FR-09 | `prd.md#functional-requirements` | Changes apply live and save on interaction, with no Apply or Save button |
| OBJ-02, OBJ-03 | `prd.md#outcomes-and-metrics` | Status transparency and immediate synchronization |
| NFR-04 | `prd.md#non-functional-requirements` | Persistence is non-blocking so the toggle stays under 50 ms |
| A-02 | `prd.md#explicit-assumptions` | Immediate in-memory application, Windows 11 fluent settings pattern |
| DEC-06 | `techspec.md#technical-decisions` | Delegate-based view models, no `ICommand`, engine-first ordering |
| R-1 | `techspec.md#prd-reconciliation-decided-with-the-user-this-session` | Rows come from live registrations, not a hardcoded four |
| CMP-09, CMP-10, CMP-16 | `techspec.md#components-and-flow` | Row view model, dialog view model, compile links |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`
- Existing code: `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — the injected `Func`/`Action` constructor shape and `INotifyPropertyChanged` implementation to follow; confirm for yourself that no `ICommand` exists in `src/TokenHound.App`
- Existing code: `src/TokenHound.App/ViewModels/ProviderCatalog.cs` — `ResolveDefaultName`, `ResolveDefaultBadge`, `ResolveGlyphKey`, `ResolveGlyphScale`; it is `internal static` and already maps `copilot` and folds `gemini`/`antigravity` into one name
- Existing code: `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs` — the `SetProperty` helper to mirror
- Contract or integration: `techspec.md#contracts-and-data` — `RegisteredProviderIds` ordering is not guaranteed by `ConcurrentDictionary`, so sort by display name

## Work

- [x] T05.1 Create `ProviderToggleViewModel`: provider id, display name, badge state and label, glyph key and scale, and a two-way `IsMonitored` whose setter invokes an injected callback.
- [x] T05.2 Create `SettingsViewModel` building rows from `UsageStore.RegisteredProviderIds`, sorted by `ProviderCatalog.ResolveDefaultName` for a stable order.
- [x] T05.3 Seed each row's `IsMonitored` from `UsageStore.IsProviderEnabled` and its badge from the current snapshot via `ProviderBadgeResolver`.
- [x] T05.4 On toggle, apply in order: `SetProviderEnabled`, then badge refresh, then a fire-and-forget `ProviderSettingsStore.SaveAsync` of the full map.
- [x] T05.5 Subscribe to `SnapshotUpdated` to refresh badges live; implement `IDisposable` to unsubscribe.
- [x] T05.6 Add both files to the `Compile Include` block of `TokenHound.Infrastructure.Tests.csproj`.
- [x] T05.7 Add `SettingsViewModelTests` covering TC-13 and TC-14.

## Acceptance criteria

- A store with five registered providers yields five rows in a stable, name-sorted order, each with the catalog display name and glyph key; registering a sixth provider adds a row with no code change.
- With no stored preference, every row starts enabled.
- Setting `IsMonitored = false` flips the engine gate, moves the badge to `Disabled`, and invokes persistence exactly once with the new map.
- Setting `IsMonitored = true` moves the badge off `Disabled` and does not block on the disk write.
- A snapshot arriving for a listed provider updates that row's badge without a re-open.
- `Dispose` unsubscribes from `SnapshotUpdated`.
- Neither file references a WPF type; both compile inside the `net10.0` test project.

## Verification

- Unit: TC-13 toggle applies to engine, badge, and persistence; TC-14 row enumeration, ordering, and catalog resolution, including the sixth-provider case.
- Integration: rows are built over a real `UsageStore` with NSubstitute providers, and persistence is asserted through an injected callback rather than a disk write — the real file semantics are already proven by TC-01 in T01, so nothing is claimed twice.
- E2E: omitted by .NET desktop policy.
- Manual: none here; keyboard and visual acceptance are MAN-01 and MAN-02 in T06.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*SettingsViewModelTests*"`
  - Regression before handoff: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: none.
- Expected evidence: non-zero executed test count with exit code 0.

## Affected files

- Create: `src/TokenHound.App/ViewModels/ProviderToggleViewModel.cs`
- Create: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/SettingsViewModelTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: a structured Serilog warning when the fire-and-forget save fails; the in-memory gate stays authoritative for the session.
- Recovery: a read-only settings file degrades to "toggles work, do not survive restart" rather than an error dialog or a crash.

## Handoff

- Produced result: `SettingsViewModel` builds one `ProviderToggleViewModel` per identifier in
  `UsageStore.RegisteredProviderIds`, ordered by `ProviderCatalog.ResolveDefaultName` (OrdinalIgnoreCase) so the
  dialog is stable across launches regardless of `ConcurrentDictionary` enumeration order. Each row seeds
  `IsMonitored` from `UsageStore.IsProviderEnabled` and its badge from `ProviderBadgeResolver.ResolveState` over
  `UsageStore.CurrentSnapshots`. The `IsMonitored` setter raises `PropertyChanged` and calls the injected
  `Action<string, bool>`; `SettingsViewModel` then applies `SetProviderEnabled` -> badge refresh -> untracked
  `Task` persisting the full map through the injected `Func<ProviderSettings, CancellationToken, Task<bool>>`,
  so the visual response never waits on disk (NFR-04). Re-enabling additionally dispatches an untracked
  `RefreshProviderNowAsync` for that provider only. `SnapshotUpdated` is subscribed for live badges and
  unsubscribed in `Dispose`. Both view models are delegate-based with no `ICommand` and no WPF type; badge
  brushes are exposed only as resource key names.
- Changed files:
  - Created `src/TokenHound.App/ViewModels/ProviderToggleViewModel.cs` (127 lines).
  - Created `src/TokenHound.App/ViewModels/SettingsViewModel.cs` (167 lines).
  - Created `tests/TokenHound.Infrastructure.Tests/ViewModels/SettingsViewModelTests.cs` (267 lines, 8 tests).
  - Modified `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (+2 `Compile Include` links).
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*SettingsViewModelTests*"` -> 8 tests passed, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> 407 tests passed, 0 warnings, exit 0 (full-project regression, no pre-existing failure).
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` -> 4 projects, 0 errors, 0 warnings, exit 0 (extra check: the WPF host still compiles with the new files).
  - E2E omitted by the .NET desktop profile of the TechSpec; manual acceptance stays with MAN-01/MAN-02 in T06.
- Validated state: worktree `settings` at branch `settings`, commit `9699f06` plus the uncommitted T01-T04 changes; SDK pinned by `global.json` to `10.0.400`; native MTP route via `dotnet test --project`; Debug configuration, `net10.0` test project without `UseWPF`; new `.cs` files verified BOM-free (`head -c 3` returns `757369`).
- Open items: none for T05. T06 consumes `SettingsViewModel.Providers`, `BadgeLabel`, `BadgeBackgroundKey`, `BadgeForegroundKey`, `GlyphKey`, and `GlyphScale` from XAML; T07 supplies the real `ProviderSettingsStore.SaveAsync` delegate and the WPF dispatcher.

### ADR candidates

None - direct TechSpec implementation of DEC-06. Two local decisions worth noting in review, both inside the
decision already recorded by DEC-06 and neither durable enough for its own ADR:

- The engine, not the row, is the source of truth when the badge and the persisted map are computed
  (`_usageStore.IsProviderEnabled`), so a no-op `SetProviderEnabled` on an unregistered identifier can never
  make the dialog or `appsettings.json` disagree with the engine.
- Persistence and the re-enable refresh use `.ConfigureAwait(false)` although they live in the App layer: both
  are untracked background continuations that only log, and marshalling them back to the UI thread would
  contradict the non-blocking intent of NFR-04. `ApplicationLifetime.cs` sets the same precedent in this layer.
