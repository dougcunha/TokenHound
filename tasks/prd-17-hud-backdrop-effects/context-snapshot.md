# Context snapshot — prd-17-hud-backdrop-effects

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-10-09
- stage: acceptance
- stage_source: codereview_4/
- covers_through: codereview_4 (APPROVED WITH RESERVATIONS; no blocks)
- authored_code: yes
- git_head: 2bc32ef
- worktree: uncommitted feature code in src/ and tests/ (PRD 17 backdrop files untracked); docs (ARCHITECTURE.md, README.md, docs/ROADMAP.md); tasks/ (PRD 16 records, PRD 17 folder untracked, triage-log); unrelated local tooling (.agents/, .codex/, .context-brake/, context-brake.config.json)
- next_step: — (feature accepted at DEC-11; optional follow-ups: simplify SetWindowPos interop, ADR for DEC-05, item A outside-click checks)
- other_eligible: —
- superseded_by: —

## Load map

| Tier | Load when |
| --- | --- |
| `now` | Session start: header, next step brief, open threads waiting on the user |
| `on-select` | Chosen unit matches a trigger: task or finding ID, affected file, or traceability ID |
| `on-edit` | About to edit or create a path matching a trigger |
| `on-run` | About to run a matching command, or it just failed |
| `on-demand` | A gist is not enough: follow its `src:` pointer, that section only |

Review sessions load only the header, next step brief, `Open threads`, and `on-run` entries.

Entry shape: `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

## Next step brief

- Why next: codereview_4 is APPROVED WITH RESERVATIONS; the only new item is the TechSpec Observability wording vs DEC-10. DEC-09 items A and C plus the limitations are accepted open items for HIL 3.
- Skill and unit: HIL 3 under `sdd-orchestrate-flow` step 6; latest review `codereview_4/`, base `2bc32ef`, delegated per `.agents/skills/sdd-review-code/references/delegated-review.md`; the coordinator authored the code, so it must not review it.
- Read first: checkpoint.json, tasks.md, done/task_02.md Handoff, validation.md T02 matrix.
- Known change points: worktree also holds unrelated local tooling changes (.agents/, .codex/, .context-brake/); the reviewer must scope to PRD 17 files listed in done/task_02.md Handoff.
- Applicable entries: L-03, O-04, O-06.

## Decisions

- [D-02] (when: on-select: DEC-02; DEC-05; CMP-03; review) Companion recipe as built: raw `CreateWindowEx` HWND with `WS_EX_NOREDIRECTIONBITMAP|TRANSPARENT|NOACTIVATE|TOOLWINDOW|TOPMOST`, `DWMWA_USE_HOSTBACKDROPBRUSH`=1, hand-written WinRT ABI interop, window region from the flattened contour — src: techspec.md; until: review done.

## Learnings

- [L-03] (when: on-run: PowerShell mouse automation; SetCursorPos; WindowFromPoint) PowerShell is DPI-unaware: on the 150% primary its mouse coordinates scale ×1.5 and land in other apps; call `SetThreadDpiAwarenessContext(-4)` first and guard with `WindowFromPoint`; prefer Windows MCP Click/Move — src: tasks.md#problems-and-solutions; until: feature completed.
- [L-05] (when: on-run: desktop checks) Transparency effects are OFF on this machine; toggling needs registry + `WM_SETTINGCHANGE` "ImmersiveColorSet" and a fresh human authorization (DEC-06 covered T02 only) — src: workflow.md#dec-06; until: feature completed.

## Code map

- [M-01] (when: on-edit: src/TokenHound.Infrastructure/Configuration/**) New settings sections must be carried by hand in `UserSettingsFile.cs` MergeWithDefaults — src: —; until: feature completed.

## Open threads

- [O-07] (when: now; HIL 3) codereview_1 limitations stay open for HIL 3: FR-03 outside clicks only proven in TopCenter (needs desktop authorization), FR-06 energy saver ≈ 2 s, TC-08 60 s runs, no clean-exit evidence; optional items include the ROADMAP PRD 17 row and the settings card high-contrast text — src: codereview_4/codereview.md#limitations-and-open-items; until: HIL 3.

- [O-04] (when: now; HIL 3) Product note: with FR-05, this user's machine (transparency off) shows the solid fallback by default; raise at HIL 3 — src: workflow.md#dec-04; until: HIL 3.
- [O-06] (when: review; on-edit: src/TokenHound.App/UI/Controls/HudContourDecorator.cs) `Background` AddOwner fix added AffectsRender; `BorderBrush` has the same latent defect, not changed at runtime and left as is — src: done/task_02.md#handoff; until: review done.
