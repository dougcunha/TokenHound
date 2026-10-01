# Stable execution context

Load in this exact order:

1. `tasks/prd-13-hud-size/prd.md`
2. `tasks/prd-13-hud-size/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Scale the HUD and add the Settings section

## Outcome

Applying a size in Settings → General resizes the live HUD, its status popup, and its tooltip card at once. The stored size is applied at startup, and the General tab opens even when the Startup section is unavailable.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: `LayoutTransform` bindings, scaled window minimums, the HUD size section and the tab-visibility change in Settings, startup load, wiring of the ViewModel, and extra placement test cases.
- Out of scope: placement logic changes, `WndProc` and window flags, other windows, and any automatic sizing.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-02, FR-03 | `prd.md#functional-requirements` | Slider, presets, Apply with immediate effect |
| FR-04, FR-05 | `prd.md#functional-requirements` | Whole capsule, popup, and tooltip scale |
| FR-06 | `prd.md#functional-requirements` | Anchor kept and clamped |
| NFR-02..NFR-04 | `prd.md#non-functional-requirements` | Invariants, crispness, accessibility |
| DEC-04..DEC-06, DEC-08..DEC-11 | `techspec.md#technical-decisions` | Scaling, tab visibility, startup load |
| CMP-07..CMP-10 | `techspec.md#components-and-flow` | Components |
| TC-08, TC-09 | `techspec.md#test-approach` | Placement cases and manual script |

## Context to recover on demand

- Applicable skills and rules: `CLAUDE.md` (HUD invariants, C# style), `dotnet-efficient-validation`, `repository-cli-efficiency`; read `docs/design/` before touching HUD geometry or tooltips.
- Existing code: `NotchWindow.xaml` and `.xaml.cs` (root `Grid`, `StatusPopup`, `OnSizeChanged`, `ApplyPlacement`), `ProviderRing.xaml:23-37` and `TooltipCard.xaml:36-45` (tooltip tree), `SettingsWindow.xaml:255-339` (General tab) and `SettingsWindow.xaml.cs:129-130`, `App.xaml.cs:350-400` (`CreateSettingsViewModel`, `InitializeUi`).
- Contract or integration: `techspec.md#components-and-flow` and `techspec.md#test-approach` (MA-1..MA-4).

## Work

- [ ] T02.1 Bind a `ScaleTransform` (`LayoutTransform`) to `HudScale.Current.Factor` on the `NotchWindow` root `Grid`, the `StatusPopup` `Border`, and the `TooltipCard` root `Border`; scale `MinWidth` and `MinHeight` in code-behind on `HudScale` changes.
- [ ] T02.2 Add the "HUD size" section (slider with ticks every 5, label, three presets, Apply, applied and error text, automation names) above the Startup section; hide only the Startup section when `Startup` is null; remove the Providers jump in `SettingsWindow.xaml.cs`.
- [ ] T02.3 In `App`, load `HudSizeStore` and set `HudScale.Current.Percent` before `_notchWindow.Show()`, and fill `SettingsViewModel.HudSize`.
- [ ] T02.4 Add the smaller-width cases to `NotchPlacementTests` (TC-08), build, and prepare the MA-1..MA-4 script for the visual check.

## Acceptance criteria

- At 50%, 75%, 125%, and 150% the capsule, popup, and tooltip card scale together, with no clipped element (checked at MA-1).
- Changing the size keeps an unmoved HUD centered on the top edge and a dragged HUD on screen (TC-08, MA-2).
- After a restart the stored size is applied before the first paint (MA-3).
- The General tab opens with HUD size when `Startup` is null (MA-4).
- `WndProc`, `WindowStyles.EnableNonActivating`, and window flags have no diff, and clicking a ring does not steal focus (MA-4).
- The App project builds with 0 warnings and the QA greps are clean.

## Verification

- Unit: TC-08 placement cases.
- Integration: none beyond the build.
- E2E: omitted by .NET desktop policy.
- Manual: MA-1..MA-4 at the visual check (owner: the human; launch through the Windows MCP `App` tool).
- Commands: `rtk dotnet build src/TokenHound.App --no-restore`, `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`, and the QA-01..QA-05 greps over the diff files.
- Environment dependency: the app is launched through the Windows MCP `App` tool or by the human; no extra authorization.
- Expected evidence: build output, the test count, the QA greps, and the human's visual check result.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`, `src/TokenHound.App/UI/Controls/TooltipCard.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`, `src/TokenHound.App/App.xaml.cs`, `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs`

## Observability and recovery

- Operational signal: `Log.Debug` of the applied percentage when the scale changes.
- Recovery: revert the diff, or choose 100% and Apply; the HUD returns to its previous look.

## Handoff

- Produced result: The HUD capsule root, the status popup `Border`, and the `TooltipCard` root `Border` apply a `ScaleTransform` through `LayoutTransform`, bound to `HudScale.Current.Factor`. `NotchWindow` scales its `MinWidth` and `MinHeight` when the factor changes (new partial `NotchWindow.Scale.cs`). Settings → General has a "HUD size" section (slider 50–150 with ticks every 5, percentage label, Small, Default, and Large presets, Apply, applied and error text, automation names) above the Startup section; the Startup section hides when `Startup` is null, the tab collapses only when both models are null, and the Providers jump applies only in that case. `App` loads the stored size into `HudScale.Current` before the HUD is created and fills `SettingsViewModel.HudSize` through the new `HudSizeSettingsViewModel.Create`. TC-08 is covered by three new `NotchPlacementTests` cases.
- Changed files: created `src/TokenHound.App/UI/Windows/NotchWindow.Scale.cs`; modified `NotchWindow.xaml`, `NotchWindow.xaml.cs` (one line: `InitializeScale();`), `TooltipCard.xaml`, `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `App.xaml.cs` (two lines), `HudSizeSettingsViewModel.cs` (static `Create`), `NotchPlacementTests.cs`.
- Checks: `dotnet build src/TokenHound.App` 0 errors, 0 warnings; Infrastructure.Tests 980 passed, 0 failed. QA-01, QA-02, QA-03 clean; QA-04 all touched `.cs` files ≤ 300 lines except `App.xaml.cs` (467, pre-existing and recorded in the baseline; +2 lines); QA-05 one hit in `NotchWindow.xaml.cs:106` (`WndProc`, pre-existing and untouched). `WndProc`, `WindowStyles.EnableNonActivating`, and the window flags have no diff (DEC-11).
- Validated state: base `2fe5515` plus the uncommitted working tree of this feature; no environment dependency.
- Deviations (within the approved intent): the scale logic lives in a new partial file `NotchWindow.Scale.cs` instead of `NotchWindow.xaml.cs`, to keep that file under the 300-line limit; `SettingsWindow.xaml` was re-indented around the Startup block (diff noise only).
- Open items: MA-1..MA-4 (visual acceptance) are pending the human visual check. Reservation hits: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
