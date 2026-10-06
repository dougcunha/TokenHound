# Workflow Log: PRD 14 — HUD multi-monitor and edge docking

## Feature Context
- **Slug**: `prd-14-hud-multimonitor-docking` (ROADMAP item "PRD 13: Multi-Monitor & Edge Docking"; folder number 13 is already used by `prd-13-hud-size`).
- **Objective**: Let the user choose which display hosts the HUD and dock it to a screen edge (Top Center, Top Right, Right edge vertical pill), with DPI-aware clamping, persisted preferred display and docking, and a defined default and migration for existing saved coordinates.
- **Process Level**: `sdd-full` (decided at HIL 0).
- **Git Base Commit**: `c4b55b9cb1b372f819f59639e6836388e78ebed5`. Pre-existing worktree changes not owned by this flow: `.agents/settings.json` (modified), untracked `.agents/hooks/`, `.agents/settings.local.json`, `.context-brake/`, `.opencode/`, `context-brake.config.json`. This flow appended one line to `tasks/triage-log.jsonl`.

## Decisions

### DEC-01: Process level (sdd-full)
- **Date**: 2026-10-05
- **Decision**: The human chose `sdd-full` at HIL 0, matching the rubric (S1 persisted HUD position shape, S5 default orientation and migration, S8 settings migration, S6 more than three obligations, S4 App + Infrastructure). Record: `tasks/triage-log.jsonl`, 2026-10-05.
- **Stops**: all gates (HIL 1, HIL 2, visual check, review, HIL 3).
- **Human text**: "sdd-full (Recommended)".

### DEC-02: Product decisions before the PRD
- **Date**: 2026-10-05
- **Decision**: Answers to the four product questions asked before drafting the PRD:
  1. Placement model: docked presets plus a "Free" mode; dragging the HUD switches to Free and stores the coordinates (current behavior preserved). Human text: "Presets + Livre (Recommended)".
  2. Default and migration: a fresh install starts docked at Top Center (horizontal, as today); an existing saved Left/Top becomes Free at the same coordinates, with no visual change on upgrade. Human text: "Topo centro; salvo vira Livre (Recommended)".
  3. Display selection: "Primary monitor" (follows the primary) or a specific monitor; when the chosen monitor is disconnected the HUD moves to the primary and returns automatically on reconnection. Human text: "Primário ou monitor específico (Recommended)".
  4. Docked positions in this delivery: Top Center, Top Right, Right edge (vertical), plus Top Left and Left edge (vertical), which extend the original ROADMAP list. Human text: "Topo centro, Topo direita, Borda direita (vertical), Topo esquerda + borda esquerda".

### DEC-03: HIL 1 approved
- **Date**: 2026-10-05
- **Decision**: The human approved `prd.md` as written, including FR-17 (context-menu "Position" submenu, agent proposal) and FR-18 (switching from Free to docked keeps the hosting display and makes it the preference when it differs).
- **Human text**: "Aprovar como está (Recommended)" (HIL 1) and "Continuar aqui (Recommended)" (session).
- **Scope**: sha256 prd.md=ac90b343d87e.

### DEC-04: HIL 2 approved
- **Date**: 2026-10-05
- **Decision**: The human approved `techspec.md` and the seven-task plan (`tasks.md`, `task_01.md`..`task_07.md`), with preparatory refactoring not recommended and live apply in the Settings section (DEC-13). This authorizes implementation and corrections within that contract.
- **Human text**: "Aprovar (Recommended)" (HIL 2) and "Continuar aqui (Recommended)" (session).
- **Scope**: sha256 techspec.md=132e117bcb2b, tasks.md=8648a554bc28, task_01.md=e1632fc46269, task_02.md=b5f91efdab27, task_03.md=b34452c35f76, task_04.md=7954d52ee2d3, task_05.md=0ff50956ce99, task_06.md=95d9ddb6baee, task_07.md=b995a64420ea.

### DEC-05: Visual check approved
- **Date**: 2026-10-05
- **Decision**: The human ran the remaining visual check on the dev build (MA-6 disconnect/reconnect of the preferred display, the rest of MA-7 with taskbar auto-hide and HUD size 125% while docked Top Right, and the FR-07 primary-display change) and approved the visuals with no adjustments.
- **Human text**: "Aprovar visuais".
- **Scope**: uncommitted feature diff on base c4b55b9; dev HUD PID 4044.

### DEC-06: Reservations HIL — correct all
- **Date**: 2026-10-06
- **Decision**: The human chose to correct all reservations of `codereview_01` (CR-01, CR-02, CR-03) in this session, then run a new delegated review.
- **Human text**: "Corrigir todas as resalvas nessa sessão".
- **Scope**: codereview_01/codereview.md CR-01..CR-03; correction round 1.

