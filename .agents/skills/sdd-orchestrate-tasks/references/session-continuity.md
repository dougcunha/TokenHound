# Session continuity

SDD skills write artifacts and code in the session that runs them; subagents are read-only explorers. Work therefore accumulates in one context, and this protocol moves on by itself between units until the context fills, writing what the session learned before suggesting a new session. The snapshot the pause writes belongs to the `sdd-snapshot` skill: every write follows its Write branch.

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

The question always comes after the snapshot is written and reread by `sdd-snapshot`. Use the host's question tool (`AskUserQuestion` in Claude Code), or a textual question when none exists. State in one line what finished, the next step, the context usage, and the snapshot path and size; then print the resume command. Put the recommended option first and mark it `(Recommended)`:

| Option | Recommend when | Effect |
| --- | --- | --- |
| End and resume in a new session | Threshold reached, or the independence rule applies | End the turn; the resume command is already printed. With ContextBrake active, end the reply with `[REQUEST_SESSION_RESET]` |
| Continue in this session | Mandatory stop below the threshold | Proceed; above the threshold, the context pause repeats at the next boundary |

- **Independence.** When the next step is `sdd-review-code` and this session wrote or changed code that step will judge, stop at any usage and say why you recommend ending: the review needs a session that did not author the code. If the user continues anyway, record the limitation where the caller keeps decisions (`workflow.md` under `sdd-orchestrate-flow`) and in the report's limitations section.
- **Gates.** At a HIL gate, ask the gate question and the session question together. Ending the session does not approve the gate, and approving does not choose the session.
- **Resume command.** At every question of this pause, print before it the resume command defined in `sdd-snapshot`. When ending, start no work, explorer, or process after the answer. Silence starts nothing.
