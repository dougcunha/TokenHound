---
name: sdd-orchestrate-flow
description: SDD flow for conducting a feature with subagents and HIL or resuming its checkpoint in a new session; does not replace a standalone stage.
---

# Orchestrate the SDD flow

Coordinate contracts, execution, and acceptance. The coordinator maintains state and human decisions; subagents produce artifacts and code in exclusive scopes.

1. **Prepare or resume.** Read [references/hil-state.md](references/hil-state.md) in full and detect `tasks/prd-[slug]/checkpoint.json` before starting work. With an explicit feature, consult only its folder; without one, select the only pending checkpoint or request a choice if there are several. Automatically resume the valid checkpoint for the selected feature, without requiring a special flag. Without a checkpoint, reconcile existing artifacts before creating state.
   Locate the skills in the table below in the installed catalog or `SKILLS/`; leave each stage's body to its responsible agent. Before delegating, read [references/delegation.md](references/delegation.md) in full. On resume, load only the index, relevant decisions, and sources needed for the next stage.
   Check worktree, local instructions, subagent tools, and existing sources. Record the Git base as a resolved commit and pre-existing changes; without Git, record scope limits. Require dependencies for the next phase; a missing skill blocks only that phase, without inventing equivalent execution. Preserve existing invocation policies: pass the exact name and path explicitly to the subagent.
   **Output:** reconciled state, known authorization, and identified next stage. Without subagents, prepare sources/state and report the limitation; request a choice before replacing the requested flow with local execution.
2. **Product.** Delegate PRD creation/update; reuse a valid existing artifact. For an explicitly requested refactoring, delegate `sdd-plan-refactoring` and use its PRD in the same gate, keeping the TechSpec as a draft until technical HIL. With more than one primary outcome in the request, delegate `sdd-orchestrate-prds` first: it approves the slicing with the user and writes one PRD per slice under a shared prefix; record the approved slicing as a decision in `workflow.md`.
   Check request coverage and present the written PRD at **HIL 1**, with product decisions and pending items. When sliced, present the whole set at a single HIL 1 and drive each slice as its own feature from step 3 onward, in dependency order. Reuse existing approval only if it matches current content/scope.
   **Output:** approved PRD and recorded decisions; when sliced, every slice has an approved PRD or an explicit deferral. A blocking pending item prevents dependent stages.
3. **Design and plan.** Delegate TechSpec, check PRD coverage, and then delegate task planning. Identify the stack per target; in desktop C#/.NET, omit E2E and keep relevant unit, integration, and manual acceptance. Require commands that also exclude E2E from aggregate suites. Preserve product behavior when choosing checks.
   Present TechSpec + DAG + concrete tasks at **HIL 2**: architecture, boundaries, required environments, manual acceptance, and authorization for implementation/corrections within those contracts. Correct documents before requesting a decision; intermediate questions are only for indispensable information.
   **Output:** approved traceable executable plan; no code writer started before required authorization.
4. **Implement.** Delegate `sdd-orchestrate-tasks`, giving it exclusive ownership of the manifest and moves while active. It delegates executors and reviews each task. Adjust depth/concurrency to real slots; if a nested coordinator does not fit, the root coordinator assumes the DAG described in this skill and delegates `sdd-execute-task` directly.
   Limit each call to an eligible batch and receive handoffs/state before authorizing the next. Save a checkpoint and offer continuation or pause between batches; handle deviations through **exception HIL**.
   **Output:** all tasks approved with integrated evidence or identified blocks; `done/` alone does not prove completion.
5. **Review and correct.** Delegate global review to an agent different from authors with `sdd-review-code`, Git base, and current artifacts. If rejected, delegate `sdd-plan-corrections` and then `sdd-execute-corrections` for the exact review. Corrections within HIL 2 proceed automatically; new scope, contract change, or external action without authorization requires exception HIL on a concrete plan.
   Limit corrections to one batch per call and offer a pause after each reconciled return. After all corrections, delegate a new independent review; the executor returns control without creating a duplicate review. Compare full finding identity (folder + ID), cause, and evidence. An incomplete original task finding requires reconciling its evidence and manifest by the DAG owner; completing only the correction task does not close the original obligation.
   **Output:** approved review or real block with evidence. After two rounds without proven reduction/change in blocks, stop the automatic cycle and present diagnosis/decision at exception HIL; no limit turns rejection into approval.
6. **Acceptance.** Check complete request, PRD, TechSpec, manifest, corrections, latest review, and validations of integrated state. Present paths, results, limitations, manual acceptance, and relevant ADR candidates at **HIL 3**. Reservations are optional only if they leave no essential requirement/security/validation pending.
   **Output:** current human acceptance recorded and no blocking obligation open before marking `completed`. Publish PR, commit/push/deploy, or promote an ADR only if requested/authorized; flow completion does not require them.

## Delegated stages

Save a checkpoint before each HIL question, after recording its answer, between batches, and after each review. Offer manual session switching at these points under the state protocol; pausing does not approve a gate or complete the feature. On completion, mark the checkpoint `completed` to prevent automatic resumption of closed work.

| Available input | Responsible skill | Artifact/result |
| --- | --- | --- |
| Request | `sdd-create-prd` | `prd.md` |
| Request with more than one primary outcome | `sdd-orchestrate-prds` | one `prd.md` per slice under a shared prefix |
| Requested refactoring | `sdd-plan-refactoring` | `prd.md`, `techspec.md` |
| Approved PRD | `sdd-create-techspec` | `techspec.md` |
| PRD + TechSpec | `sdd-plan-tasks` | `tasks.md`, `task_*.md` |
| Approved plan | `sdd-orchestrate-tasks` / `sdd-execute-task` | implementation and handoffs |
| Implementation | `sdd-review-code` | `codereview_[num]/codereview.md` |
| Review with authorized findings | `sdd-plan-corrections` | tasks in the review folder |
| Planned corrections | `sdd-execute-corrections` | corrections and handoffs; return to review |
