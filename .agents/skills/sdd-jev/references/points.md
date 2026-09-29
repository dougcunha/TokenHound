# Point J3 — Per-criterion check

| Stage and moment | Tool |
| --- | --- |
| `sdd-orchestrate-tasks` step 6 and `sdd-execute-corrections` step 5, after the reread and before moving the task to `done/` | `jev_verify` |

## Input

- `claims` = one per task acceptance criterion, in order: `Criterion <n> is met: <literal criterion text>.`
- `evidence` = one item per criterion, `id` = `criterion-<n>`, with literal text only:
  - the `git diff` hunks against the recorded base that implement the criterion, copied without editing; before the diff, mark new files with `git add -N <files>` (intent to add, with no content staged);
  - the name and result of each test that proves the criterion, with its `TC-NN`, copied from the runner output;
  - the build, typecheck, or lint output, when the criterion is about them.

  Add a `suite` item with the summary lines of the latest full suite run (counts and failures).
- `## Handoff` does not enter the evidence: a claim checked against the author's own statement proves nothing.
- A summary, paraphrase, or reconstructed hunk is not evidence: without the literal excerpt, record `operational-failure` and do not count the call. A criterion with no excerpt to support it enters with an empty item and the note `no-excerpt`; the resulting `unsupported` is a signal, not a failure.

## Limits

Each item holds up to 20,000 characters: select only the criterion's hunks. A criterion that needs more is split into calls by group of criteria, recorded as one unit. A task whose diff does not fit this way is recorded with its size in the log, as a signal for planning.

## Record

Write each claim's verdict and confidence and `chars_sent`, the sum of the characters in `claims` and `evidence`.

## Shadow

Run after closing `## Handoff` and go straight to recording the task, without opening the result to decide. If the diff changes after the call, record `effect: diff-changed` and a new line on the new diff.

## Active

- `contradicted` → treat as a reread finding: fix and run again on the new diff.
- `unsupported` or confidence below `auto_accept` (0.8) → check it against the lines; fix or record a justification in `## Handoff`.
- Every claim `verified` at `auto` confidence → continue to recording the task.
- Two calls without progress follow the stage skill's block-after-two-attempts rule.
