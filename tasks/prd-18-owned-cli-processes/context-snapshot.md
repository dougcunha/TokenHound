# Context snapshot: prd-18-owned-cli-processes

Hints only; code, contracts, reports, handoffs, and checkpoint win on conflict.

## Header

- status: active
- generated: 2026-10-15
- stage: corrections
- stage_source: codereview_01/
- covers_through: codereview_01/codereview.md received; automated implementation validated
- authored_code: yes
- git_head: 577a55a
- worktree: uncommitted changes in solution, docs, src, tests, and this feature's tasks
- next_step: sdd-execute-corrections - codereview_01/task_01.md (manual evidence)
- other_eligible: none
- superseded_by: none

## Load map

`now`: header, brief, open threads. `on-select`: matching task/requirement. `on-run`: matching command. `on-edit`: matching path. Follow sources only when the gist is insufficient. A reviewer loads only header, brief, open threads and on-run entries.

## Next step brief

Implementation is present and all 1145 Infrastructure tests and the App build pass. Formal review found no current code defect but is REJECTED because TC-05 real desktop evidence is missing. Read `codereview_01/task_01.md` and validation.md's pending script. Do not restart implementation, release, commit, or mark acceptance complete. Apply O-01 and L-01.

## Decisions

- [D-01] (when: on-select: FR-03; review) Windows JOB_LIST plus restricted HANDLE_LIST assigns ownership at process creation, avoiding an unprotected launch window; src: techspec.md#decisions-and-flow; until: DEC-03 replaced.

## Learnings

- [L-01] (when: on-run: dotnet test) Native MTP uses --project, --no-build --no-restore and -- --minimum-expected-tests 1; preserve LASTEXITCODE; src: validation.md#commands-and-observed-results; until: runner changes.

## Code map

- [M-01] (when: on-edit: src/TokenHound.Infrastructure/Processes/**) OwnedProcess provides redirected streams, stderr draining, bounded shutdown and Windows job cleanup; src: techspec.md#decisions-and-flow; until: process ownership changes.

## Open threads

- [O-01] (when: now) TC-05 one-hour desktop/live quota/event observation remains pending; Windows MCP process listing returned USER_CONTROL and no desktop run was started; src: codereview_01/task_01.md#verification-and-environment; until: evidence recorded and independently reviewed.
