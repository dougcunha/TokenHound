# Jev pilot summary — 09-provider-status-window

Mode `shadow` throughout (workflow DEC-01). No verdict changed a stage, gate, or artifact. Source: `jev-log.jsonl` (25 calls), handoffs, and `codereview_1..5`.

## 1. Task gate (J3) against the first review

`codereview_1` findings by owning task: CR-01 blocking (TC-05/TC-09 scenarios missing) → T01; CR-02 reservation (blocked alert colour, not carried into the TechSpec) → T01/T02; CR-03 informational (DEC-06) → T01.

| Task | J3 action | Flags | First-review outcome | Classification |
| --- | --- | --- | --- | --- |
| task_01 | escalate | rubric `test_gap` 0.40; criteria 2 and 3 `review` | CR-01 blocking: test gap | **hit (weak)**. The gate flagged a test gap, which is the cause the review found, but not on criterion 1 (the TC-01..TC-09 criterion). Its diff was condensed with test bodies omitted, so the flag may reflect the missing input rather than the gap itself |
| task_02 | escalate | criteria 1, 2, 5 `escalate`; 3, 4 `unsupported`; `safe_to_apply` 0.27 | no blocking finding | **false alarm**. The XAML was summarized and the diff condensed; every criterion was later confirmed by tests, MA-01..MA-04, and review |
| task_03 (correction) | escalate | criterion 1 `unsupported`; low-confidence rubric | `codereview_2` resolved codereview_1/CR-01 | **false alarm** (condensed diff; the gate ran after the move to `done/`) |
| task_04 (correction) | escalate | criteria 1 and 3 `unsupported`; others low confidence | `codereview_3` resolved codereview_2/CR-01 | **false alarm** (hand-assembled excerpts of untracked files) |
| task_05 (correction) | escalate | `safe_to_apply` 0.42, correctness limiting; criterion 2 `escalate` (0.38) | `codereview_4` found no defect in T05 (CR-02 was outside its scope) | **false alarm** |
| task_06 (correction) | escalate | `safe_to_apply` 0.61, composite 0.80; one handoff claim `review` (0.73) | `codereview_5` resolved codereview_4/CR-02 with no new finding | **unspecific signal** (rubric only; every claim verified) |

- Hits confirmed by the author: 0 (no `diff-changed` or `fixed` effect).
- Misses: 0 (no `auto` gate was followed by a blocking finding).
- All 6 J3 calls returned `escalate`, most on a condensed or hand-assembled diff. T06, a small XAML-only diff passed almost literally, still escalated (`safe_to_apply` 0.61) with every claim verified. A gate that always escalates carries no signal in this pilot. The main confound is input fidelity: the untracked files and the 50 KB limit prevented passing the literal diff.

## 2. Coverage (J1, J2)

| Point | Flagged | Confirmed | Found by review without a flag |
| --- | --- | --- | --- |
| J1 (PRD → TechSpec) | NFR-07 `contradicted`; US-01..US-05 `review` | NFR-07: yes. It was the known DEC-03 deviation, accepted by the human at HIL 2. US items: no gap confirmed | The PRD UX blocked alert colour not carried into the TechSpec (codereview_1/CR-02) was not flagged |
| J2 (TechSpec → tasks) | FR-08 `review` (0.86 supports) | Not confirmed; FR-08 is conformant in all five reviews | The TC-05/TC-09 scenario gap (codereview_1/CR-01) was not flagged at planning |

## 3. Review (J4, J5, J6)

