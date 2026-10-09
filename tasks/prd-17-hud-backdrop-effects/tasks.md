# Implementation plan — HUD Backdrop Effects

## Stable sources

- PRD: `tasks/prd-17-hud-backdrop-effects/prd.md`
- TechSpec: `tasks/prd-17-hud-backdrop-effects/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Desktop spike verdict on the Acrylic mechanism, with screenshots and measurements | PRD 16 acceptance, desktop availability, merged HIL 1+2 | T02 (or exception HIL) |
| T02 | Acrylic HUD capsule with Settings toggle, live fallback, and contour sync | T01 verdict and any exception HIL decision | Visual check, independent review, HIL 3 |

Serial. T02 builds on the candidate T01 selects; no production code is written in T01.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Acrylic in every docking mode | T01, T02 | TC-04, TC-05 |
| OBJ-02 | `prd.md#outcomes-and-metrics` | PRD 16 behavior preserved | T02 | TC-03, TC-05 |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Toggle and clean fallback | T02 | TC-01, TC-02, TC-06 |
| US-01..03 | `prd.md#stories-and-journeys` | Docked user, opt-out user, unsupported system | T02 | TC-05, TC-06 |
| FR-01 | `prd.md#functional-requirements` | Acrylic material | T01, T02 | TC-04, TC-05 |
| FR-02 | `prd.md#functional-requirements` | Material inside contour only | T01, T02 | TC-04, TC-05 |
| FR-03 | `prd.md#functional-requirements` | Outside click-through | T01, T02 | TC-03, TC-05 |
| FR-04 | `prd.md#functional-requirements` | Settings toggle, default on | T02 | TC-02, TC-06 |
| FR-05 | `prd.md#functional-requirements` | Solid fallback | T02 | TC-01, TC-06 |
| FR-06 | `prd.md#functional-requirements` | Live availability changes | T02 | TC-01, TC-06 |
| FR-07 | `prd.md#functional-requirements` | Coherent with HUD changes | T02 | TC-05 |
| NFR-01 | `prd.md#non-functional-requirements` | Focus and composition | T01, T02 | TC-03, TC-04, TC-05 |
| NFR-02 | `prd.md#non-functional-requirements` | Contrast ≥ 4.5:1 | T02 | TC-07 |
| NFR-03 | `prd.md#non-functional-requirements` | No polling, idle cost | T01, T02 | TC-08 |
| NFR-04 | `prd.md#non-functional-requirements` | Undocumented API isolated, fail closed | T01, T02 | Exception HIL, TC-01 |
| NFR-05 | `prd.md#non-functional-requirements` | Boundaries and settings pattern | T02 | TC-02, scoped diff |
| PD-01..04 | `prd.md#user-experience` | Acrylic, toggle default on, silent fallback, spike decides trade-off | T01, T02 | TC-04, TC-06, exception HIL |
| DEC-01..08 | `techspec.md#technical-decisions` | Companion, candidates, go/no-go, tint, sync, policy, setting, TFM | T01, T02 | Matching work items |
| TC-01..08 | `techspec.md#test-approach` | Unit and manual scenarios | T01 (TC-04), T02 (others) | MTP output, screenshots |

## Tasks

- [T01 — Prove an Acrylic mechanism that survives a never-activated HUD](done/task_01.md): verdict per candidate with screenshots, drag lag, and publish size; exception HIL when PD-04 applies.
- [T02 — Ship the Acrylic capsule with toggle and fallback](done/task_02.md): production companion, policy, setting, and full manual matrix.

## Coverage gate

- Coverage: pass; every PRD and TechSpec obligation maps to a task and evidence.
- Traceability: pass; stable IDs link PRD, decisions, components, and scenarios.
- Dependencies: pass; acyclic T01 → T02, both gated on PRD 16 acceptance.
- Atomicity: pass; T01 is a decision-producing spike, T02 one usable delivery with its tests.
- Executability: pass for builds and unit tests; desktop checks need the user's screen.
- Validation profile: .NET desktop, MTP executable route, `--minimum-expected-tests 1`; E2E omitted by .NET desktop policy.
- Idempotency: reruns reuse recorded spike evidence while the Windows build and candidate code are unchanged.

## Assumptions and open items

- Assumption: Windows 11 22H2 or later on the validation machine (verify the build in T01).
- Open item: PD-04 trade-off, decided at an exception HIL inside T01 only if the best viable candidate is B or C.
- Required environment: user desktop released for Windows MCP automation, three mixed-DPI monitors, disposable click target; authorization pending until the user releases the desktop.

## State

- [x] T01 — completed 2026-10-09: candidate A passes; verdict in `validation.md`; T02 contract choices at exception HIL (workflow DEC-05)
- [x] T02 — completed 2026-10-09: implementation, 1101 tests, manual matrix in `validation.md`; remaining TC-05 cases go to the visual check

## Problems and solutions

- T01 desktop automation: PowerShell (DPI-unaware) `SetCursorPos` scaled coordinates by 1.5 on the 150% primary monitor, so two scripted drags pressed and dragged inside the user's Chrome page (text selection only, no button or input). Fix: call `SetThreadDpiAwarenessContext(-4)` before any scripted mouse input and guard each press with a DPI-aware `WindowFromPoint` check of the target process. Windows MCP `Click`/`Move` already use physical coordinates.
- T02: runtime fill swaps never repainted because `HudContourDecorator.Background` used `AddOwner` without metadata (no `AffectsRender`); fixed at the registration. Energy saver on AC power is not visible in `GetSystemPowerStatus`; read from the power-setting notifications instead.
- T01: one evidence screenshot captured the user's browser session; deleted, and DEC-03 evidence cites panel-foreground screenshots only.