### DEC-07: HIL 3 — adjustment requested before acceptance
- **Date**: 2026-10-06
- **Decision**: The human did not accept yet and asked to reorder the Settings General tab so the "Startup" (start with Windows) card comes first. This is a layout adjustment the human authorized directly; it becomes task T11 before a new delegated review and a new HIL 3. O-02: keep the current HUD settings (Top Center on Display 1); the pre-feature backup stays on disk. Escalation: after PRD-14 closes, plan the suggested refactoring (`sdd-plan-refactoring`) for `SettingsWindow.xaml` and `DialogResources.xaml` as its own feature.
- **Human text**: "mude a ordem da config. Coloque o inicio com o windows no começo" (HIL 3), "Manter as atuais (Recommended)" (O-02), "Planejar agora" (refactoring).
- **Scope**: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` General tab card order; no behavior change.

### DEC-08: HIL 3 accepted
- **Date**: 2026-10-06
- **Decision**: The human accepted the delivery (T01..T07, corrections T08..T10, adjustment T11; latest review `codereview_03` APPROVED) and authorized conventional commits on local `main` without push. Accepted open notes: FR-18 reading per DEC-09, SystemAware DPI caveat. Next: plan the refactoring from DEC-07 in this session.
- **Human text**: "Aceitar e commitar (Recommended)" (HIL 3) and "Continuar nesta sessão (Recommended)" (session).
- **Scope**: uncommitted feature diff on base c4b55b9 as reviewed in codereview_03.

## Events

- 2026-10-05: Feature state opened. Next: product questions, then `prd.md` and HIL 1.
- 2026-10-05: Product questions answered (DEC-02). Next: `prd.md`, then HIL 1.
- 2026-10-05: `prd.md` written (OBJ-01..03, US-01..05, FR-01..18, NFR-01..06). FR-17 (context-menu submenu) is an agent proposal; FR-18 (Free to docked keeps the hosting display) closes a gap found in review. Snapshot written. HIL 1 pending.
- 2026-10-05: HIL 1 approved (DEC-03). Next: `techspec.md` with `sdd-create-techspec`.
- 2026-10-05: `techspec.md` written (DEC-01..DEC-14, CMP-01..CMP-18, TC-01..TC-11, MA-1..MA-8, QA-01..QA-07). Preparatory refactoring not recommended (`App.xaml.cs` 468 lines and `SettingsWindow.xaml` 949 lines are touched in one or two places each). Findings that shaped the design: `WindowPlacement.GetWorkArea` divides by the window's current DPI, so docking works in physical pixels with `SetWindowPos`; `NotchWindow.PersistPosition` writes a fresh `{ Left, Top }` record that would drop new fields, so one service owns writes with `with`; the App has no DPI manifest (DEC-03 holds in either awareness mode); tooltips render in separate visual trees, so their placement uses a shared observable like `HudScale`; the net10.0 test project links pure App files; `DragMove` also runs on a plain click.
- 2026-10-05: `tasks.md` + `task_01.md`..`task_07.md` written (DAG T01 → T02 → T03 → T05 → T06 → T07, T02 → T04 → T05). After review the window work was split into a docking engine (T05) and edge chrome (T06), T04 gained a startup display log so it is verifiable alone, and QA-07 was scoped to the task diff. HIL 2 pending.
- 2026-10-05: HIL 2 approved (DEC-04). Next: T01 through `sdd-orchestrate-tasks`.
- 2026-10-05: T01 done: placement mode and display preference persisted with DEC-02 resolution; test build 0 warnings, Infrastructure.Tests 999 passed (27 in *HudPosition*).
- 2026-10-05: T02 done: pure docking, display resolution, and edge layout rules; Infrastructure.Tests 1025 passed (36 in *Placement*), App build 0 warnings.
- 2026-10-05: T03 done: HudPlacementService with DEC-08/DEC-09 rules and HudPlacementContext; Infrastructure.Tests 1035 passed (10 in *HudPlacementService*), App build 0 warnings.
- 2026-10-05: T04 done: DisplayCatalog and pixel window helpers; startup log lists 3 displays with device paths; the process runs SystemAware (coordinates virtualized by the 150% system scale; DEC-03 still holds). Installed TokenHound (PID 22236) stopped for testing; user settings.json backed up to the session scratchpad. Infrastructure.Tests 1035 passed.
- 2026-10-05: T05 done: docking engine, coalesced display events, drag → Free, Position submenu (+ HudSubmenuItemStyle/HudCheckMenuItemStyle). Manual fix: dock size from ActualWidth × DPI (GetWindowRect is stale during SizeChanged). MA-1, MA-2 positions, MA-4 passed on the primary display; 1035 tests passed.
- 2026-10-05: T06 done: edge chrome (vertical capsule, outline per edge, tooltip and popup inward, min-size swap). MA-2 side edges, MA-3, MA-1 regression passed on the primary display; 1035 tests passed.
- 2026-10-05: T07 done: Settings HUD placement section (+ DialogComboBoxStyle, LabelItemTemplate). MA-8, MA-5, and the cross-scale part of MA-7 passed on the three-display setup; 1043 tests passed. All tasks complete; next: human visual check (MA-6, rest of MA-7, FR-07 primary change), then the delegated review.
- 2026-10-05: Visual check pending. Dev HUD relaunched (PID 4044) for the human. Settings backup also at %LOCALAPPDATA%\TokenHound\settings.json.before-prd14-20261005.bak; installed TokenHound (D:\Apps\TokenHound) still stopped. Windows 11 recipes: MA-6 via Settings > System > Display > Disconnect this display, then Extend; MA-7 taskbar via Taskbar > Automatically hide; FR-07 via Make this my main display. Session ended at 93% context (ContextBrake CRITICAL).
- 2026-10-05: Visual check approved (DEC-05). Dev HUD already closed by the human; installed TokenHound (D:\Apps\TokenHound) running again (PID 28760). User settings not restored yet (O-02, decide at HIL 3). Worktree recorded (HEAD c4b55b9, 44 porcelain lines). Next: delegated `sdd-review-code` in `codereview_01/` with --base c4b55b9.
- 2026-10-05: Delegated review received: `codereview_01/codereview.md`, status APPROVED WITH RESERVATIONS, Execution: delegated reviewer. Worktree check clean (HEAD c4b55b9, porcelain unchanged; only the report folder added). Reservations CR-01..CR-03 (Low, QA-05/QA-06); escalation suggestion `sdd-plan-refactoring` for `SettingsWindow.xaml` (949 → 1051 lines); FR-18 interpretation note (DEC-09). Reservations HIL pending.
- 2026-10-06: Reservations HIL decided (DEC-06): correct CR-01..CR-03 in this session. Next: `sdd-plan-corrections` for codereview_01.
- 2026-10-06: Corrections planned in `codereview_01/`: T08 (CR-01), T09 (CR-02), T10 (CR-03, depends on T08+T09). Round 1 started under DEC-06.
- 2026-10-06: Corrections T08 (CR-01), T09 (CR-02), T10 (CR-03) done in `codereview_01/done/`: App build 0 warnings, test build 0 warnings, 1043 tests passed; `App.xaml.cs` 459 lines, `InitializeUi` 25 lines. Manual startup smoke left for HIL 3 (Windows MCP disconnected). Next: delegated re-review in `codereview_02/` (previous `codereview_01/`).
- 2026-10-06: Delegated re-review received: `codereview_02/codereview.md`, status APPROVED, Execution: delegated reviewer; worktree check clean (porcelain unchanged). Limitations: post-correction startup smoke not verified (human or agent at HIL 3); escalation suggestion `sdd-plan-refactoring` for `DialogResources.xaml` (375 → 561) and `SettingsWindow.xaml` (949 → 1051); DPI note (DpiChanged must trigger ApplyPlacement if a per-monitor manifest is added). Next: startup smoke via Windows MCP, then HIL 3.
- 2026-10-06: Post-correction startup smoke passed via Windows MCP (dev PID 32644): log lists 3 displays with device paths, restores "TopCenter" on Display 1, 'HUD window displayed successfully.'; screenshot shows the capsule centered and flush at the top of Display 1; context menu shows Position submenu; Settings shows HUD placement (Top center, Display 1). Installed TokenHound stopped for the smoke and restarted (PID 17564). HIL 3 pending.
- 2026-10-06: HIL 3 not accepted yet (DEC-07): T11 reorders the Settings General tab (Startup first). O-02 closed: current settings kept. Refactoring of SettingsWindow.xaml/DialogResources.xaml to be planned after PRD-14 closes.
- 2026-10-06: T11 done (`done/task_11.md`): Startup card first in the Settings General tab; App and test builds 0 warnings, 1043 tests passed; visual check via Windows MCP confirmed the order. Installed TokenHound restarted (PID 28772). Next: delegated review `codereview_03` (previous `codereview_02`), then HIL 3.
- 2026-10-06: Delegated review received: `codereview_03/codereview.md`, status APPROVED, Execution: delegated reviewer; worktree check clean (porcelain unchanged). Limitations: T11 order needs human confirmation at HIL 3; DPI and FR-18 notes carried; refactoring escalation already decided in DEC-07. HIL 3 pending.
- 2026-10-06: HIL 3 accepted (DEC-08). Feature completed; checkpoint closed and snapshot closed. Next: commits, then `sdd-plan-refactoring` for SettingsWindow.xaml/DialogResources.xaml.
