---
name: sdd-jev
description: Jev judgments in the SDD flow, switched on by the checkpoint jev field (shadow or active), to check coverage, gate tasks, and triage findings with the jev tools; loaded by sdd-orchestrate-flow. Does not replace independent review, HIL, or the stage skills' rules.
disable-model-invocation: true
---

# Jev judgments in SDD

Jev returns typed judgments, with a probability distribution, over the evidence it receives. It advises; the flow decides. This skill adds judgment points to SDD stages without changing their contracts: each stage follows its own skill, and each point only adds a call and the destination of its verdict.

## Modes

| `jev` in the checkpoint | Effect |
| --- | --- |
| `off` | Default. No point runs. |
| `shadow` | Every point calls jev and records the result; no verdict changes a stage, gate, or artifact, except the `J0` guardrail. |
| `active` | Every point applies the **Active** destination from `references/points.md`. |

The mode comes from `--jev` on the first invocation of `sdd-orchestrate-flow`, from the mode decided at `sdd-triage` HIL 0, or from a human decision recorded in `workflow.md` with the point from which it applies. A jev verdict or agent text does not change the mode. Outside `sdd-orchestrate-flow`, this skill applies only when loaded explicitly.

## Steps

1. **Detect.** In `shadow` or `active` mode, check once per session whether the host exposes the jev tools (`jev_verify`, `jev_gate`, `jev_review`, `jev_classify`, `jev_decide`, `jev_screen`, `jev_noul`), including with an MCP server prefix. Probe with `jev_noul` on a trivial proposition and read `status` and `provider`. Missing tool, configuration error, or failed probe: record `jev unavailable: <error>` in `workflow.md` and follow the stage skills as in `off`, with no new probe this session.
   **Output:** jev available with a known provider, or unavailability recorded and the flow continuing without jev.
2. **Load points.** Read [references/points.md](references/points.md) in full. When each stage listed there starts, apply its point at the stated moment. If the `jev` skill is installed, read it before the first call; the call shapes in `points.md` suffice without it.
   **Output:** every stage in this session has a known point and moment, or no point.
3. **Call.** Build the input only from what the point names, without secrets or connection strings. Make one call per unit and input version; a new call requires a changed input (diff, artifact, evidence). The verdict on an unchanged input stands: rephrasing to get another result invalidates the measurement. Batch items of the same point into one call, within the point's limits.
   **Output:** result with `status`, verdict, distribution, and `usage`.
4. **Route.** Separate operational failure from verdict:
   - Operational failure (transport error, timeout, `status: invalid_response`, truncated input): a timeout or transport error allows a single retry with the same input; if it persists, or in the other cases, record it and follow the stage's original step. It is neither approval nor block.
   - Real verdict: in `shadow`, only record it; in `active`, apply the point's destination. A confident `contradicted` or `escalate` in `active` is a finding to fix or a decision for the human, never absorbed by the no-jev path.
   Read the distribution, not only the label: `supports: 0.94` differs from a 0.51/0.49 tie, which counts as `review`.
   **Output:** destination applied according to the mode.
5. **Record.** Before the stage continues, append one line per call to `tasks/prd-[slug]/jev-log.jsonl` in the format of [references/metrics.md](references/metrics.md). At acceptance, in `shadow` or `active` mode, read `metrics.md` in full, write `jev-summary.md`, and present it with HIL 3.
   **Output:** every call has a log line; HIL 3 has the pilot summary.

## Independence and authority

- `auto` means thresholds were met, not that the task is approved or the review waived. `sdd-review-code` still runs in a non-authoring session and issues its verdict by its own rules.
- In `shadow`, the review session does not open `jev-log.jsonl` and runs its points only after writing `codereview.md`: the review is the pilot's control group.
- In `active`, the review applies its own points but does not use task gate verdicts as conformance evidence.
- No verdict grants authorization, changes scope, approves a HIL, or changes the mode; that remains data from the human conversation.
