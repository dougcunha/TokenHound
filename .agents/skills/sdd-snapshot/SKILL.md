---
name: sdd-snapshot
description: SDD snapshot for writing, updating, or loading a session's distilled context in tasks/prd-[slug]/context-snapshot.md. Use when saving what the session learned before pausing or clearing the context, including when ContextBrake asks to record progress, or when resuming a feature that has a snapshot. Don't use for the flow checkpoint, task handoffs, or SDD artifacts.
argument-hint: [--prd feature-name]
---

# SDD snapshot

The snapshot carries what one session learned into the next, limited to what the next step needs. It is a routing table for context: a few inline facts that exist nowhere else, plus pointers that say when to load each durable source. It is a hint, never an authority: the PRD, TechSpec, manifest, task files, handoffs, reports, checkpoint, and code win on conflict, and the diverging entry is dropped.

| Branch | When | Follow |
| --- | --- | --- |
| Write | Called alone, by another SDD skill's session pause, or when ContextBrake asks to record progress | The [Write](#write) steps |
| Load | An SDD skill starts or resumes work on a feature with `context-snapshot.md` | Read [references/load.md](references/load.md) in full and apply its protocol |

## Location

- Skills scoped to one feature: `tasks/prd-[slug]/context-snapshot.md`. Every stage of that feature (planning, tasks, corrections, review) rewrites the same file.
- Skills that span several features (`sdd-orchestrate-prds`, `sdd-plan-audit`): the folder of the next feature to work on, creating the folder with only the snapshot when it does not exist yet. The previous snapshot of the same run gets `status: superseded` and `superseded_by`, so only one stays `active`.

## Structure

The template [assets/context-snapshot.template.md](assets/context-snapshot.template.md) has four parts, in reading order:

