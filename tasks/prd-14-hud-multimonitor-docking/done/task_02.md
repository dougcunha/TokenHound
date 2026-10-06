# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Pure docking, display resolution, and edge layout rules

## Outcome

Pure, display-free functions compute the docked window position for the five docked modes, resolve the preferred display against a list of connected displays, and map each mode to its edge and orientation; all are unit-tested in `TokenHound.Infrastructure.Tests`.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T03, T04
- In scope: `DisplayInfo`, `DisplayResolver` (+ `DisplayResolution`), `NotchPlacement.Dock`, `HudEdgeLayout` (+ `HudEdge`), test-project links.
- Out of scope: Win32 calls, WPF types, window code.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-02, FR-03, FR-04 | `prd.md#functional-requirements` | Docked rectangles; edge and orientation |
| FR-06, FR-07, FR-08, FR-09 | `prd.md#functional-requirements` | Display resolution and fallback |
| DEC-05, DEC-11, DEC-12 | `techspec.md#technical-decisions` | Identity match order; pure edge layout; flush window |
| CMP-04..CMP-07, CMP-18 | `techspec.md#components-and-flow` | Pure components and links |
| TC-03, TC-04, TC-05 | `techspec.md#test-approach` | Unit tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `src/TokenHound.App/UI/Placement/NotchPlacement.cs`, `ScreenBounds.cs` — style and pure pattern; `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj:42-65` — `<Compile Include … Link>` entries to extend.
- Existing tests: `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs`.

## Work

- [x] T02.1 Create `DisplayInfo` (record: `DevicePath`, `EdidKey?`, `Name`, `Number`, `IsPrimary`, `WorkArea` as `ScreenBounds` in pixels, `Width`, `Height`) and `DisplayResolver` with `Resolve`, `ToPreference`, and `FindHosting(displays, centerX, centerY)` per DEC-05 (no primary flagged → first display).
- [x] T02.2 Add `NotchPlacement.Dock(HudDockMode, ScreenBounds, double width, double height)` returning whole-pixel `(Left, Top)`; overflow clamps to the work-area origin; `Free` throws `ArgumentOutOfRangeException`.
- [x] T02.3 Create `HudEdgeLayout.For(HudDockMode)` → `(HudEdge Edge, bool IsVertical)`.
- [x] T02.4 Link the new files in the test project; add `DisplayResolverTests`, `HudEdgeLayoutTests`, and `Dock` cases in `NotchPlacementTests`.

## Acceptance criteria

- On work area (−1920, 40, 1920×1000) a 300×60 window docks Top Right at (−300, 40), and a 60×300 window docks Right edge at (−60, 390).
- Center alignment with an odd remainder rounds consistently (documented in the test).
- Resolver: an exact device path wins over EDID; a unique EDID match is used when the path is absent; an ambiguous EDID match falls back to the primary with `IsFallback = true`.

## Verification

- Unit: TC-03, TC-04, TC-05.
- Integration: none (pure).
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` filtered with `--filter-class "*Placement*"`, `"*DisplayResolver*"`, `"*HudEdgeLayout*"`, then unfiltered.
- Environment dependency: none.
- Expected evidence: build 0 warnings; test counts.

## Affected files

- Create: `src/TokenHound.App/UI/Placement/DisplayInfo.cs`, `DisplayResolver.cs`, `HudEdgeLayout.cs`; `tests/TokenHound.Infrastructure.Tests/Placement/DisplayResolverTests.cs`, `HudEdgeLayoutTests.cs`
- Modify: `src/TokenHound.App/UI/Placement/NotchPlacement.cs`, `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: none (pure).
- Recovery: additive.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: pure placement rules. `DisplayInfo` and `DisplayResolution` records; `DisplayResolver.Resolve` (null or key-less preference → primary; exact device path; else a unique EDID match; else primary with `IsFallback`; no display flagged primary → first; empty list throws), `ToPreference`, `FindHosting` (nearest work area to a point), `IsPrimaryPreference`; `NotchPlacement.Dock` for the five docked modes (centering floors the remainder, overflow starts at the work-area origin, Free throws); `HudEdge` enum and `HudEdgeLayout.For`.
- Changed files: `src/TokenHound.App/UI/Placement/DisplayInfo.cs`, `DisplayResolution.cs`, `DisplayResolver.cs`, `HudEdge.cs`, `HudEdgeLayout.cs` (new); `NotchPlacement.cs` (`Dock`); `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (5 links); `tests/.../Placement/DisplayResolverTests.cs`, `HudEdgeLayoutTests.cs` (new); `NotchPlacementTests.cs` (+6 test methods).
- Checks: test project build 0 warnings; `--filter-namespace "*Placement*"` → 36 passed; unfiltered → 1025 passed, exit 0; App build 0 warnings. One test-data error found and fixed on the way (a point in the gap between displays was expected on the wrong display).
- Validated state: working tree at `c4b55b9` plus the T01 and T02 diffs.
- Quality profile: QA-01..QA-04, QA-07 clean. QA-05 hits at `DisplayResolverTests.cs:56` and `:80` are false positives (commas inside a collection expression, two real arguments). QA-06: largest touched file `NotchPlacementTests.cs`, 283 lines.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
