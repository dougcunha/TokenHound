# Context snapshot: prd-16-hud-geometry-hit-testing

> Hints only; checkpoint, workflow, contracts and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-10-09
- stage: tasks
- stage_source: tasks.md
- covers_through: completed; codereview_03 APPROVED WITH RESERVATIONS; HIL 3 accepted
- authored_code: yes
- git_head: 2bc32ef
- worktree: PRD 16 docs/evidence (ARCHITECTURE, ROADMAP, task artifacts, new t03 JSON/logs), PRD 17 artifacts, unrelated local tooling
- next_step: none (feature completed 2026-10-09, DEC-07)
- other_eligible: none
- superseded_by: none

## Load map

Read now entries before choosing work, on-select for T03, on-run before commands, and on-edit before source changes. Contracts and current evidence remain authoritative.

## Next step brief

T03 coordinator acceptance is recorded in validation.md "T03 desktop acceptance, 2026-10-09": right-boundary fix confirmed (x=1920), six-mode outside-click/focus matrix passed, sizes 50/125/150%, DPI transitions over three displays, drag-to-Free, restart persistence, and idle comparison with a v0.1.13 baseline (repository lower). Visual gate approved; T03 in done/. codereview_01 was REJECTED for evidence and records only (no code defect); correction round 1 (codereview_01/task_01, task_02) is done. Next: delegate the re-review to a new reviewer in codereview_02, then HIL 3, where the human confirms CR-01 (physical disconnection accepted without execution).

## Decisions

- [D-01] (when: now) Workflow DEC-03 permits implementation/validation/corrections within existing contracts; no commit/push/release without an explicit request; src: workflow.md#dec-03-hil-2-approve-the-technical-plan-and-implementation; until: material scope change.
- [D-02] (when: on-select: T03) Preserve the canonical contour and passive WS_EX_TRANSPARENT shadow companion; src: techspec.md#technical-decisions; until: technical decision changes.

## Learnings

- [L-03] (when: on-run: desktop screenshot) Single-monitor dxcam captures can be stale; regions crossing a display boundary use the live pillow backend; src: validation.md#validated-baseline; until: MCP changes.
- [L-09] (when: on-run: click target) WinForms child labels swallow MouseDown; keep target labels away from test points; src: validation.md#t03-desktop-acceptance-2026-10-09; until: next session.
- [L-10] (when: on-run: tray) The notification-overflow tray icon did not open its menu through automation; use HUD menus or process stop for lifecycle checks; src: validation.md#t03-desktop-acceptance-2026-10-09; until: harness changes.
- [L-11] (when: on-run: bash heredoc) Heredocs containing apostrophes fail in this shell wrapper; write scripts with the Write tool; src: —; until: next session.

## Code map

- [M-02] (when: on-edit: NotchWindow*) ApplyChrome sets mode/padding/popups; drag records Free only when Left/Top change; src: done/task_02.md#handoff; until: integration changes.
- [M-04] (when: on-edit: HudContourController*; HudShadowWindow*) Controller converts owner/shadow DPI and suppresses unchanged frames; Win32 failures log and hide the shadow; src: done/task_02.md#handoff; until: integration changes.

## Open threads

- [O-06] (when: now) CR-01: physical disconnection accepted without execution; confirm at HIL 3; src: validation.md#t03-desktop-acceptance-2026-10-09; until: decided.
- [O-07] (when: on-demand) Idle CPU of about 8-13 s per minute exists in v0.1.13 as well; candidate for a separate investigation, not this feature; src: validation.md#t03-desktop-acceptance-2026-10-09; until: triaged.
