---
name: sdd-execute-task
description: Execution of one exact SDD task, standalone or delegated; does not plan or approve its own work.
argument-hint: --task tasks/prd-name/task_01.md
---

# Execute an SDD task

1. Resolve one `tasks/prd-[slug]/task_[num].md`. Require the PRD, TechSpec, and feature manifest. Read stable sources once per version in PRD, TechSpec, task order; consult state afterward. Confirm completed dependencies and a pending task. A task in `done/` is only reported.
   **Output:** exact contract, satisfied dependencies, and identified write scope.
2. Read local instructions and only skills relevant to the change. Inspect worktree, callers, and affected tests; preserve pre-existing changes. Map every acceptance item to implementation and evidence.
   **Output:** known change points; source or write conflicts returned to the caller before mutation.
3. Implement the smallest coherent change and behavior tests proportional to risk. Mark subtasks only with evidence. Edit only assigned files and the task itself; reserve manifest and moves for the caller.
   **Output:** implementation limited to the contract, with no global state changed.
4. Apply the TechSpec profile. In desktop C#/.NET, omit E2E even when legacy commands include it: select projects/filters without E2E and record the divergence. Preserve acceptance with relevant unit, integration, and manual scripts; unexecuted manual work remains pending. When available, use `dotnet-efficient-validation` for runner and build reuse.
   Run checks required by the diff; reuse evidence only from the same code, configuration, and environment. Zero tests or listing are not success. Record pre-existing failures separately.
   **Output:** every acceptance item has evidence or a reproducible block.
5. Update one `## Handoff`: result, files, commands, results, validated version, and pending items. Return a short summary and path for independent review; on retry, change the same section and preserve valid evidence.
   **Output:** handoff sufficient for the caller to review diff, tests, and acceptance; task remains at root until approval.

## Decisions and failures

- Record an ADR candidate only for a durable decision with alternatives and trade-offs governing contracts, boundaries, or quality attributes. Use `TXX-ADR-NN`, title, context, decision, alternatives, consequences, evidence, and TechSpec relationship; otherwise use `None`. Promotion occurs after QA.
- Architectural or scope deviation requires a human decision unless existing authorization covers it. Record the conflict and return it to the caller; do not invent approval.
- An unavailable environment or pending dependency keeps affected acceptance unmarked; return command/error/impact. Retry fixes only the failure; after two attempts without new evidence, return a block for a decision.
