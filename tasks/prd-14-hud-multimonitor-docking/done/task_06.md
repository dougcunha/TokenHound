# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T06 — Edge chrome: vertical capsule, outline, tooltips, and popup

## Outcome

The capsule follows its docked edge: side docks stack providers vertically; the shadow gutter, rounded corners, and border sides match the edge; minimum sizes swap for the vertical capsule and keep scaling with HUD size; provider tooltips and the status popup open toward the inside of the screen.

## Dependencies and boundaries

- Depends on: T05
- Unblocks: T07
- In scope: `HudDockLayout`, `NotchWindow.Dock.cs` (chrome and orientation-aware minimums), `NotchWindow.Scale.cs` (minimums move out), `NotchWindow.xaml` names and items panel, `ProviderRing.xaml` tooltip placement binding.
- Out of scope: Bézier outline, backdrop, click-through, animation (PRD out of scope); Settings (T07).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03, FR-04, FR-05 | `prd.md#functional-requirements` | Vertical stack; outline; tooltip and popup direction |
| FR-13 | `prd.md#functional-requirements` | Re-anchor after orientation and size changes |
| NFR-02 | `prd.md#non-functional-requirements` | Flush after the orientation change, no oscillation |
| DEC-11, DEC-12, DEC-14 | `techspec.md#technical-decisions` | Edge layout mapping; gutters; tooltip and popup placement |
| CMP-11, CMP-13, CMP-14 (chrome part), CMP-15 | `techspec.md#components-and-flow` | Shared observable, dock partial, XAML, ring tooltip |
| TC-08 (MA-2 side edges, MA-3) | `techspec.md#test-approach` | Manual checks |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `NotchWindow.xaml` (22-31 chrome, 73-77 items panel, 92-98 popup); `NotchWindow.Scale.cs` (min sizes); `src/TokenHound.App/Presentation/HudScale.cs` (shared observable pattern for separate visual trees); `ProviderRing.xaml:23-35` (tooltip).
- Design: `docs/design/2026-08-28-usage-notch-design.md` ("The shape of the thing", "Hover state").

## Work

- [x] T06.1 Create `HudDockLayout` (`Current`, `TooltipPlacement`) and bind the `ProviderRing` tooltip `Placement` to it.
- [x] T06.2 Create `NotchWindow.Dock.cs`: on mode change apply the DEC-11/12/14 chrome (items panel orientation, root grid gutter, capsule corner radius and border thickness, ring margin, status popup placement) before the placement pass; orientation-aware scaled minimums replacing the fixed pair in `NotchWindow.Scale.cs`.
- [x] T06.3 Name the XAML elements the partial needs and remove the hard-coded values it now owns.
- [x] T06.4 Build, run all tests, stop any running instance, launch, and run MA-2 for the side edges and MA-3 on the primary display with screenshots; re-run MA-1 to confirm the top-center look is unchanged.

## Acceptance criteria

- Right edge: vertical capsule, rounded left corners, no right border, flush with the right work-area edge; tooltips and the status popup open to its left (MA-2, MA-3).
- Left edge: mirrored.
- Top docks and Free look exactly as before this feature (MA-1 screenshot matches the pre-feature capsule).
- Switching between a top dock and a side dock re-anchors once and stays still (no oscillation).

## Verification

- Unit: none new (`HudEdgeLayout` tested in T02); the full suite must stay green.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-1, MA-2 (side edges), MA-3 by the agent on the primary display.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: Windows MCP desktop access; stop the running HUD before building (MSB3027).
- Expected evidence: builds 0 warnings; test count; screenshots of Right edge with a tooltip open, Left edge, and Top Center.

## Affected files

- Create: `src/TokenHound.App/Presentation/HudDockLayout.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.Dock.cs`
- Modify: `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `NotchWindow.Scale.cs`, `src/TokenHound.App/UI/Controls/ProviderRing.xaml`

## Observability and recovery

- Operational signal: none beyond T05's placement log.
- Recovery: top docks and Free keep today's chrome.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: edge chrome. `HudDockLayout.Current` (edge, `IsVertical`, `Orientation`, `RingMargin`, `TooltipPlacement`) drives the ring panel orientation, ring spacing, and tooltip placement through static bindings; `NotchWindow.Dock.cs` `ApplyChrome()` sets the root gutter (DEC-12), capsule corners, border sides, padding, and status popup placement (Bottom / Left / Right with a 4 px gap) and runs before every placement pass (load and `Changed`); `NotchWindow.Scale.cs` swaps the scaled minimums for the vertical capsule. Four-value `Thickness`/`CornerRadius` constructors were written as uniform value + zeroed sides (one argument each, QA-05).
- Changed files: `src/TokenHound.App/Presentation/HudDockLayout.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.Dock.cs` (new); `NotchWindow.xaml` (`RootGrid`, bound items panel orientation and ring margin), `NotchWindow.Scale.cs`, `NotchWindow.Placement.cs` (`OnPlacementChanged` applies chrome then placement), `NotchWindow.xaml.cs` (`OnLoaded` applies chrome), `src/TokenHound.App/UI/Controls/ProviderRing.xaml` (tooltip `Placement` binding).
- Checks: App build 0 warnings; Infrastructure.Tests 1035 passed. Manual on the primary display: MA-2 side edges pass (Right edge: vertical capsule flush right, rounded left corners, centered 393..686 on a 1080 display, re-centered after the fourth provider loaded; Left edge mirrored); MA-3 pass (Antigravity tooltip opened to the left of the ring; "Refreshing usage…" popup opened to the left of the capsule; the Position submenu flipped left when the HUD was on the right edge); MA-1 regression pass (Top center returned to the original look, capsule 813..1107).
- Validated state: working tree at `c4b55b9` plus T01..T06. The last visual check ran before the cosmetic constructor rewrite (same values); the build after the rewrite is clean.
- Quality profile: QA-01..QA-04, QA-07 clean; QA-05 only the baseline `WndProc` signature; QA-06 largest touched file `NotchWindow.xaml.cs` 246 lines.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
