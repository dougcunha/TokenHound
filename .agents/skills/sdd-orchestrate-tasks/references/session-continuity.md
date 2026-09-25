# Session continuity

SDD skills write artifacts and code in the session that runs them; subagents are read-only explorers. Work therefore accumulates in one context, and this protocol moves on by itself between units until the context fills, writing what the session learned before suggesting a new session. It has two parts: the session pause, and the context snapshot it writes.

## Session pause

Runs at every **boundary** the calling skill names (between tasks, slices, or workstreams, at HIL gates, before review, and at the end of standalone use), only after writes are persisted and no explorer or process is running.

### Measure the context

The **threshold** is 65% of the context window, where ContextBrake's `RED` zone starts. Pausing there leaves room to write the snapshot and ask before `CRITICAL` (75%), where ContextBrake blocks every tool except its plan, checkpoint, validation, and git commands, and the snapshot can no longer be written.

- **Telemetry.** When a tool result carries a ContextBrake block (`[ContextBrake vN] … usage=<p>% … zone=<ZONE> …`), or the harness reports usage, use the latest reading: it is a measurement and overrides the estimate. `zone=RED` or `zone=CRITICAL` reaches the threshold at any `usage`.
- **Estimate.** Without telemetry, add up what entered the context since the session started or last compacted: the fixed system and tool load (about 20k tokens), skills and sources read, tool outputs, diffs, and the text you wrote, at about 4 characters per token, against the model's window (200k tokens when unknown). Start from the estimate announced at the previous boundary and add only what came after; when in doubt, round up. A compaction in this session, or a low-context warning from the harness, reaches the threshold.

Inside a unit, the threshold does not interrupt the work: finish the unit without starting large new explorations. When it cannot finish before `CRITICAL`, stop at the next consistent point, record partial state and open items in the handoff, wait for explorers and processes, and take the context pause with that unit as next step.

### Destinations

At each boundary, follow exactly one destination:

| Destination | When | Effect |
| --- | --- | --- |
| Move on | Below the threshold and no mandatory stop | State in one line the unit finished, the usage (`58% measured` or `~40% estimated`), and the next unit; start it without asking |
| Context pause | Threshold reached | Write the snapshot, then ask |
| Mandatory stop | HIL gate, independence rule, block with no other eligible unit, or end of standalone use | Write the snapshot, then ask |

The question always comes after the snapshot is written and reread. Use the host's question tool (`AskUserQuestion` in Claude Code), or a textual question when none exists. State in one line what finished, the next step, the context usage, and the snapshot path and size; then print the resume command. Put the recommended option first and mark it `(Recommended)`:

| Option | Recommend when | Effect |
| --- | --- | --- |
| End and resume in a new session | Threshold reached, or the independence rule applies | End the turn; the resume command is already printed. With ContextBrake active, end the reply with `[REQUEST_SESSION_RESET]` |
| Continue in this session | Mandatory stop below the threshold | Proceed; above the threshold, the context pause repeats at the next boundary |

- **Independence.** When the next step is `sdd-review-code` and this session wrote or changed code that step will judge, stop at any usage and say why you recommend ending: the review needs a session that did not author the code. If the user continues anyway, record the limitation where the caller keeps decisions (`workflow.md` under `sdd-orchestrate-flow`) and in the report's limitations section.
- **Gates.** At a HIL gate, ask the gate question and the session question together. Ending the session does not approve the gate, and approving does not choose the session.
- **Resume command.** At every question of this pause, print before it, alone in a code block, the command that continues the work after `/clear` or in a new session: the host's invocation prefix (`/` in Claude Code, `$` in Codex), the skill that owns the next step, and the arguments of its `argument-hint` filled with current values, with no placeholders. Example: `/sdd-orchestrate-flow --prd shop-01-checkout`. Under `sdd-orchestrate-flow`, the skill is always the flow. When ending, start no work, explorer, or process after the answer. Silence starts nothing.

## Context snapshot

The snapshot carries what one session learned into the next, limited to what the next step needs. It is a routing table for context: a few inline facts that exist nowhere else, plus pointers that say when to load each durable source. It never replaces the PRD, TechSpec, manifest, task files, handoffs, reports, or code; when they disagree, they win and the snapshot entry is dropped.

### Location

- Skills scoped to one feature: `tasks/prd-[slug]/context-snapshot.md`. Every stage of that feature (planning, tasks, corrections, review) rewrites the same file.
- Skills that span several features (`sdd-orchestrate-prds`, `sdd-plan-audit`): the folder of the next feature to work on, creating the folder with only the snapshot when it does not exist yet. The previous snapshot of the same run gets `status: superseded` and `superseded_by`, so only one stays `active`.

### What belongs

