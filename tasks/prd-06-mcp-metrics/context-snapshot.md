# Context snapshot — 06-mcp-metrics

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-orchestrate-tasks/references/session-continuity.md`.

## Header

- status: active
- generated: 2026-09-22
- stage: tasks
- stage_source: tasks/prd-06-mcp-metrics/tasks.md
- covers_through: T05 complete (done/task_05.md)
- authored_code: yes
- git_head: 97f17c7
- worktree: uncommitted MCP feature and T05 files in src, tests, docs, README, and this folder (including `acceptance/`); unrelated tracked deletions under tasks/prd-arch-* and tasks/prd-feat-*
- next_step: sdd-review-code — independent re-review (codereview_3) of T05 and the DEC-24 amendments, `--base 97f17c79a5afeebbd8ff3a534cced7681b382476`
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

- Why next: `codereview_2` APPROVED the feature, but manual acceptance found ACC-01. T05 fixed it under DEC-23/DEC-24/DEC-25. This session wrote T05 and ran `codereview_2`, so it cannot review T05.
- Read first: `done/task_05.md` (contract and handoff), `workflow.md` DEC-22..DEC-25 and ACC-01, the amended PRD (NFR-03, out of scope) and TechSpec (DEC-24, CMP-09, TC-07, terrain baseline), then `codereview_2/codereview.md` for prior coverage.
- Known change points: not listed here on purpose; derive them from the diff against the base.
- Applicable entries: L-03, O-01, O-02, O-03.
- Watch out: files outside T05 are unchanged since `codereview_2`; after an APPROVED status, go to HIL 3.

## Decisions

- [D-01] (when: on-select: corrections; acceptance) HIL 2 authorized corrections within the approved contract; ACC-01 exceeds it and is governed by DEC-23 — src: tasks/prd-06-mcp-metrics/workflow.md#DEC-13; until: feature acceptance.

## Learnings

- [L-03] (when: on-run: dotnet test TokenHound.Infrastructure.Tests without filter) `AntigravityUsageProviderTests.GetSnapshotAsync_LiveIntegration_WhenAgyRunning_ReturnsOfficialMetrics` hits the real provider when `agy` runs locally and can fail with `Derived` fidelity; use class filters for feature evidence — src: tasks/prd-06-mcp-metrics/done/task_02.md#handoff; until: feature acceptance.
- [L-04] (when: on-run: manual script; Windows MCP) The TokenHound tray icon sits in the taskbar overflow; its first right-click often shows only the tooltip, so right-click again. The overflow button toggles, so check its state before clicking. Tooltips are not in the UI tree: capture them with a `System.Drawing` `CopyFromScreen` crop via windows-mcp PowerShell. The scratchpad SSE clients were stdlib Python — src: tasks/prd-06-mcp-metrics/acceptance/; until: feature acceptance.

## Code map

- (none carried; T05 change points are in `done/task_05.md#Handoff`)

## Open threads

- [O-01] (when: now; acceptance) Manual script executed 2026-09-22: all steps passed except ACC-01. T05 re-ran step 2 for Antigravity and it now matches (`acceptance/t05_gemini.txt`) — src: tasks/prd-06-mcp-metrics/workflow.md#manual-acceptance-evidence-dec-22; until: HIL 3.
- [O-02] (when: now: resume; on-edit: tasks/**) Unrelated tracked deletions under `tasks/prd-arch-20260912-*` and `tasks/prd-feat-20260916-01-cline-provider/` are not this feature's writes; preserve and exclude them — src: tasks/prd-06-mcp-metrics/workflow.md#worktree-observation-at-pause; until: the user confirms ownership.
- [O-03] (when: now) The user's installed TokenHound (`D:\Apps\TokenHound`) was closed for the manual script and not reopened — src: workflow.md#manual-acceptance-evidence-dec-22; until: the user reopens it.
