# Jev pilot summary — 12-windows-autostart

- Mode: `active` (triage HIL 0; workflow DEC-01).
- Control of the first review: `delegated` (fresh-context subagent, which did not read `jev-log.jsonl` or call jev).
- Authoring model: Claude Opus 5.5 (`claude-opus-5-5`).
- Skill version change mid-flow: the `sdd-jev` skill was reduced to per-criterion J3 in commit `b9bffe6` during this feature. The J1, J2, and J3 calls for T01/T02 used the earlier points (J3 through `jev_gate`, with per-criterion claims but whole-diff evidence). The corrections T03..T06 used the new per-criterion `jev_verify` format. So this feature does **not** count toward the stop criterion, whose counted features need per-criterion J3 on the tasks the first review judged.

Source: `jev-log.jsonl` (8 lines), the handoffs in `done/` and `codereview_1/done/`, and `codereview_1`/`codereview_2`.

## 1. J3 against the first review

`codereview_1` findings, by owning task and criterion:

| Finding | Owner task | Criterion | J3 claim | Classification |
| --- | --- | --- | --- | --- |
| **CR-01 (Medium, drove `REJECTED`)**: `CreateDefault` returns an empty path when the Startup folder is missing | T01 (`StartupLaunchService.cs`) | T01 criterion 3 ("creates the folder when missing") | verified, 0.99, `auto` | **miss** |
| CR-02 (Low): failure log lacks `{Operation}` | T02 (`StartupSettingsViewModel.cs`) | no T02 criterion (TechSpec Observability) | — | outside the criteria, not counted |
| CR-03 (Low): 4-argument calls on one line | T01 (`StartupLaunchService.cs`, `ShellLink.cs`) | no T01 criterion (CLAUDE.md style) | — | outside the criteria, not counted |
| CR-04 (Low): manifest and handoff state lag the visual check | flow artifacts | none | — | outside the criteria, not counted |

Flagged claims with no `CR-NN` in their criterion:

- **T01 false alarms (3):** C6 (per-user locations, 0.76), C8 (Core purity/COM/build, 0.71), and C9 (handoff full runs, 0.74). C9 was answered by rerunning the suites; the diff did not change.
- **T02 false alarms (3):** C4 (synchronous + no-stall, 0.39), C5 (tab markup, 0.67), and C6 (installer script, 0.76). Each mixed a code part with a manual part that was pending then and later passed MA-1..MA-4.
- **Hits confirmed by the author:** 0. The working-directory assertion added to T01 before its gate came from the author's own reread, not from a flag.
- **False alarms:** 6 over 2 tasks, 3.0 per task (above the limit of 1 per task).
- **Rubric-only `escalate`:** both `jev_gate` calls, with `safe_to_apply` 0.44. This is unspecific, as in prd-09.

Corrections (new format, not part of the first-review comparison):

- T03, T04, and T06: every criterion verified at `auto`.
- T05: criterion 2 came back `review` (0.77) because its evidence had only filtered tests. It was justified by the full runs.

## 2. Flow

- `codereview_1`: `REJECTED` (CR-01 block, plus CR-02..CR-04 Low).
- Correction round 1: T03..T06.
- `codereview_2`: `APPROVED WITH RESERVATIONS`, with no actionable findings and optional items OI-01..OI-03.
- Reservations HIL: the human chose to correct OI-02 directly and finalize with acceptance. OI-01 and OI-03 are accepted as open items.
- Rounds: 1. Reopened tasks: none.

## 3. Cost

- Returned jev usage across the 8 logged calls:
  - Input: 57,535 tokens (J1 11,094; J2 11,897; J3 T01 14,901; J3 T02 14,219; corrections 5,424).
  - Output: 9,241 tokens.
  - Outside the log: the triage `jev_decide` and the session probe (`jev_noul`, 363 in / 25 out).
- `chars_sent` was recorded for the 4 new-format calls: 8,600 characters, about 2,150 tokens. It was not recorded for the 4 earlier calls. Those carried whole-file diffs or TechSpec sections, estimated at about 80,000 characters (about 20,000 tokens) the agent wrote to build them.
- Duration: unmeasured (no telemetry).

## 4. Baseline

Features without jev, from their `codereview_*`:

| Feature | First review | Reviews to close |
| --- | --- | --- |
| prd-06 | `REJECTED` | 4 |
| prd-07 | `REJECTED` | 2 |
| prd-08 | `APPROVED WITH RESERVATIONS` | 2 |

- 2 of 3 had a `REJECTED` first review, averaging 2.7 reviews.
- This feature: first review `REJECTED`, closed at the 2nd review.
- The sample is small (3 features) and differs in size, so it is a limitation.

## Stop criterion

- Counted features with per-criterion J3 and a delegated or new-session control: **0**, since prd-09 and prd-12 both used the earlier gate format on the first-review tasks.
- As a data point only:
  - In this feature, the one finding that drove `REJECTED` (CR-01) was a **miss**, verified at 0.99.
  - False alarms ran at 3 per task.
  - prd-09 had one weak hit and five false alarms.
- If the counted-feature rule is applied, both point toward **ending jev**. The decision is human, recorded in `workflow.md`, and outside this feature's acceptance.
