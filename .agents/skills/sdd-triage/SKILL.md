---
name: sdd-triage
description: SDD triage that recommends to the human the process level for a feature, fix, or refactoring request — full SDD, lean SDD, or spot change — from risk signals with evidence, before any artifact exists. Use when starting sdd-orchestrate-flow on a feature without a checkpoint or when asked whether a request is worth the SDD flow. Don't use for resuming a feature with a checkpoint or for slicing a broad request into several PRDs (use sdd-orchestrate-prds).
argument-hint: --prompt "request description" [--mode auto|monitored] [--log-decisions path|false]
---

# Triage a request for SDD

Triage spends little to avoid spending a lot: it gathers signals with evidence, applies a rubric, and recommends a level; the human decides. Budget: up to ten targeted searches or reads, with no build or tests. Triage that needs more than that to understand the request already points to `sdd-full`.

| Level | Path |
| --- | --- |
| `sdd-full` | `sdd-orchestrate-flow` with every gate |
| `sdd-lean` | `sdd-orchestrate-flow` with a short PRD (problem, RF with acceptance, out of scope), HIL 1 and HIL 2 merged into one decision, a plan of one or two tasks, and independent review kept |
| `spot` | The spot branch of this skill, outside SDD |

1. **Understand.** Extract the type (feature, fix, refactoring), the primary outcome, and the expected behavior. With an existing checkpoint for the request, stop and resume through `sdd-orchestrate-flow`. With more than one primary outcome, recommend `sdd-full` through `sdd-orchestrate-prds` and go to step 4.
   **Output:** request with one identified primary outcome, or a resume or slicing referral.
2. **Gather signals.** Read `AGENTS.md` for critical areas, risk skills, and commands. Locate the likely change points through the repository's context graph when it has one (e.g. `graft ask`, `graft callers <symbol> --depth 2`), or by direct search. Fill each signal with `path:line` evidence or `not measured`:

   | Signal | Question |
   | --- | --- |
   | S1 Public contract | Does it change what a consumer sends or receives: an API or MCP surface, a serialized DTO, a schema or database script, a file format, or configuration? Documentation-only text (description, example, grouping) counts as absent. |
   | S2 Critical area | Does it touch an area that `AGENTS.md` declares critical or that requires a risk skill (e.g. credentials, provider authentication, payments)? |
   | S3 Concurrency | Does it involve a transaction, lock, idempotency, queue, background polling, or shared state? |
   | S4 Blast radius | How many production files, modules, and transitive callers change behavior? |
   | S5 Open decision | Is there an ambiguous requirement or an unanswered product decision? |
   | S6 Obligations | How many distinct observable behaviors does the request require? |
   | S7 Tests | Do tests exercise the behavior, or does a direct regression test fit? |
   | S8 Reversibility | Does it persist data, migrate state, or trigger an external action that is hard to undo? |

   **Output:** every signal filled with evidence or `not measured`.
3. **Recommend.** Apply the rubric, whose thresholds are a starting point calibrated by the triage log:
   - `sdd-full` when any holds: S1, S2 with a behavior change, S5, S8, S6 above three obligations, or S4 across more than one module.
   - `spot` when all hold: one module, up to three production files, none of S1, S2, S3, S5, or S8, an unambiguous expected behavior, and a feasible regression test (S7).
   - `sdd-lean` otherwise.
   A `not measured` signal that would decide the level counts as present.
   **Output:** rubric level with the deciding signals.
4. **HIL 0.** Present the level the rubric recommends, the deciding signals with evidence, and what each level costs in artifacts, HILs, and review. Ask with the available question tool, recommended option first. Silence keeps the decision pending. With `--mode auto`, do not ask: the rubric level is the decision, recorded as autonomous, and the protocol in `.agents/skills/sdd-orchestrate-flow/references/autonomous-mode.md` applies from here on. Append one line to `tasks/triage-log.jsonl` with date, summarized request, signals, rubric level, and human decision, or `autonomous` in auto mode.
   **Output:** level decided by the human and recorded.
5. **Continue.** `sdd-full` and `sdd-lean` return to `sdd-orchestrate-flow`, passing on `--mode` and `--log-decisions` when supplied, and the flow records the triage as a decision in `workflow.md`; for `sdd-lean`, also record the merge of HIL 1 and HIL 2 as a change to the stops. `spot` follows the branch below.
   **Output:** next step started on the decided path.

## Spot branch

1. **Implement.** Make the smallest coherent change with a regression test that fails before and passes after, or a behavior test when there is no defect. Validate with the repository's validation skills.
   **Output:** change applied and test result recorded before and after.
2. **Tripwire.** Stop and return to step 2 with the new evidence when the diff exceeds the file limit decided at HIL 0 (default: three production files), touches S1, S2, S3, or S8, or a product decision appears. The change stays in the worktree as evidence; discarding it is a human decision.
   **Output:** diff confirmed within the limit, or triage reopened with the new evidence.
3. **Review.** Review the diff with the repository's review skill or the host's native review.
   **Output:** review findings fixed or taken to the human.
4. **Deliver.** Commit only when asked, through the `commit` skill.
   **Output:** delivery in the requested state, with the regression test and review as evidence.
