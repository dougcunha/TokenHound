# Context snapshot — [feature slug]

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: active
- generated: [YYYY-MM-DD]
- stage: [planning | tasks | corrections | review | acceptance]
- stage_source: [tasks.md | codereview_NN/ | .audits/...destinations.md]
- covers_through: [last completed unit, e.g. T03 | techspec.md | codereview_02]
- authored_code: [yes | no]
- git_head: [short hash]
- worktree: [clean | N changed: top-level paths]
- next_step: [skill — unit, e.g. sdd-orchestrate-tasks — T04 (task_04.md)]
- other_eligible: [IDs | —]
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

- Why next: [dependencies satisfied, gate decided, recommendation]
- Read first: [unit file, then only the PRD/TechSpec sections it cites]
- Known change points: [`path:symbol` from exploration, verified at git_head | none yet]
- Applicable entries: [IDs]
- Watch out: [one or two lines, or —]

## Decisions

- [D-01] (when: on-select: T04; DEC-18) [decision and its consequence] — src: [task_03.md#Handoff | —]; until: [condition]

## Learnings

- [L-01] (when: on-run: dotnet test) [fact and what to do about it] — src: [path#section | —]; until: [condition]

## Code map

- [M-01] (when: on-edit: src/[area]/**) [`path:symbol` does what, relevant because] — src: —; until: [changed files overlap]

## Open threads

- [O-01] (when: now) [pending item, reservation hit, block, or question for the user] — src: [path#section]; until: [resolved]
