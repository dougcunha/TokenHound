# Autonomous mode

Applies when the feature's checkpoint has `mode: auto`. Without the key, or with `monitored`, this file does not apply and the flow runs as usual.

In auto mode, the user delegates the flow's decisions so the feature runs from request to acceptance without anyone following the session. This session answers the gates, backed by the specs; ContextBrake takes care of the context: it writes the snapshot, clears the session, and resumes the flow in a new one. The quality of the PRD and TechSpec therefore decides the quality of the choices: every decision needs a basis in a requirement, a technical decision, `AGENTS.md`, or the code, and the log shows which.

## Enable and switch

- `--mode auto` on `sdd-orchestrate-flow` or on `sdd-triage`, which passes it on to the flow. The flow writes `mode` to the checkpoint when creating it and records the activation in `workflow.md` as a human decision: that decision authorizes the autonomous decisions within this protocol.
- `decision_log` defaults to `autonomous-decisions.md` in auto mode. `--log-decisions <path>` changes the file (relative to the feature folder); `--log-decisions false` writes `null` and turns the log off, a decision that also goes to `workflow.md`.
- A resume without `--mode` keeps the checkpoint's mode. A `--mode` different from the saved one switches the mode and is recorded in `workflow.md`. In `monitored`, earlier autonomous decisions remain valid.
- When the request is sliced into several PRDs or workstreams, every checkpoint opened inherits `mode` and `decision_log`.
- The level `sdd-triage` chooses in auto mode, before the feature folder exists, becomes the log's first entry when the flow creates the folder.

## Check ContextBrake

Once per session, at the start, read `context-brake.config.json` at the repository root, and nothing else from ContextBrake. Warn in one line per item, without blocking, and record in the log only the warnings it does not have yet, because every restart is a new session:

| Condition | Consequence to report |
| --- | --- |
| Missing file | No automatic restart: the session relies on harness compaction; the checkpoint saved at every boundary keeps resumption possible |
| Current harness not in `activeHarnesses` | No telemetry in this session |
| No `autoRestart` block | The snapshot is written, but the session is neither cleared nor resumed by itself |
| `autoRestart.maxConsecutiveRestarts` below 5 | A long run stops after a few restarts without a typed prompt |
| `snapshot.command` does not name `sdd-snapshot` | The snapshot request does not follow the Write branch |
| `snapshot.resumeCommand` does not name `sdd-orchestrate-flow` | The new session does not resume this flow |

When there is a warning, suggest the command that fixes everything at once: `npx context-brake init --auto-restart --max-restarts 10 --snapshot-command "/sdd-snapshot" --resume-command "/sdd-orchestrate-flow"`.

## Decide without asking

At every point where monitored mode would ask the user (HIL 0 to 3, exception, reservations, visual check, questions from the stage skills this flow runs, and session pause choices):

1. Build the question as it would be asked: context, options, and the recommended one.
2. Check the escalation rubric below. If any item holds, escalate.
3. Without escalation, choose the recommended option. Without a clear recommendation, prefer, in this order, the one that preserves the approved contract, the most reversible one, and the one with the smallest scope. A product gap becomes an explicit assumption in the artifact itself, with the log ID.
4. Record the decision in the log before acting, and in `workflow.md` with provenance `autonomous` and the log ID; `approved_sources` points to that record. Proceed without waiting.

A problem during execution (failing test, unstable environment, implementation ambiguity) follows the retry and block rules of the stage skills. A blocked task stays recorded and execution continues with the other eligible ones; escalation happens only when no independent unit remains.

A deviation the TechSpec does not decide, such as an implementation detail it leaves open, is resolved by the alternative consistent with the `DEC-NN` entries and the code's patterns, and recorded in the task handoff and in the log. A deviation that contradicts a `DEC-NN` or the PRD scope escalates.

### Escalation rubric

Ask the user only when the decision:

