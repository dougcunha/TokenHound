# Context snapshot — prd-14-hud-multimonitor-docking

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-10-05
- stage: completed
- stage_source: codereview_03/codereview.md
- covers_through: T11
- authored_code: yes
- git_head: c4b55b9
- worktree: uncommitted feature diff in src/, tests/, tasks/; pre-existing .agents/, .context-brake/, .opencode/, context-brake.config.json
- next_step: — (feature closed; refactoring per DEC-07 opens its own feature)
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

- Why next: T11 (DEC-07) done and reviewed; codereview_03 APPROVED.
- Read first: `workflow.md#DEC-07` and `#Events` (last entries), `codereview_03/codereview.md` (Summary, Limitations).
- Known change points: see each handoff's "Changed files".
- Applicable entries: L-05, O-04
- Watch out: the installed TokenHound (D:\Apps\TokenHound) is running again (PID 28760); stop it only if a correction needs the dev HUD; user settings still modified by tests.

## Decisions

- [D-01] (when: on-select: review; FR-17; FR-18) FR-17 and FR-18 were approved at HIL 1 (DEC-03) — src: workflow.md#DEC-03; until: feature closed

## Learnings

- [L-01] (when: on-edit: tasks/**/checkpoint.json) Git Bash heredocs keep `\` literally; write the workspace path with forward slashes or the JSON fails to parse — src: —; until: feature closed
- [L-02] (when: on-run: python) Long inline `python - <<'PYEOF'` heredocs with nested quotes fail in the Bash tool; write scripts to the scratchpad first — src: —; until: feature closed
- [L-03] (when: on-run: launch; review) The App process is SystemAware (no DPI manifest): monitor rectangles of non-150% displays are virtualized by 1.5; docking stays consistent because window and monitor rectangles share that space — src: done/task_04.md#Handoff; until: feature closed
- [L-04] (when: on-run: PowerShell; screenshot) The PowerShell tool runs on an isolated desktop: it cannot find HUD windows (FindWindow returns 0) but can stop processes and edit settings; use Windows MCP App/Screenshot/Click for UI — src: —; until: feature closed
- [L-05] (when: on-run: dotnet build) Stop the dev HUD (`Get-Process TokenHound.App | Stop-Process`) before building; dev build logs go to `src/TokenHound.App/bin/Debug/net10.0-windows/logs/TokenHound/` — src: —; until: feature closed

## Code map

- [M-01] (when: on-edit: src/TokenHound.App/UI/Windows/NotchWindow*) Placement engine `NotchWindow.Placement.cs`, chrome `NotchWindow.Dock.cs`, submenu `NotchWindow.Menu.cs`; docking size comes from ActualWidth × DPI because GetWindowRect is stale during SizeChanged — src: done/task_05.md#Handoff; until: files change

## Open threads

- [O-04] (when: now) After PRD-14 closes, plan the refactoring of SettingsWindow.xaml (1051 lines) and DialogResources.xaml (561 lines) with sdd-plan-refactoring as its own feature — src: workflow.md#DEC-07; until: refactoring feature opened
