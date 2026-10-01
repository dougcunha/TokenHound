# Implementation plan — HUD size setting

## Stable sources

- PRD: `tasks/prd-13-hud-size/prd.md`
- TechSpec: `tasks/prd-13-hud-size/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Persisted `HudSize` setting, `HudScale` holder, and `HudSizeSettingsViewModel` (slider and presets, Apply), tested on a temp settings file | — | T02 |
| T02 | The HUD, status popup, and tooltip card scale with the setting; Settings General tab gets the HUD size section; startup loads the stored size | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | 50–150% in steps of 5, default 100, clamped | T01 | TC-01 |
| FR-02 | `prd.md#functional-requirements` | Slider with label plus Small, Default, Large presets | T01, T02 | TC-04; MA-1 |
| FR-03 | `prd.md#functional-requirements` | Apply pattern, immediate effect, no restart | T01, T02 | TC-04, TC-05, TC-06; MA-1 |
| FR-04 | `prd.md#functional-requirements` | Whole capsule scales uniformly | T02 | TC-07; MA-1 |
| FR-05 | `prd.md#functional-requirements` | Status popup and tooltip scale too | T02 | MA-1 |
| FR-06 | `prd.md#functional-requirements` | Anchor kept, clamped inside the screen | T02 | TC-08; MA-2 |
| FR-07 | `prd.md#functional-requirements` | Persisted in its own settings section | T01 | TC-02, TC-03 |
| NFR-01 | `prd.md#non-functional-requirements` | Core pure; persistence in Infrastructure, scaling in App | T01, T02 | QA-03 |
| NFR-02 | `prd.md#non-functional-requirements` | Non-activating HUD invariants unchanged | T02 | DEC-11; MA-4 |
| NFR-03 | `prd.md#non-functional-requirements` | Crisp text and borders at every step | T02 | DEC-04; MA-1 |
| NFR-04 | `prd.md#non-functional-requirements` | Automation names and help text | T02 | MA-1 (UI review) |
| US-01..US-03 | `prd.md#stories-and-journeys` | Shrink, enlarge, return to default | T01, T02 | MA-1..MA-3 |
| DEC-01, DEC-02, DEC-07 | `techspec.md#technical-decisions` | Section, bounds, ViewModel | T01 | TC-01..TC-06 |
| DEC-03 | `techspec.md#technical-decisions` | `HudScale` shared holder | T01 | TC-07 |
| DEC-04..DEC-06, DEC-08..DEC-11 | `techspec.md#technical-decisions` | Scaling, tab visibility, wiring, invariants | T02 | MA-1..MA-4 |
| TC-01..TC-07 | `techspec.md#test-approach` | Settings, store, ViewModel, scale tests | T01 | `Infrastructure.Tests` |
| TC-08, TC-09 | `techspec.md#test-approach` | Placement cases and manual script | T02 | `NotchPlacementTests`; MA-1..MA-4 |

## Tasks

- [T01 — HUD size setting and ViewModel](done/task_01.md): persisted `HudSize` section, `HudScale`, and the Apply-pattern ViewModel, all covered by unit tests.
- [T02 — Scale the HUD and add the Settings section](done/task_02.md): the capsule, popup, and tooltip scale from `HudScale`, the General tab shows HUD size, and the app loads the stored size at startup.

## Coverage gate

- Coverage: pass. Every PRD requirement maps to a task and evidence.
- Traceability: pass. TC-09 groups the manual obligations (FR-04..FR-06, NFR-02..NFR-04) under MA-1..MA-4.
- Dependencies: pass. A single chain, T01 → T02, with no cycles; T02 consumes `HudScale` and the ViewModel from T01.
- Atomicity: pass. T01 is testable without WPF; T02 is the UI slice, checked by a build and the visual check.
- Executability: pass. Commands come from `CLAUDE.md`. The new ViewModel and `HudScale` need `<Compile Include>` links in the test csproj (T01).
- Validation profile: pass. E2E is omitted for .NET desktop; unit tests cover logic, and the manual script covers visuals.
- Idempotency: pass. Re-running a task rewrites the same files and the same settings section.

## Assumptions and open items

- Assumption: the lower bound of 50% is readable. If MA-1 shows otherwise, raise `MINIMUM_PERCENT` (one constant).
- Open item: none blocking.
- Required environment: the Windows MCP `App` tool to launch the app for MA-1..MA-4, or the human launches it | no extra authorization needed.

## State

- [x] T01 — done
- [x] T02 — done

## Problems and solutions

- T01: the approved step of 10 conflicted with the presets 75 and 125, so the step became 5 (PRD FR-01, TechSpec DEC-02, TC-01 updated). `UserSettingsFile.MergeWithDefaults` dropped unknown-to-it sections, so `HudSize` was added there. Evidence: `done/task_01.md` handoff.
