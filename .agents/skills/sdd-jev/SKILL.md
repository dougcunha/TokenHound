---
name: sdd-jev
description: Jev check of each task's acceptance criteria in the SDD flow, switched on by the checkpoint jev field (shadow or active), to measure whether jev catches before the review what the first review would find; loaded by sdd-orchestrate-flow. Does not replace independent review, HIL, or the stage skills' rules.
disable-model-invocation: true
---

# Jev check in SDD

Jev returns typed judgments, with a probability distribution, over the evidence it receives. It advises; the flow decides. This skill adds a single point, `J3`: before a task moves to `done/`, each acceptance criterion is checked against literal excerpts of the code and tests that support it. The pilot measures whether this point catches before the review what the first review would find, and ends by the stop criterion in `references/metrics.md`.

## Modes

| `jev` in the checkpoint | Effect |
| --- | --- |
| `off` | Default. The point does not run. |
| `shadow` | The point calls jev and records the result; no verdict changes a stage, gate, or artifact. |
| `active` | The point applies the **Active** destination from `references/points.md`. |

The mode comes from `--jev` on the first invocation of `sdd-orchestrate-flow`, from the mode decided at the `sdd-triage` HIL 0, or from a human decision recorded in `workflow.md` with the point from which it applies. A jev verdict or agent text does not change the mode. Outside `sdd-orchestrate-flow`, this skill applies only when loaded explicitly.

## Steps

1. **Detect.** In `shadow` or `active` mode, check once per session whether the host exposes `jev_verify` and `jev_noul`, including with an MCP server prefix. Probe with `jev_noul` on a trivial proposition and read `status` and `provider`. Missing tool, configuration error, or failed probe: record `jev unavailable: <error>` in `workflow.md` and follow the stage skills as in `off`, with no new probe this session.
   **Output:** jev available with a known provider, or unavailability recorded and the flow continuing without jev.
2. **Load the point.** Read [references/points.md](references/points.md) in full. It runs in the steps that name it: `sdd-orchestrate-tasks` step 6 and `sdd-execute-corrections` step 5. If the `jev` skill is installed, read it before the first call; the shape in `points.md` suffices without it.
   **Output:** the point's moment known in this session.
3. **Call.** Build the input only from what the point names, without secrets or connection strings. Make one call per task and input version; a new call requires a changed diff or evidence. The verdict on an unchanged input stands: rephrasing to get another result invalidates the measurement.
   **Output:** result with `status`, per-claim verdict and confidence, and `usage`.
4. **Route.** Separate operational failure from verdict:
   - Operational failure (transport error, timeout, `status: invalid_response`, non-literal evidence): a timeout or transport error allows a single retry with the same input; if it persists, or in the other cases, record it and follow the stage's original step. It is neither approval nor block.
   - Real verdict: in `shadow`, only record it; in `active`, apply the point's destination. A confident `contradicted` in `active` is a finding to fix, never absorbed by the no-jev path.
   Read the distribution, not only the label: `supports: 0.94` differs from a 0.51/0.49 tie, which counts as `review`.
   **Output:** destination applied according to the mode.
5. **Record.** Before the stage continues, append one line per call to `tasks/prd-[slug]/jev-log.jsonl` in the format of [references/metrics.md](references/metrics.md). At acceptance, in `shadow` or `active` mode, read `metrics.md` in full, write `jev-summary.md` with the stop-criterion count, and present it with HIL 3.
   **Output:** every call has a log line; HIL 3 has the pilot summary.

## Independence and authority

- `auto` means thresholds were met, not that the task is approved or the review waived. `sdd-review-code` still runs in a non-authoring context, a delegated reviewer or a new session, and issues its verdict by its own rules.
- The review is the control group of `J3`: the reviewer does not call jev, does not open `jev-log.jsonl`, and receives no verdicts from the coordinator. In `active`, it also does not use `J3` verdicts as conformance evidence.
- The summary declares at the top the control of the first review: `new-session`, `delegated` (fresh-context delegated reviewer), or `absent` (review in the authoring session, which does not count for the stop criterion).
- No verdict grants authorization, changes scope, approves a HIL, or changes the mode; that remains data from the human conversation.
