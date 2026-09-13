---
name: sdd-orchestrate-tasks
description: SDD DAG when PRD, TechSpec, and tasks are already approved and must be executed; for the cycle from PRD, use sdd-orchestrate-flow.
argument-hint: --prd feature-name [--budget economical|medium|high]
disable-model-invocation: true
---

# Orchestrate SDD tasks

If the caller limits execution to a batch, return after step 5 before starting another batch: `batch-completed` with pending IDs if the batch was approved, or `blocked` with evidence if the batch still has a failure or no task is eligible. When all tasks are complete, execute step 6 before returning. A batch return does not mean the feature is complete.

1. Resolve the feature and check `prd.md`, `techspec.md`, `tasks.md`, root, and `done/`. Read sources once per version, then task metadata. Confirm authorization to implement and preserve pre-existing changes.
   If there is evidence of an incorrect completion, reopen the task through the DAG owner: record reason, review, and previous handoff under `Problems and solutions`, move it from `done/` to root, and update link/state to pending. Preserve contract and IDs; revalidate affected dependents. This does not authorize regenerating completed tasks to change their scope.
   **Output:** IDs, links, states, and dependencies reconciled; divergences have evidence and block the affected unit.
2. Select pending tasks with completed dependencies. Use one executor subagent per task; parallelize only units without collisions in files, contracts, configuration, or build/test resources. Reserve available slots and keep the orchestrator as the manifest's sole writer.
   **Output:** batch with exclusive owners and scopes. Without available subagents, report the limitation and execute sequentially if the caller allows it.
3. Delegate `sdd-execute-task` with skill path, task, sources, authorization, and write limits. Use minimal context without copying history. Require a handoff with evidence and wait for the executor's real handle.
   **Output:** each task returns a result or block; the executor does not move files or alter the manifest.
4. Review diff and handoff independently of the author against contract, tests, and the TechSpec quality profile. Reuse validation proven in the same state; rerun only for a change, failure, uncovered risk, or project requirement. In desktop .NET, omit E2E and check replacement evidence; essential manual validation pending prevents completion.
   Run the profile's blocking commands over the task diff: it is a sweep whose cost is proportional to hits, not an audit. Discount what the Terrain baseline already recorded; charging prior debt to whoever touched the file last produces a finding the executor cannot resolve within the contract. A new or aggravated hit without a `DEC-NN` covering it prevents completion even with conformant acceptance and tests. Accumulate reservation hits per feature for the escalation trigger, without treating them as blocks.
   Return findings only to the same executor. After two attempts without progress, record a block and continue independent work. Scope/architecture deviations return to the caller's HIL.
   **Output:** each task approved by evidence or pending with a concrete cause; no task completed with an unjustified blocking hit.
5. Move only approved tasks to `done/`, checking that resolved source/destination stay inside the feature. Update link and state in the manifest and check both. Record relevant problems/solutions at the end of the manifest. Recalculate the DAG.
   **Output:** file, link, and state consistent; if interrupted between move and update, reconcile using handoff and review without assuming approval from location.
6. When none remain eligible, check every obligation and integrated-set validation. Serialize builds/tests to avoid `bin/` and `obj/` contention. Report task completion or blocks to the caller; global review belongs to `sdd-review-code`.
   **Output:** all complete with valid integrated evidence, or pending items enumerated; no feature is claimed approved only because tasks moved.

## Budget

Preserve the inherited model by default. `economical`: minimum sufficient context and concurrency; `medium`: useful independent parallelism; `high`: additional review only for real risk. Change model/capability only when authorized and available. Budget reduces redundant work, never acceptance criteria.