- is irreversible or destructive: a migration that discards data, removal of user data, a persisted format change with no way back;
- triggers an external action the request did not authorize: push, deploy, release publication, sending messages, a call to a paid or production service;
- touches a critical area declared in `AGENTS.md` (e.g. credentials, provider authentication, payments) and the PRD and TechSpec do not decide it;
- changes the approved PRD scope, adding or removing an obligation;
- depends on a contradiction between PRD, TechSpec, and code with no basis in the specs to choose;
- depends on an unavailable environment, credential, or essential manual validation, with no other eligible unit;
- hits the flow's stagnation rule (two correction rounds without progress);
- has no option the sources can sustain.

When escalating: first finish the independent units, save the checkpoint with `status: awaiting-hil` and `pending_hil`, write the snapshot, record the escalation in the log, and ask as in monitored mode. Do not end the reply with `[REQUEST_SESSION_RESET]`: the restart would land on the same question with no one to answer it.

### Gates

| Gate | Autonomous decision |
| --- | --- |
| HIL 0 | The `sdd-triage` rubric level; `spot` follows its spot branch |
| PRD slicing or workstreams | The split the owning skill proposes |
| HIL 1 | Approves the PRD after the coverage check; a blocking pending item becomes a recorded assumption or an escalation |
| Preparatory refactoring | Follows the TechSpec recommendation |
| HIL 2 | Approves TechSpec and plan after the skill's checks; authorizes implementation and corrections within the contract |
| Exception | Escalation rubric |
| Reservations | Corrects the reservations that fit the approved contract without widening scope; the others become accepted open items in the log |
| Visual check | Records the manual acceptance script as pending manual acceptance and proceeds to the review |
| HIL 3 | Automatic acceptance, below |

**Automatic acceptance.** With the latest review `APPROVED`, or `APPROVED WITH RESERVATIONS` with the reservations routed, no blocking obligation open, and no essential manual validation pending, mark the checkpoint `completed` and the snapshot `closed`, and record `automatic acceptance` in the log with the review path. A pending visual check or other essential manual validation prevents completion: escalate, with the manual acceptance script and the log summary at HIL 3. ADR candidates and accepted open items go into the final summary, because the later removal of the artifacts would take them along.

## Context and session

In auto mode, the flow neither estimates context usage nor asks about continuity. At every session pause boundary:

| Situation | Destination |
| --- | --- |
| No ContextBrake telemetry, or telemetry below the trigger zone | Move on, stating the unit finished and the next one |
| Telemetry asking for the snapshot (trigger zone, `RED`, or `CRITICAL`) | In `RED`, finish the unit as in monitored mode; in `CRITICAL`, stop at the next consistent point with the partial state in its handoff. Then save the checkpoint `paused` with `safe_to_stop: true` (an `active` checkpoint would make the new session suspect another coordinator), write the snapshot through `.agents/skills/sdd-snapshot/SKILL.md`, and end the reply with `[REQUEST_SESSION_RESET]`, without a question |
| Review as next step, this session wrote the code, and no delegated reviewer is eligible | The same snapshot path with `[REQUEST_SESSION_RESET]`: the new session did not author the code and runs the review |
| Escalation | Rubric above, without `[REQUEST_SESSION_RESET]` |

Without ContextBrake, the session runs until harness compaction; the checkpoint saved at every boundary is enough to resume.

## Resume without arguments

The new session arrives through ContextBrake's `resumeCommand`, without `--prd`. With more than one incomplete checkpoint, resume the `mode: auto` one that is not `awaiting-hil` and was saved last (file modification time); among slices sharing a prefix, the next one in dependency order. If doubt remains, escalate.

## Decision log

A file at `tasks/prd-[slug]/[decision_log]`, append-only, never rewritten. Create it with the title `# Autonomous decisions — [slug]` and one entry per decision, warning, or escalation:

```markdown
## AUTO-NN — [gate or skill/step] — [YYYY-MM-DD HH:MM]

- Question: [as it would be asked of the user]
- Options: [A (Recommended) — effect]; [B — effect]
- Choice: [option] | Escalated: [rubric item]
- Reason: [fact and source, with FR/NFR/DEC/TC/CR IDs or path:line]
- Reversal: [how to undo it and its cost]
```

`AUTO-NN` IDs are stable and cited in `workflow.md`, handoffs, and assumptions. The HIL 3 final summary, automatic or escalated, lists the highest-impact decisions by ID.