| Review | J4 agreement with the matrix | J5 severity agreement | J6 destination agreement |
| --- | --- | --- | --- |
| codereview_1 | TC-05, TC-09 `contradicted`, which agrees with CR-01. FR-06 `contradicted (review)` diverged: the reviewer marked it conformant. NFR-04 `contradicted` vs reviewer `not verifiable` | Diverged on 2 of 3. CR-02 `blocking (review)` vs reviewer reservation; CR-03 `reservation (review)` vs reviewer informational | CR-02 aligned (informational). CR-03 `informational` 0.52/0.42 vs planner `pending`: diverged, but near a tie |
| codereview_2 | NFR-04 and NFR-06 `contradicted` vs reviewer `not verifiable`; NFR-07 `review`. Other rows agree | Agreed on 2 of 2 (auto) | CR-01 aligned (actionable, auto). CR-02 `informational (review)` vs planner `pending`: diverged |
| codereview_3 | 16 of 18 rows `verified`. NFR-04 and NFR-06 `contradicted` vs reviewer `not verifiable`; FR-12 and NFR-07 `review` | Agreed on 1 of 1 (CR-01 informational, auto) | Aligned (auto) when T05 was planned after the HIL 3 rejection |
| codereview_4 | 17 of 22 `verified`. FR-10, the T04 visual, NFR-04, NFR-06, and TC-11 `contradicted`, all matching the reviewer's `not verifiable`/`pending`; FR-09 `review` | CR-02 agreed (auto). CR-01 `informational` in a 0.50/0.48 tie with `reservation` (review) | Aligned on 2 of 2 (auto) |
| codereview_5 | 19 of 25 `verified`. FR-10, DEC-10 (1) Windows 10 fallback, NFR-04, NFR-06, and TC-11 `contradicted`, all matching the reviewer's `not verifiable`/`pending`. FR-08 `unsupported` (0.40; the evidence given lacked the formatter tests) diverged from the reviewer's `conformant`. The T04 visual `verified` at 0.69 (review), matching the reviewer, who now has on-screen evidence | Agreed on 1 of 1 (CR-01 informational, auto 0.99) | — (no correction planning) |

- Recurring divergence: J4 maps "manual evidence not run" to `contradicted`, while the review uses `not verifiable`. The mapping in `points.md` routes `contradicted` to `non-conformant`, which would over-reject in `active` mode.
- J7 (reservations HIL): at `codereview_2` it recommended `correct-cr01` (0.60), and the human chose to correct. At `codereview_3` it recommended `finalize-keep-dec06` (0.67), and the human chose to finalize. At `codereview_4` it recommended `correct-cr02` (0.91), and the human chose to correct, extending the scope to Settings. It agreed with the human on 3 of 3.

## 4. Flow

- First review: `REJECTED` (codereview_1).
- Rounds: 4 correction rounds (T03, T04, T05, T06). The automatic cycle closed at `codereview_3` (`APPROVED WITH RESERVATIONS`, DEC-09). HIL 3 was then not accepted (DEC-10: visual changes, T05), and a further reservation was corrected (DEC-12: dark scrollbar, T06). The latest review is `codereview_5`, `APPROVED WITH RESERVATIONS` with only the accepted CR-01.
- Only rounds 1 and 2 came from review findings on the original contract. Rounds 3 and 4 were human-requested visual scope.
- Reopened tasks: T01 was completed through correction T03 without moving it back to the root.

## 5. Cost

- Total: 93,918 input + 19,737 output tokens over 25 calls.
- By point (input/output): J1 9,521/2,998; J2 8,456/2,427; J3 ×6 26,546/1,948; J4 ×5 35,088/10,138; J5 ×5 4,634/447; J6 ×4 2,906/396; J7 ×3 6,767/1,383.
- Duration: unmeasured. The host exposes no per-call latency, and the log timestamps cover 14:05–16:41 (about 156 minutes of feature activity, not jev time).

## 6. Baseline

Features in this repository without jev, counted from `codereview_*/codereview.md`:

| Feature | First review | Reviews | Correction rounds |
| --- | --- | --- | --- |
| prd-06-mcp-metrics | REJECTED | 4 | 3 |
| prd-07-claude-multi-profile | REJECTED | 2 | 1 |
| prd-08-claude-quota-breakdown | APPROVED WITH RESERVATIONS | 2 | 1 |
| **prd-09 (jev shadow)** | REJECTED | 5 | 4 (2 from findings, 2 human visual scope) |

- Without jev, 2 of 3 first reviews were `REJECTED`, with an average of 1.7 rounds. Counting only finding-driven rounds, this feature (shadow, so jev could not change outcomes) has 2 and falls within that range.
- Limitation: a sample of 3. Shadow mode cannot show an outcome effect by design.

## Reading

In this pilot, the review-side points J5 and J7 were well aligned with the human and the reviewer: J5 matched the reviewer on 7 of 9 severities (one of them a 0.50/0.48 near-tie), with both divergences in `codereview_1`, and J7 matched the human on 3 of 3. J3 and J4 are not usable as they are. J3 escalated on all 6 tasks, including a near-literal diff, and J4 treats pending manual evidence as `contradicted`. Before any point moves to `active`, the literal diff input for J3 and the J4 mapping for manual obligations need fixing. Adopting any point in `active` is a human decision to record in `workflow.md`.
