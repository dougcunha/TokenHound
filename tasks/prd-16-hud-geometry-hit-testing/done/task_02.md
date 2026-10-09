# Stable execution context

Load the current versions in this order: `prd.md`, `techspec.md`, then this file. Recover only missing/changed sources.

# T02: Render the interactive contour and passive shadow

## Outcome

The existing provider HUD uses the new contour, receives input inside fill/stroke, and allows clicks through outside space and its visible passive shadow.

## Dependencies and boundaries

- Depends on: completed T01. Unblocks: T03.
- Scope: WPF decorator, shared fill/stroke/envelope, companion/controller, checked native shadow styles/bounds, existing menu/popup integration.
- Excludes: settings schema, provider behavior, backdrop/animation/theme redesign, global input hooks.

## Traceability

| Source | Section | Covered obligation |
| --- | --- | --- |
| FR-01, FR-03..06, FR-10..11, PD-01, PD-04 | prd.md#functional-requirements; user-experience | Painted/input boundary and existing controls |
| NFR-01..05 | prd.md#non-functional-requirements | Focus, idle, boundaries and honest evidence |
| DEC-02..05, DEC-08, CMP-02..06, TC-03..04 | techspec.md#technical-decisions; components-and-flow | Alpha routing and passive companion |

## Context to recover on demand

- T01 handoff and frame contract; TechSpec Lifecycle and native interface; quality baseline.
- ApplyChrome callers and current event wiring; source spans in TechSpec Sources.
- Current context menu/StatusPopup behavior and WindowStyles regression tests.

## Work

- [x] T02.1 Implement frozen WPF geometry adaptation and a sealed decorator retaining CapsuleBorder's content/padding/menu/anchor role. Render fill/stroke only; use shared geometries for WPF hit testing.
- [x] T02.2 Implement the shadow-only companion and owning controller. Exclude the full painted envelope after the effect; apply transparent/non-activating styles before showing. Handle frame/location/visibility/close without polling or duplicate windows.
- [x] T02.3 Integrate XAML/chrome/controller with existing orientation and popup behavior. Preserve the main WndProc focus hook and existing native composition flags. Add checked native shadow operations without weakening main input.
- [x] T02.4 Add pure style-policy/regression tests, build affected projects, and run targeted HudContour tests plus the existing WindowStyles class filter using MTP.
- [x] T02.5 Perform real different-process click/focus smoke checks in all modes on the primary display, including shadow-only points; record successes/failures in validation.md. These checks are manual desktop work, not an E2E suite.

## Acceptance criteria

- Fill/stroke remain interactive and outside points, including visible shadow, reach the underlying process for left/right/double clicks.
- Existing glyphs/values/states/menu/popups remain usable; the companion never activates or obscures provider content.
- One companion follows visibility and exits with its owner; native failures are checked and visible in structured logs.
- No HTTRANSPARENT-only claim, click forwarding, arbitrary delay, new timer/global hook, or diagnostic suppression is introduced.

## Verification

- Unit: TC-03 style composition, bit preservation and main/companion distinction; existing style regressions.
- Integration: App build and actual desktop rendering/input boundary. Pure tests do not prove WPF alpha routing.
- E2E: omitted by .NET desktop policy.
- Manual: coordinator TC-04 primary-display smoke, screenshot identity/shape and hide/show/exit checks. Full matrix remains T03.
- Commands: TechSpec build/targeted route; additional `--filter-class "*WindowStylesTests*"` after `--` for the existing class. Enforce nonzero count and check `$LASTEXITCODE` for each invocation.
- Evidence: current build/test outputs, mouse/focus result table, screenshots, failure/lifecycle logs if applicable.

## Affected files

- Create: `src/TokenHound.App/UI/Controls/HudContourGeometry.cs`, HudContourDecorator.cs; `UI/Windows/HudShadowWindow.xaml`, .xaml.cs, HudContourController.cs; `Interop/HudShadowInterop.cs`.
- Modify: `src/TokenHound.App/Interop/WindowStyles.cs`; `UI/Windows/NotchWindow.xaml`, .xaml.cs, .Dock.cs; existing Infrastructure test csproj for pure links if needed.
- Create: `tests/TokenHound.Infrastructure.Tests/Interop/HudContourStyleTests.cs`.
- Update: validation.md and this handoff.

## Observability and recovery

Log actual frame/style/lifecycle changes and numeric native errors, with no per-pointer logs. Reversal restores the existing Border and removes companion ownership; saved placement/provider settings remain unchanged.

## Handoff

- Produced result: completed T02. Canonical WPF fill/stroke/envelope, provider decorator, passive owned shadow/controller, checked native operations and existing window integration are implemented. Fresh six-mode primary-display outside-click/focus smoke, side-to-Free drag, hide/show and graceful Exit/disposal passed after the shadow scaling correction.
- Changed files: new HudContourGeometry.cs, HudContourDecorator.cs, HudShadowWindow.xaml/.xaml.cs, HudContourController.cs, HudShadowInterop.cs and HudContourStyleTests.cs; modified WindowStyles.cs and NotchWindow.xaml/.xaml.cs/.Dock.cs. Feature validation/evidence files updated. No provider/settings/Core production changes.
- Checks: App/test Release builds passed with zero warnings/errors. Native MTP targeted HudContour: 23 passed, 0 failed/skipped, exit 0; WindowStylesTests: 12 passed, 0 failed/skipped, exit 0; minimum expected tests 1. Final App rebuild after formatting passed. Scoped QA inspection found no new suppression/polling/global interception; checked native errors are logged and stop shadow retry. git diff --check passed. New files are 34/69/71/94/146/192 lines, before any further changes.
- Validated state: base 6f2e92b; final T02 shadow uses a Canvas and transforms the whole effect source. App Release rebuild: zero warnings/errors, exit 0. Existing 23 contour and 12 style passes remain valid because linked test sources/configuration did not change. Repository processes 10988 and 35448 exited through tray Exit; final logs show controller disposal and exit code 0. Target 19216 closed with Escape; installed process 23240 restored. Exact current mouse/native/render evidence is in validation.md.
- Task review: acceptance items map to current rendering/native code, fresh cross-process logs and lifecycle evidence. No new blocking quality hit or reservation; new files remain under 300 lines and methods under 30. Native callback signatures retain their prescribed arguments. T02.1..5 are complete; coordinator approval permits movement to done/.
- Open items: full DPI/size/restart/provider-state matrix, physical display disconnection, human visuals, Anti Slop delivery gate, independent review and HIL 3 remain T03/flow obligations. ContextBrake reached RED during this task boundary; no T03 writing started.

### ADR candidates

None. Implementation follows the approved two-surface contract.
