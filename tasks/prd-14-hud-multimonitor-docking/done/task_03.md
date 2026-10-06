# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — Placement service

## Outcome

`HudPlacementService` holds the current `HudPositionSettings`, applies the drag, mode, and display operations with the FR-18 rule, persists each change once through an injected save function using `with` (no field lost), and raises `Changed`.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: T05, T07
- In scope: the service, a `Create(HudPositionStore)` factory, tests, test-project link.
- Out of scope: window code, Win32, Settings UI.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-10, FR-18 | `prd.md#functional-requirements` | Drag → Free; Free → docked display rule |
| NFR-03 | `prd.md#non-functional-requirements` | No writes outside user operations |
| DEC-08, DEC-09 | `techspec.md#technical-decisions` | Single owner; display rule; Free stores current coordinates |
| CMP-10 | `techspec.md#components-and-flow` | Service |
| TC-06, TC-11 | `techspec.md#test-approach` | Service tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `src/TokenHound.App/ViewModels/HudSizeSettingsViewModel.cs` — `Func<T, bool> save` plus `Create(store, …)` pattern; `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs:PersistPosition` — the `new { Left, Top }` write this service replaces (QA-07).

## Work

- [x] T03.1 Create `HudPlacementService` (`TokenHound.App.Presentation`): `Current`, `Mode` (resolved), `Changed`, `SelectMode(...)` taking the mode, the hosting `DisplayInfo?`, and the window's current DIP position (use a small record if the list reaches four parameters), `SelectDisplay(HudDisplayPreference?)`, `RecordDrag(double left, double top)`, and `UpdateFreePosition(double left, double top)` for the clamp correction path; each returns whether it saved.
- [x] T03.2 Link it in the test project; add `HudPlacementServiceTests` covering TC-06 and the NFR-03 part of TC-11 (counting save).

## Acceptance criteria

- `RecordDrag` → `Mode = "Free"`, new coordinates, `Display` unchanged; one save.
- From Free with preference "Primary monitor" and hosting display 2 (not primary), selecting Top Right stores display 2's preference; hosting the primary keeps `Display = null`.
- Selecting Free stores the given coordinates.
- Selecting the current mode or display again does not save.
- A failed save still updates `Current`, raises `Changed`, and returns `false`.

## Verification

- Unit: TC-06, TC-11 (service part).
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` filtered with `--filter-class "*HudPlacementService*"`, then unfiltered.
- Environment dependency: none.
- Expected evidence: build 0 warnings; test counts.

## Affected files

- Create: `src/TokenHound.App/Presentation/HudPlacementService.cs`, `tests/TokenHound.Infrastructure.Tests/Presentation/HudPlacementServiceTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: `Log.Warning` on save failure with the settings file path; `Log.Debug` of the new mode and display.
- Recovery: none needed (in-memory state plus the existing store).

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `HudPlacementService` (`Current`, resolved `Mode`, `Changed`, `Create(HudPositionStore)`, `SelectMode(mode, HudPlacementContext)`, `SelectDisplay`, `RecordDrag`, `UpdateFreePosition`). Every write is `Current with { … }`; an unchanged result neither saves nor raises `Changed`; a failed save keeps the new state, raises `Changed`, logs a warning, and returns `false`. `SelectMode` to a docked mode is a no-op when that exact mode name is already stored, so choosing Top Center over an unknown stored value persists the choice and clears the DEC-02 warning. Leaving Free applies DEC-09 by comparing the resolved preferred display with the hosting display by device path. `HudPlacementContext` record (displays, hosting display, DIP position) keeps `SelectMode` at two parameters.
- Changed files: `src/TokenHound.App/Presentation/HudPlacementService.cs`, `HudPlacementContext.cs` (new); `tests/TokenHound.Infrastructure.Tests/Presentation/HudPlacementServiceTests.cs` (new, 10 tests); test project links (+2).
- Checks: test project build 0 warnings; `--filter-class "*HudPlacementService*"` → 10 passed; unfiltered → 1035 passed, exit 0; App build 0 warnings.
- Validated state: working tree at `c4b55b9` plus the T01..T03 diffs.
- Quality profile: QA-01..QA-04, QA-07 clean. QA-05 hits in `HudPlacementServiceTests.cs:77, 79, 122` are false positives (object-initializer commas and a nested call; at most three real arguments). QA-06: largest touched file 182 lines.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
