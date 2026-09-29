# Pilot metrics

## Log line

`tasks/prd-[slug]/jev-log.jsonl` receives one JSON object per line, appended right after each call and before the stage continues. The log only grows: a written line is never edited or rewritten; a wrong record is corrected by a new line with `corrects` pointing to the wrong line number.

| Field | Content |
| --- | --- |
| `ts` | Real date and time of the call in ISO 8601 with the local offset, read from the system (e.g. `Get-Date -Format o`), never a fixed time or UTC |
| `mode` | `shadow` or `active` |
| `stage` | Skill and step, e.g. `sdd-orchestrate-tasks#6` |
| `unit` | Task judged, e.g. `task_03` or `codereview_1/task_04` |
| `status` | Returned `status`, or `not-called` with the reason in `note` |
| `flagged` | Criteria with a verdict other than `verified` at `auto` confidence, with the verdict, e.g. `{"criterion-2": "unsupported"}` |
| `confidences` | Each claim's confidence, per criterion |
| `effect` | `none` (shadow), `diff-changed` (shadow with a change after the call), `fixed`, `justified`, `blocked`, or `operational-failure` |
| `chars_sent` | Sum of the characters in `claims` and `evidence`: estimates the tokens the agent itself wrote to build the call |
| `usage` | Returned `input_tokens` and `output_tokens` |
| `corrects` | Optional: number of the line this record corrects |
| `note` | Optional: failure reason, call split, or diff size |

## Summary at acceptance

Write `tasks/prd-[slug]/jev-summary.md` from the log, handoffs, and reports. At the top: mode, control of the first review (`new-session`, `delegated`, or `absent`), and the model that ran the authoring session, when known.

1. **`J3` against the first review.** Assign each `CR-NN` in `codereview_1` to the task that owns the files and to the acceptance criterion or `TC-NN` it affects. Per criterion:
   - *hit*: claim flagged and a `CR-NN` in the criterion;
   - *false alarm*: claim flagged with no `CR-NN` in the criterion;
   - *miss*: claim `verified` at `auto` confidence and a `CR-NN` in the criterion;
   - *hit confirmed by the author*: claim flagged and the diff changed before `done/` (`diff-changed` or `fixed`), counted separately because the review can no longer find what was fixed.

   A call with `operational-failure` or `not-called` stays out of the counts; list the task and the reason. Mark the findings that drove the first review to `REJECTED`.
2. **Flow.** First review status, rounds until `APPROVED` or decided reservations, and reopened tasks.
3. **Cost.** Tokens returned by jev and total `chars_sent`, with the estimate of tokens the agent wrote (characters ÷ 4). Duration when the host exposes it; without telemetry, declare it unmeasured.
4. **Baseline.** From features in the same repository without jev: share with a `REJECTED` first review and average rounds, counted in `codereview_*/codereview.md`. Declare a small sample as a limitation.

With control `absent`, the feature enters only items 2 and 3, with the absence declared at the top.

## Stop criterion

The features that count have `J3` per criterion and control `new-session` or `delegated`. Each feature's summary adds the previous ones and states where the criterion stands:

- **End and remove jev from SDD** when, across the counted features, no finding that drove a first review to `REJECTED` got a hit or a hit confirmed by the author, or when false alarms exceed one per task on average. The count applies after two features with at least one such finding; with no such finding, count up to four features and, with four and none, end as well.
- **Move `J3` to `active`** when at least one of those findings got a hit or a hit confirmed by the author, with false alarms within the limit.

The decision is human and recorded in `workflow.md`. Delete targets when ending: the `sdd-jev` skill, the `With the sdd-jev skill…` sentences in the stage skills, the jev mode question at the `sdd-triage` HIL 0, the `jev` field in the checkpoint and `hil-state.md`, and the step 1 passage of `sdd-orchestrate-flow`.