1. **Header**: `status` (`active`, `superseded`, or `closed`), `generated`, `stage` (`planning`, `tasks`, `corrections`, `review`, or `acceptance`), `stage_source` (the manifest, report folder, or map the stage works from), `covers_through` (last completed unit of that stage: task ID, artifact, or report), `authored_code` (`yes` when the writing session changed code, which the review independence rule needs), `git_head`, `worktree`, `next_step` (skill and unit), and `other_eligible`.
2. **Load map**: the tiers below, so the reader knows when each entry applies before reading entries.
3. **Next step brief**: the `now` tier, written for `next_step`.
4. **Entries**: one line each, in sections `Decisions`, `Learnings`, `Code map`, and `Open threads`, with this shape:

   `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

   - `ID`: `D-NN` decision, `L-NN` learning, `M-NN` code map, `O-NN` open thread. IDs stay stable across regenerations so other entries and handoffs can cite them.
   - `when`: the load tier plus its triggers. Triggers are task or finding IDs (`T04`, `CR-03`), path globs (`src/TokenHound.Infrastructure/**`), traceability IDs (`DEC-04`, `FR-17`), stages (`review`), commands (`dotnet test`), or events (`on failure: MSB3027`).
   - `src`: where the full detail lives, loaded only when the gist is not enough. `—` when the gist is the only record.
   - `until`: when the entry expires (`T06 done`, `DEC-18 replaced`, `next session`). Expired entries are dropped on the next write.

One line per entry keeps selective loading cheap: `Grep` on `when:.*T04` or on a path fragment finds the entries for a unit without reading the rest.

### Load tiers

| Tier | Load when | Examples |
| --- | --- | --- |
| `now` | At session start, before choosing work | Header, next step brief, open threads waiting on the user |
| `on-select` | After choosing the unit, if a trigger matches its ID, affected files, or traceability IDs | A decision that constrains the ledger API for T04 |
| `on-edit` | Just before editing or creating a path that matches | A WAL read-only quirk for `src/TokenHound.Infrastructure/**` |
| `on-run` | Just before running a matching command, or right after it fails | A new test project must join `TokenHound.slnx` before `dotnet test` runs it |
| `on-demand` | Only when a gist is insufficient; this applies to every `src:` pointer | The previous review's finding text |

An entry without a matching trigger is not loaded for the unit. The reader decides per tier, not once: new triggers fire as the work reaches new paths, commands, or failures.

## Write

1. **Resolve the file.** Use the feature from `--prd` or the one the calling skill names. Called alone without an argument, use the feature this session worked on (a `tasks/prd-*` folder read or written in the conversation); without one, the only `context-snapshot.md` with `status: active`. With several candidates or none, ask which through the host's question tool (`AskUserQuestion` in Claude Code), offering the existing `tasks/prd-*` folders; a new slug creates the folder with only the snapshot. An existing file is updated; a missing one is created from the template, read in full.
   **Output:** one resolved path, and whether the write updates or creates it.
2. **Stabilize.** Wait for explorers and processes to finish. Write with the unit recorded (artifact written, task moved, manifest or map consistent); with a unit in progress, the common case when ContextBrake asks, first record its partial state and open items in its handoff.
   **Output:** no explorer or process running, and all unit state in a durable file.
3. **Distill.** Start from the previous snapshot, if any. Drop each entry that expired, was superseded, contradicts current sources, or was promoted to a durable file the next step will read anyway; keep the IDs of surviving entries. Add what this session produced, writing each gist so it is actionable without the conversation: the fact and its consequence, not the story.

   | Include | Leave out |
   | --- | --- |
   | Decisions taken in the session that no durable file records yet, with a pointer to where they should live | Anything the PRD, TechSpec, task contract, report, or handoff already states |
   | Learnings: codebase conventions, environment quirks, commands that fail and why, approaches tried and discarded | Conversation transcript, reasoning narrative, or praise |
   | Explorer conclusions still useful for the next step, as `path:symbol` or `path:line` at the current Git head | File contents, code blocks, diffs, logs, or test output |
   | Open threads: pending items, reservation hits, blocks, and questions awaiting the user | Secrets, credentials, or user data |

   Prefer promoting a durable decision to its real home (the artifact being written, the task handoff, `Problems and solutions` in the manifest, or `workflow.md` under the flow) and keep only a one-line gist plus pointer here. Give each entry the narrowest tier and triggers that still catch every case where it matters: `now` is for what the next session must know before choosing; most entries belong to `on-select`, `on-edit`, or `on-run`. Keep the file under 8 KiB; when it grows past that, prune before adding.
   **Output:** every entry has an ID, tier, triggers, source, and expiry, and none depends on this conversation to be understood.
4. **Rewrite the brief and header.** The next step brief says why it is next, the skill and unit, the minimal sources to read first, known change points, and the entry IDs that apply; when several units are eligible, name the recommended one and list the others. In the header, `git_head` is `git rev-parse --short HEAD`, `worktree` is `clean` or a short count with the changed top-level paths, and `authored_code` is `yes` if this session changed code since the last review, otherwise it carries the previous value until a review covers it.
   **Output:** header with current values and a brief written for `next_step`.
5. **Write and verify.** Replace the whole file, then reread it to check the header and that every `src:` path exists. Finish by reporting path and size and printing the resume command; under the session pause, they go into its message, once. When the write answers a ContextBrake request, end the reply with `[REQUEST_SESSION_RESET]`.
   **Output:** the next session can pick the next unit, knows which entries apply to it and when, and has the command to start.

### Resume command

Alone in a code block, ready to paste after `/clear` or in a new session: the host's invocation prefix (`/` in Claude Code, `$` in Codex), the skill, and the arguments of its `argument-hint` filled with current values, with no placeholders. The skill is `sdd-orchestrate-flow` when the feature folder has a `checkpoint.json` that is not completed; otherwise, the skill in `next_step`. Example: `/sdd-orchestrate-flow --prd shop-01-checkout`.

## Failures

- In the `CRITICAL` zone, ContextBrake blocks writing the snapshot: record progress in its plan and checkpoint, which stay allowed, and report that the snapshot was not written.
- A failed write leaves the previous version intact; report the error instead of announcing a ready snapshot.