| Include | Leave out |
| --- | --- |
| Decisions taken in the session that no durable file records yet, with a pointer to where they should live | Anything the PRD, TechSpec, task contract, report, or handoff already states |
| Learnings: codebase conventions, environment quirks, commands that fail and why, approaches tried and discarded | Conversation transcript, reasoning narrative, or praise |
| Explorer conclusions still useful for the next step, as `path:symbol` or `path:line` at a known Git head | File contents, code blocks, diffs, logs, or test output |
| Open threads: pending items, reservation hits, blocks, and questions awaiting the user | Secrets, credentials, or user data |

Prefer promoting a durable decision to its real home (the artifact being written, the task handoff, `Problems and solutions` in the manifest, or `workflow.md` under the flow) and keep only a one-line gist plus pointer here. Keep the file under 8 KiB; when it grows past that, prune before adding.

### Structure

The template in [../assets/context-snapshot.template.md](../assets/context-snapshot.template.md) has four parts, in reading order:

1. **Header**: `status` (`active`, `superseded`, or `closed`), `generated`, `stage` (`planning`, `tasks`, `corrections`, `review`, or `acceptance`), `stage_source` (the manifest, report folder, or map the stage works from), `covers_through` (last completed unit of that stage: task ID, artifact, or report), `authored_code` (`yes` when the writing session changed code, which the independence rule needs), `git_head`, `worktree`, `next_step` (skill and unit), and `other_eligible`.
2. **Load map**: the tiers below, so the reader knows when each entry applies before reading entries.
3. **Next step brief**: the `now` tier, written for `next_step`.
4. **Entries**: one line each, in sections `Decisions`, `Learnings`, `Code map`, and `Open threads`, with this shape:

   `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

   - `ID`: `D-NN` decision, `L-NN` learning, `M-NN` code map, `O-NN` open thread. Keep IDs stable across regenerations so other entries and handoffs can cite them.
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

An entry without a matching trigger is not loaded for this unit. The reader decides per tier, not once: new triggers fire as the work reaches new paths, commands, or failures.

### Load protocol

1. Locate the snapshot as the calling skill says. Read the header and load map. If the file is under 8 KiB, read it whole in one call; otherwise read up to the end of the next step brief and use `Grep` for entries.
2. Validate the header:
   - `covers_through` against the stage source (manifest `State`, report folder `done/`, or map). If the source shows more progress, the snapshot is behind: keep entries, but treat the next step brief as stale and reselect.
   - `git_head` against `git rev-parse HEAD`, and `worktree` against `git status --porcelain`. When files changed since the snapshot, entries whose triggers or `src` match those paths are suspect: verify them before relying on them.
   - `superseded` means follow `superseded_by`. `closed` means the stage it describes is finished; report and do not resume from it.
   - An unparseable header makes the whole file a list of hints: re-derive state from the stage source and verify any entry before use.
3. **Independent stages.** A session running `sdd-review-code` loads only the header, next step brief, `Open threads`, and `on-run` entries (environment and command facts). It skips `Decisions`, `Code map`, and other `Learnings`, which carry the author's framing, and derives what it judges from the sources. If this session itself authored the code under review, stop and apply the independence rule of the session pause.
4. Load the `now` tier. For open threads waiting on the user, ask before starting work those threads block.
5. After choosing the unit, load the `on-select` entries that match. Keep the `on-edit` and `on-run` triggers in mind and load those entries when the work reaches them.
6. Follow a `src:` pointer only when the gist leaves a question the work needs answered, reading only the cited section.

### Write protocol

Write only when the unit is recorded (artifact written, task moved, manifest or map consistent), or, at a pause inside a unit, with partial state recorded in the handoff, and no explorer or process is running.

1. Start from the previous snapshot, if any. For each entry, drop it if it expired, was superseded, contradicts current sources, or was promoted to a durable file that the next step will read anyway. Keep IDs of surviving entries.
2. Add what this session produced: decisions, learnings, explorer conclusions still valid at the current head, and open threads. Write each gist so it is actionable without the conversation: state the fact and its consequence, not the story.
3. Assign each entry the narrowest tier and triggers that still catch every case where it matters. `now` is for what the next session must know before choosing; most entries belong to `on-select`, `on-edit`, or `on-run`.
4. Rewrite the next step brief: why it is next, the skill and unit, the minimal sources to read first, known change points, and the entry IDs that apply. When several units are eligible, name the recommended one and list the others.
5. Fill the header from current values: `git_head` is `git rev-parse --short HEAD`, `worktree` is `clean` or a short count with the changed top-level paths, `authored_code` is `yes` if this session changed code since the last review, otherwise carry the previous value until a review covers it.
6. Replace the whole file, then reread it to check the header and that every `src:` path exists. Report the path and size in the pause message.

**Complete when:** the next session can pick the next unit, knows which entries apply to it and when, and has no entry that depends on this conversation to be understood.
