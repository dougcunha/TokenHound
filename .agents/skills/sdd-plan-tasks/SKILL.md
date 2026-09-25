---
name: sdd-plan-tasks
description: SDD tasks when a PRD and TechSpec must be decomposed into an executable DAG; does not implement the feature.
argument-hint: --prd feature-name [--update]
disable-model-invocation: true
---

# Plan SDD tasks

1. Resolve `tasks/prd-[slug]/`. When `context-snapshot.md` exists there, load it through the Load branch of `.agents/skills/sdd-snapshot/SKILL.md` first. Require and read `prd.md` and `techspec.md`, in that order, once per version. Then inventory `tasks.md`, `task_*.md`, and `done/task_*.md`. Reuse an existing plan without overwriting; for an authorized update, preserve IDs, handoffs, and completed tasks.
   **Output:** sources and state reconciled; broken links or conflicting IDs block only the affected update.
2. Extract obligations, decisions, components, risks, and tests in one pass. Preserve IDs; for legacy sources, such as PRDs that use `RF`/`CA`, keep their IDs or assign local IDs with the origin section. Map each item to a delivery and evidence, or to a pending item that changes scope or acceptance.
   **Output:** complete, traceable inventory, including out-of-scope limits.
3. Use vertical slices: one reviewable result, implementation, and tests in the same task. Separate foundation only when it unlocks multiple deliveries or enables independent migration. Model acyclic dependencies and file or contract collisions; number new tasks after the largest ID in the root and `done/`. Tasks run one at a time in a single writing session, so size each one to fit a session with room for exploration, tests, and review, and name in its `Context to recover on demand` the few sources a cold session needs.
   **Output:** every non-pending item has a task; every task has origin, limits, dependencies, and verification.
4. Apply the TechSpec profile. In desktop C#/.NET, omit E2E even locally and record the exclusion; keep relevant unit and integration tests and a manual script for visual acceptance. Cover provider behavior following the relevant spec in `docs/specs/`.
   Prefer local validation that proves behavior. A fake does not prove real provider or service semantics: preserve gaps. Record required environments and existing authorization; if an essential decision is missing, keep the obligation pending and prepare independent tasks.
   **Output:** real commands from `AGENTS.md` and known prerequisites; no obligation disappeared to make tests cheaper.
5. When generating contracts, read [assets/tasks.template.md](assets/tasks.template.md) and [assets/task.template.md](assets/task.template.md) in full. Write the reviewable draft before requesting HIL. Use `tasks.md` as the source of DAG, links, and state; copy only short invariants into tasks and reference TechSpec details.
   **Output:** manifest and tasks exist; links resolve; IDs are unique; no placeholder outside the initial handoff.
6. Check coverage, traceability, DAG, atomicity, commands, environment, and idempotency. Present the plan with risks and pending items. Return to the orchestrator's HIL; in standalone use, obtain approval before implementation only if it is not already authorized, and run the session pause from that reference with `sdd-orchestrate-tasks` as next step.
   **Output:** plan ready for execution in the approved scope, or blocks associated with concrete IDs.

If a source changes during planning, reconcile the inventory and invalidate only affected derivatives. A missing PRD or TechSpec directs to the corresponding creator skill. Mutable state comes after sources; read only the code needed to resolve paths or commands.
