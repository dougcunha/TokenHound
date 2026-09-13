---
name: sdd-execute-corrections
description: SDD execution when tasks from a review need correction; does not create or reclassify findings.
argument-hint: --prd feature-name --num review-number
disable-model-invocation: true
---

# Execute SDD corrections

If the caller limits execution to a batch, return after step 5 before starting another batch: `batch-completed` with pending IDs if the batch was approved, or `blocked` with evidence if the batch still has a failure or no task is eligible. When all tasks are complete, execute step 6 before returning. A batch does not end the review.

1. Fix one review by argument/context and require `codereview.md`. Inventory root and `done/` tasks by ID, findings, acceptance, dependencies, and files. If ambiguous, request a choice; if the plan is missing, direct to `sdd-plan-corrections`.
   **Output:** exact review, with no duplicates or missing/circular dependencies. If all are complete, continue to joint validation.
2. Select eligible tasks. Delegate one task per subagent using this skill, with the exact path and instruction to execute **only step 3**, without delegating, reviewing, or moving files. The coordinator executes steps 1, 2, and 4 through 6. Without subagents, execute sequentially and keep review separate from the author.
   Parallelize only without collisions in files, contracts, configuration, and test resources.
   **Output:** batch with exclusive owners and confirmed correction authorization.
3. In the executor, read the stable report and task once per version; then retrieve relevant PRD/TechSpec sections, code, and skills. Trace the cause and implement within the limits. Apply TechSpec validation; in desktop C#/.NET, omit E2E even in legacy commands. Record unit, integration, required manual checks, and limitations; when available, use `dotnet-efficient-validation`. Run the quality profile's blocking commands over the files the correction touched: fixing one finding without reintroducing another is part of the task's acceptance.
   Update only the assigned task and its single `## Handoff` with result, files, commands, validated version, and pending items. Preserve the report, other tasks, and global state.
   **Output:** implementation/evidence or reproducible block; executor returns to coordinator.
4. Review diff and handoff independently of the author. Reuse proven tests from the same state; run only missing/invalidated checks. Return findings to the same executor; after two attempts without new evidence, record a block and advance independent work. Architecture/scope divergence requires HIL when not covered by existing authorization.
   **Output:** acceptance of each task proven or a specific pending item; unexecuted essential manual work prevents approval.
5. Move only approved tasks to `done/`, preserving names and checking absolute paths inside the review. Preserve the immutable report. Recalculate the DAG from remaining files.
   **Output:** every approved task moved; pending tasks at root. On interruption, check review/handoff before inferring completion from the folder.
6. Validate the integrated set without repeating already valid commands. Check every actionable finding and its evidence. In the orchestrated flow, return the execution report so the caller can delegate `sdd-review-code`; in standalone use, request independent review by that skill and create a new immutable report.
   **Output:** tasks complete with integrated evidence and re-review issued or explicitly returned to the caller; persistent/new findings remain open until a decision.

If the environment prevents acceptance, keep the affected task pending with command, error, and impact. If there is a collision, stop affected writers, wait for completion confirmation, and reconcile changes before resuming sequentially.
