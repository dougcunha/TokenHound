# Context snapshot — 07-claude-multi-profile

> Routing hints for the next session. The PRD, TechSpec, tasks, handoffs, review, workflow, and code are authoritative.

## Header

- status: closed
- generated: 2026-09-23
- stage: acceptance
- stage_source: codereview_02/
- covers_through: codereview_02
- authored_code: no
- git_head: 3dc0c4e
- worktree: modified src/ and tests/ files; untracked feature folder and new source/test files
- next_step: none — feature completed and accepted
- other_eligible: none
- superseded_by: —

## Load map

| Tier | Load when |
| --- | --- |
| `now` | Session start: header, next step brief, and open threads |
| `on-select` | The chosen task, finding, or stage matches an entry |
| `on-edit` | About to edit a matching path |
| `on-run` | About to run a matching command, or that command failed |
| `on-demand` | A gist needs detail from its `src:` pointer |

## Next step brief

- Why next: Independent re-review `codereview_02` approved the multi-profile implementation (all findings CR-01 to CR-05 resolved; 861 tests passing). The next step is Step 6 (HIL 3 acceptance).
- Read first: `tasks/prd-07-claude-multi-profile/codereview_02/codereview.md`, `tasks/prd-07-claude-multi-profile/prd.md`, and `tasks/prd-07-claude-multi-profile/workflow.md#REC-07`.
- Known change points: Present paths, validations, reservations, and manual acceptance script to the user at HIL 3.
- Applicable entries: L-01, O-02.
- Watch out: HUD manual acceptance remains for HIL 3.

## Decisions

- [D-01] (when: on-select: acceptance; DEC-11) Multi-profile architecture and authorized scope are recorded in the approved TechSpec — src: tasks/prd-07-claude-multi-profile/techspec.md#technical-decisions; until: completion
- [D-02] (when: on-select: acceptance; DEC-02) FR-08 popup identification is part of the approved PRD; the HUD binding needs manual acceptance — src: tasks/prd-07-claude-multi-profile/prd.md#FR-08; until: completion
- [D-03] (when: on-select: acceptance; DEC-19) NFR-03 performance contract requires warm p95 < 25 ms with cold-call overhead (~25–70 ms) documented — src: tasks/prd-07-claude-multi-profile/workflow.md#DEC-19; until: completion

## Learnings

- [L-01] (when: on-run: dotnet test) The MTP test runner requires `--project` and arguments after `--`, with `--minimum-expected-tests 1` — src: AGENTS.md#Running & reading MTP tests; until: project lifetime
- [L-02] (when: on-select: acceptance) `ClaudeProfileDiscovery` benchmark demonstrates warm median ~1.2 ms and p95 ~2.4 ms on 10-profile Release fixture — src: tasks/prd-07-claude-multi-profile/codereview_02/codereview.md#executed-validations; until: completion

## Code map

- [M-01] (when: on-edit: src/TokenHound.Infrastructure/Providers/Claude/**) `ClaudeProfileDiscovery.DiscoverProfiles` scans default and isolated directories and qualifies credentials via read-only stream — src: tasks/prd-07-claude-multi-profile/codereview_02/codereview.md#coverage-matrix; until: completion

## Open threads

- [O-02] (when: now) HUD manual acceptance remains for HIL 3 — src: tasks/prd-07-claude-multi-profile/techspec.md#manual-acceptance-script; until: completion
