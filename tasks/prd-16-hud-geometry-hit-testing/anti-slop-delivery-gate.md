# Anti Slop delivery gate: HUD Edge Geometry and Contour Hit Testing

- Date: 2026-10-09. Mode: during (workflow DEC-04). Build: `2bc32ef` Release.
- Scope: what this feature changes in the Windows HUD (capsule contour geometry, stroke, passive shadow, mouse pass-through). Palette, typography, provider content, menus, and copy are unchanged by contract (PRD PD-01, FR-11).
- Design Read: Windows desktop HUD for developers who monitor AI quotas, in the existing native dark capsule language set by PRD PD-01..04, dial ENERGY 1 / RHYTHM 1 / MOTION 1. Direction source: PRD PD-01..04 (approved at HIL 1), so this is not a draft without direction.
- Evidence: `validation.md` sections "T03 desktop acceptance, 2026-10-09" and "Correction round 1 evidence (codereview_01 CR-02), 2026-10-09"; collage `t03-visual-gate-20261009.png`; human visual approval in `workflow.md` Events.

## Block 1: Hard Gate

| Rule | Result | Evidence |
| --- | --- | --- |
| R-02 em dash | PASS | No user-facing text added; HUD and popup strings unchanged in the diff |
| R-03 responsive layout | PASS (adapted) | Desktop HUD, not a web layout: content stays inside the contour at sizes 50/80/100/125/150% and three DPIs, no clipping (validation.md) |
| R-17 statistics | PASS | No new numbers; provider values are real readings |
| R-18 testimonials | N/A | None |
| R-23 assets | PASS | No new logo, avatar, or image; provider glyphs unchanged |
| R-24 navigation | N/A | No navigation |
| R-25 contrast | PASS | No new text or text color; StatusPopup and hover card unchanged and readable in captures |
| R-26 interactive elements | PASS | Inside the contour every existing action works (menu, Position, Refresh, popup dismiss, drag); outside points pass to other apps by design |
| R-27 UI states | PASS | Empty HUD (`cr01-empty-*.png`), busy/refreshing and result StatusPopup, populated, and attention states captured |
| R-28 FAQ | N/A | None |
| R-32 keyboard | PASS (by invariant) | The HUD is non-activating by AGENTS.md invariant and never takes focus; keyboard access to settings and actions stays in the Settings window and tray, unchanged (NFR-04) |
| R-33 patch scripts | PASS | Feature written in source (`HudContour*`, `HudShadow*`) |
| R-34 themes | N/A | Single existing dark theme; no toggle shipped |
| R-35 verify before delivery | PASS | Release build run; six modes, sizes, DPI, drag, restart, popups exercised on the desktop |
| R-36 claims | PASS | None |
| R-37 direction | PASS | PRD PD-01..04 direction and dials 1/1/1 |
| R-38 real content | PASS | No fabricated content |

## Block 2: Purpose-Gate

| Rule | Result | Reason written |
| --- | --- | --- |
| R-01 gradients/glow | PASS | None added |
| R-04 icons | PASS | No new icons |
| R-06 typography | PASS | Unchanged |
| R-07 background pattern | PASS | None |
| R-08 arrows | PASS | None |
| R-09 badges | PASS | Existing provider glyphs only; no new badge |
| R-10 glassmorphism | PASS | None (backdrop materials are PRD 17) |
| R-12 shadow | PASS | One existing shadow, kept for elevation of a floating HUD over other apps; now drawn by a passive companion so it never captures input |
| R-13 glow | PASS | None |
| R-14 cards | N/A | None |
| R-19 animation | PASS | No new motion; existing busy pulse unchanged (MOTION 1) |
| R-22 illustrations | N/A | None |

## Block 3: Liveliness

| Check | Result | Evidence |
| --- | --- | --- |
| Dials declared | YES | ENERGY 1 / RHYTHM 1 / MOTION 1 above |
| Output consistent with dials | YES | Calm native capsule, no new motion |
| One focal point | YES | The capsule with provider rings |
| Structural whitespace | YES | Padding inside the contour unchanged; gutters outside are click-through |
| One deliberate accent | YES | Provider ring colors (existing), no new accent |
| Identity motif | YES | Inverse rounded joins where the capsule meets a screen edge, repeated in every docked mode |
| Design Read declared | YES | Above |

## Block 4: Craftsmanship and Quality Locks

| Check | Result | Evidence |
| --- | --- | --- |
| C-1 intentionality | PASS | Each geometry choice traces to PRD FR-01..03 and PD-01..04 |
| C-2 functional completeness | PASS | See R-26 |
| C-3 content-driven | N/A | No sections |
| C-4 resilience | PASS | Empty, busy, populated states; six modes; three DPIs; restart |
| C-5 evidence | PASS | No claims |
| R-05 template layout | N/A | Not a page |
| R-11 radius | PASS | Free is fully rounded by PD-02; docked modes use edge joins; menus keep their own radius |
| R-15 CTA | N/A | None |
| R-16 buzzwords | PASS | None |
| R-20 identity | PASS | Edge-attached capsule with inverse joins is specific to this HUD |
| R-21 dark mode | PASS | Existing dark HUD kept for a developer tool overlay; unchanged |
| R-29 palette | PASS | Unchanged |
| R-30 clone | PASS | Windows-native rendition of the project's own design reference |
| R-31 reasons written | PASS | Reasons in PRD PD-01..04 and TechSpec DEC-01..08 |

Result: PASS. No FAIL item. Gate closed for this feature's scope.
