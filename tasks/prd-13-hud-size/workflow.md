# Workflow Log: PRD 13 — HUD size setting

## Feature Context
- **Slug**: `prd-13-hud-size`
- **Objective**: Add a Settings option to scale the HUD (percentage slider with presets) because it is too large on some resolutions.
- **Process Level**: `sdd-lean` (decided at HIL 0).
- **Git Base Commit**: `2fe5515c3793e83f6c97c2328d77dba997b640f8`. The worktree was clean before this flow, apart from the `tasks/triage-log.jsonl` line that triage appended.

## Decisions

### DEC-01: Process Level and Stops Adjustment (sdd-lean)
- **Date**: 2026-10-01
- **Decision**: The human chose `sdd-lean` at HIL 0. The rubric recommended `sdd-full` (S1 persisted setting, S5 open decisions, S4 two modules). Record: `tasks/triage-log.jsonl`, 2026-10-01.
- **Stops Modification**: HIL 1 and HIL 2 merge into one decision after the PRD, TechSpec, and task plan are drafted. The independent code review is kept.
- **Human text**: "sdd-lean".

### DEC-02: Merged HIL 1+2 approved
- **Date**: 2026-10-01
- **Decision**: The human approved `prd.md`, `techspec.md`, and `tasks.md` (T01, T02) as presented. This includes the product decision of a 50–150% slider plus Small/Default/Large presets, and authorizes implementation and corrections within that contract.
- **Human text**: "Aprovar" (HIL 1+2) and "Continuar aqui (Recommended)" (session).
- **Scope**: sha256 prd.md=1c9c288c330c, techspec.md=57fcbbdad298, tasks.md=3ce167c0dd09.

### DEC-03: Reservations decided
- **Date**: 2026-10-01
- **Decision**: At the reservations HIL the human chose to correct CR-01 and CR-02 directly, without another review round.
- **Human text**: "Corrigir CR-01 e CR-02".
- **Scope**: CR-01: `HudSizeSettingsViewModel.Apply` logs `Applied HUD size {Percent}` at debug level. CR-02: `App` keeps one `_hudSizeStore` field used by the startup load and by the Settings ViewModel (the two uses happen at different moments, startup and dialog open, so the stored value is still re-read when Settings opens). Limitation: these two small changes were not independently reviewed, by the human's decision. Build 0 warnings and Infrastructure.Tests 980 passed after the change.

### DEC-04: Feature accepted (HIL 3)
- **Date**: 2026-10-01
- **Decision**: The human accepted the delivery. Accepted open items: none beyond the DEC-03 limitation (CR-01 and CR-02 corrections were not independently reviewed). No commit, push, or PR was requested.
- **Human text**: "Aceitar".

## Events

- 2026-10-01: `prd.md` written (OBJ-01..02, US-01..03, FR-01..07, NFR-01..04, sha256 1c9c288c330c). Open product question for the merged HIL: slider plus presets (proposed) versus presets only.
- 2026-10-01: Product question answered: slider (50–150%, step 10) plus Small/Default/Large presets, as proposed in FR-01/FR-02. Human text: "Slider + presets (Recommended)". Session: "Continuar aqui (Recommended)". Next: techspec.md and tasks.md, then the merged HIL 1+2.
- 2026-10-01: `techspec.md` written (DEC-01..DEC-11, CMP-01..CMP-10, TC-01..TC-09, MA-1..MA-4). Preparatory refactoring not recommended: `SettingsWindow.xaml` is above 500 lines (821), but the feature adds one section and moves one trigger. Findings that shaped the design: tooltip and popup live in separate visual trees, so they bind to a shared static `HudScale`; `HudPositionStore.Save` would overwrite a size stored in the `Hud` section, so size has its own section; the General tab collapses when `Startup` is null, so the tab visibility moves to the Startup section; the fixed `MinWidth`/`MinHeight` need to scale.
- 2026-10-01: `tasks.md` + `task_01.md`, `task_02.md` written (DAG T01 → T02). The merged HIL 1+2 is next.
- 2026-10-01: Merged HIL 1+2 approved (DEC-02). Continuing in this session with T01.
- 2026-10-01: T01 done: `HudSizeSettings`/`HudSizeStore`/`HudScale`/`HudSizeSettingsViewModel` plus TC-01..TC-07; build 0 warnings, Infrastructure.Tests 977 and Core.Tests 166 passed. Two deviations found while testing, both inside the approved intent: (1) the percentage step is 5, because the approved presets 75 and 125 are not multiples of the approved step of 10 (PRD FR-01, TechSpec DEC-02/TC-01 and the task wording were amended); (2) `UserSettingsFile.MergeWithDefaults` copied sections by hand and dropped `HudSize` on load, so one line was added there. The approved-source hashes were refreshed after the step wording change and the T01 state and link update. Next: T02.
- 2026-10-01: T02 done: HUD, popup, and tooltip card scale through `LayoutTransform` bound to `HudScale.Current`; scaled window minimums in the new partial `NotchWindow.Scale.cs` (keeps `NotchWindow.xaml.cs` at 295 lines); General tab has the HUD size section and stays visible when only that model exists; stored size loaded before the HUD is created. App build 0 warnings, Infrastructure.Tests 980 and Core.Tests 166 passed. The approved-source hash of `tasks.md` was refreshed after the T02 state and link update. Next: the human visual check (MA-1..MA-4), then the delegated review.
- 2026-10-01: Visual check approved by the human: MA-1..MA-4 all passed. Human text: "Tudo OK". Session: "Continuar aqui (Recommended)". The running app (PID 10032) was stopped so the reviewer can build. Next: delegated review codereview_1 with `--base 2fe5515c3793e83f6c97c2328d77dba997b640f8`.
- 2026-10-01: Delegated review codereview_1 received: APPROVED WITH RESERVATIONS, Execution: delegated reviewer. The worktree matched the state recorded before delegation. Findings: CR-01 (Low, no `Log.Debug` of the applied percentage, as the TechSpec observability section planned) and CR-02 (Low, `App.xaml.cs` creates two `HudSizeStore` instances at startup). No blocks. Next: reservations HIL.
- 2026-10-01: CR-01 and CR-02 corrected directly (DEC-03). Next: HIL 3.
- 2026-10-01: HIL 3 accepted (DEC-04). Feature completed.
