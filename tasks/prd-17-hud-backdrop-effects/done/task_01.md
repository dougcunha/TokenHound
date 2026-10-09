# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/prd.md`
2. `tasks/prd-17-hud-backdrop-effects/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Prove an Acrylic mechanism that survives a never-activated HUD

## Outcome

A recorded verdict for each candidate in TechSpec DEC-02, stating whether it shows a translucent blur inside the contour while the HUD is never activated, with screenshots and measurements. The task ends with either a selected candidate for T02 or an exception HIL (PD-04 or no viable candidate).

## Dependencies and boundaries

- Depends on: PRD 16 acceptance; user desktop released; merged HIL 1+2.
- Unblocks: T02, or an exception HIL.
- In scope: throwaway prototypes in a scratch project or branch, desktop observation, measurements.
- Out of scope: production code in `src/`, settings, commit of prototype code.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-02, FR-03 | `prd.md#functional-requirements` | Material, contour-only clip, preserved click-through |
| NFR-01, NFR-03, NFR-04 | `prd.md#non-functional-requirements` | Focus, idle cost, undocumented-API isolation |
| PD-04 | `prd.md#user-experience` | Trade-off decided from evidence |
| DEC-02, DEC-03, DEC-08, TC-04 | `techspec.md` | Candidates, go/no-go, TFM size delta |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`; AGENTS.md HUD invariants and desktop-launch rules (Windows MCP `App` tool; screenshot display index in PRD 16 snapshot L-03).
- Existing code: `HudShadowWindow.xaml.cs` (companion pattern), `HudContourController.cs` (frame and z-order), `WindowPlacement.cs:181-244` (DWM wrapper).
- Research summary: TechSpec DEC-02 and DEC-03 links.

## Work

- [x] T01.1 Record the Windows build, Transparency effects state, and display inventory.
- [x] T01.2 Candidate A: owned `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_NOREDIRECTIONBITMAP` window hosting a Windows.UI.Composition host backdrop brush clipped by a capsule path geometry, under a layered test capsule. Check translucency with another app focused, clip edge quality, click pass-through, drag behavior, and single-file publish size delta.
- [x] T01.3 (not run: A passed) Only if A fails: candidate B (`SetWindowCompositionAttribute` Acrylic accent on a region-clipped companion). Same checks, plus drag lag and whether the blur ignores Transparency effects.
- [x] T01.4 (not run: A passed) Only if A and B fail: candidate C (`DWMSBT_TRANSIENTWINDOW` on a non-layered companion with `SetWindowRgn`). Same checks.
- [x] T01.5 Write the verdict table in `validation.md` and the handoff. If the selected candidate is B or C, or none passes, stop at an exception HIL with screenshots and a recommendation.

## Acceptance criteria

- Each evaluated candidate has a pass/fail on DEC-03 with a screenshot taken while another application holds the foreground.
- Outside clicks reach a different-process target, and the HUD never takes focus, in every evaluated prototype.
- The selected candidate (if any) and its costs (edge quality, API status, drag lag, size delta) are explicit.

## Verification

- Unit: none (spike).
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: TC-04, run by the coordinator through Windows MCP; the human confirms screenshots at the exception HIL when raised.
- Commands: scratch build and `dotnet publish` of the prototype for the size delta; no repository test run required.
- Environment dependency: user desktop released for automation; pending.
- Expected evidence: verdict table, screenshots, publish sizes, drag observations.

## Affected files

- Create: `tasks/prd-17-hud-backdrop-effects/validation.md`, spike notes and screenshots in this folder.
- Modify: none in `src/`.

## Observability and recovery

- Operational signal: none (prototype only).
- Recovery: delete the scratch prototype; nothing to revert in the repository.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: verdict table in `validation.md#t01-spike-2026-10-09`. Candidate A (host backdrop brush in a `WS_EX_NOREDIRECTIONBITMAP` companion) passes DEC-03: translucent blur while the capsule is never activated and another process holds the foreground. Variants AL, AR, and ALR pass outside left/right/double click-through to a different-process target; the plain non-layered full-rect variant fails click-through. B and C not evaluated (gated on A failing). Recommendation: A with a window-region clip (AR/ALR); projection route (DEC-08) and clip route (DEC-02/PD-04) go to the human.
- Changed files: `validation.md` (new), `t01-*.png` evidence, `t01-spike-log-20261009.txt`. No file in `src/`. Prototype in the session scratchpad (`BackdropSpike`), not committed.
- Checks: scratch `dotnet build` and `dotnet publish` (release.yml flags) of the prototype and of `TokenHound.App` into the scratchpad; Windows MCP launches, clicks, screenshots; DPI-aware `WindowFromPoint` hit tests; GDI pixel sampling of the edge.
- Validated state: Git `2bc32ef` (repository unchanged by T01), Windows 10.0.26200.9457, transparency effects temporarily on and restored to 0, primary monitor at 150%.
- Open items: polygon region from the flattened PRD 16 contour untested (only round-rect); per-frame drag lag not measured; energy-saver state not determined; `DWMWA_USE_HOSTBACKDROPBRUSH` on a layered companion is empirical only; with FR-05, this machine (transparency off) shows the solid fallback by default.

### ADR candidates

- T01-ADR-01 — Acrylic via a host-backdrop composition companion under the layered HUD. Context: the HUD must stay layered for per-pixel click-through, and WPF refuses system backdrops on `AllowsTransparency` windows. Decision: draw the material in a raw, input-transparent, topmost companion HWND using `Compositor.CreateHostBackdropBrush` through `CreateDesktopWindowTarget`, kept directly below the HUD. Alternatives: undocumented accent Acrylic (B), `DWMSBT_TRANSIENTWINDOW` with a 1-bit region (C), self-drawn blur. Consequences: WinRT composition dependency (projection or hand interop), z-order sync by insert-after, region-based clipping. Evidence: `validation.md#t01-spike-2026-10-09`. TechSpec relationship: confirms DEC-01/DEC-02 candidate A; amends CMP-03 and DEC-05 details after the T01 exception HIL.
