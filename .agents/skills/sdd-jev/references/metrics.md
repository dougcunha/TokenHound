# Pilot metrics

## Log line

`tasks/prd-[slug]/jev-log.jsonl` receives one JSON object per line, appended right after each call and before the stage continues. The log only grows: a written line is never edited or rewritten; a wrong record is corrected by a new line with `corrects` pointing to the wrong line number.

| Field | Content |
| --- | --- |
| `ts` | Real date and time of the call in ISO 8601, read from the system (e.g. `Get-Date -Format o`), never a fixed time: pilot duration comes from these values |
| `mode` | `shadow` or `active` |
| `session` | `authoring` or `reviewing` |
| `point` | `J0` to `J7` |
| `stage` | Skill and step, e.g. `sdd-orchestrate-tasks#6` |
| `unit` | Task, review, or artifact judged, e.g. `task_03`, `codereview_1` |
| `tool`, `status` | Tool called and returned `status` |
| `action` | `auto`, `review`, `escalate`, `pass`, `block`, `skip`, or the `jev_decide` recommendation |
| `flagged` | IDs with a verdict other than `verified`/`auto` and that verdict, e.g. `{"RF-03": "unsupported"}` |
| `effect` | `none` (shadow), `diff-changed` (shadow with a change after the call), `fixed`, `justified`, `blocked`, or `operational-failure` |
| `numbers` | In `J3`: `safe_to_apply`, composite, rubric scores, and per-claim confidence; in other points, the confidence of each flagged item |
| `usage` | Returned `input_tokens` and `output_tokens` |
| `corrects` | Optional: number of the line this record corrects |
| `control` | `absent` when the review runs in the authoring session; `delegated` when it runs in a fresh-context delegated reviewer; omitted when it runs in a new session |

## Summary at acceptance

Write `tasks/prd-[slug]/jev-summary.md` from the log, handoffs, and reports, with counts and IDs:

1. **Task gate (`J3`) against the first review.** Assign each `CR-NN` in `codereview_1` to the task that owns the files and to the acceptance criterion or `TC-NN` it affects. The gate returns scores and per-claim confidence, not causes; classify per criterion:
   - *hit*: the criterion's claim flagged (`unsupported`, `contradicted`, or confidence below `auto_accept`) and the review found a `CR-NN` in that criterion;
   - *false alarm*: claim flagged with no `CR-NN` in the criterion;
   - *miss*: claim `verified` at `auto` confidence and a `CR-NN` in the criterion;
   - *hit confirmed by the author*: claim flagged and the diff changed before `done/` (`diff-changed` or `fixed`), counted separately because the review can no longer find what was fixed;
   - *unspecific signal*: `review` or `escalate` from the rubric alone, with no flagged claim; count it separately, as neither hit nor alarm.

   A gate with `operational-failure`, including a summarized diff, stays out of the counts. List `safe_to_apply` and composite per task to calibrate thresholds. In `active`, also count the fixes made because of the gate.
2. **Coverage (`J1`, `J2`).** Gaps flagged, how many the human or the review confirmed, and how many the review found without a flag.
3. **Review (`J4`, `J5`, `J6`).** Agreement per matrix row, severity, and destination, with the divergences listed.
4. **Flow.** First review status, rounds until `APPROVED` or decided reservations, and reopened tasks.
5. **Cost.** Tokens per point and total; duration when the host exposes it. Without duration telemetry, declare it unmeasured.
6. **Baseline.** From features in the same repository without jev: share with a `REJECTED` first review and average rounds, counted in `codereview_*/codereview.md`. Declare a small sample as a limitation.

With `control: absent`, the feature enters only items 4 (Flow) and 5 (Cost), with the absence declared at the top; hits, alarms, misses, and agreement stay out of the counts.

The summary informs; adopting a point in `active` or removing the mode is a human decision recorded in `workflow.md`.
