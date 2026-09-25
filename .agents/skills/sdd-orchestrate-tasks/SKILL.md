---
name: sdd-orchestrate-tasks
description: SDD DAG when PRD, TechSpec, and tasks are already approved and must be executed; implements each task in this session with read-only explorers and moves on between tasks until the context threshold. For the cycle from PRD, use sdd-orchestrate-flow.
argument-hint: --prd feature-name [--budget economical|medium|high]
disable-model-invocation: true
---

# Orchestrate SDD tasks

The session that runs this skill is the only writer: it implements every task itself, following `sdd-execute-task`, and owns the manifest and moves. Subagents are read-only explorers; they never edit files, run builds or tests that write `bin/`, `obj/`, or `TestResults/`, or talk to the user. Execution is one task at a time; between tasks the session moves on by itself until the session pause says stop. This session does not issue the global review of the code it wrote.

If the caller limits execution to one task, return after step 7 instead of running the session pause: `task-completed` with the next eligible IDs, or `blocked` with evidence. When all tasks are complete, run step 8 before returning. A task return does not mean the feature is complete.

1. **Resume.** Read [references/session-continuity.md](references/session-continuity.md) in full once per session: its context measurement applies to every task. Look for `tasks/prd-[slug]/context-snapshot.md`; if it exists, apply its load protocol before anything else: validate the header against the manifest and Git, load the `now` tier, and keep the other tiers for their triggers. A stale or invalid snapshot is a hint, never an authority.
   **Output:** snapshot applied, partially trusted with the suspect entries named, or absent.
2. **Reconcile.** Resolve the feature and check `prd.md`, `techspec.md`, `tasks.md`, the root, and `done/`. Read sources once per version, then task metadata. Confirm authorization to implement and record pre-existing changes so they are preserved.
   If there is evidence of an incorrect completion, reopen the task: record reason, review, and previous handoff under `Problems and solutions`, move it from `done/` to the root, and update link and state to pending. Preserve contract and IDs; revalidate affected dependents. This does not authorize regenerating completed tasks to change their scope.
   **Output:** IDs, links, states, and dependencies reconciled; divergences have evidence and block the affected unit.
3. **Select.** Pick one pending task whose dependencies are complete, preferring the snapshot's `next_step` when it is still eligible. Read `.agents/skills/sdd-execute-task/SKILL.md` once per session and follow it as the procedure for this task.
   **Output:** one task with its contract, write scope, and the snapshot entries that match it loaded.
4. **Explore.** Before editing, list the questions the change needs answered (callers, affected tests, provider behavior in the relevant `docs/specs/` spec, Terrain baseline hits, similar implementations). Answer small, targeted ones directly. Delegate a question to a read-only explorer subagent only when answering it means sweeping many files or directories and only the conclusion matters; run disjoint questions in parallel. Give each explorer the exact question, the paths to start from, the read-only constraint, and the return format: conclusion, `path:line` evidence, and what it could not confirm. Do not send the conversation, the PRD, or the TechSpec unless the question is about them.
   **Output:** change points and test targets known with evidence; explorer conclusions that later work relies on are verified at the cited lines before editing.
5. **Implement.** Apply steps 2 to 5 of `sdd-execute-task` in this session: smallest coherent change, behavior tests proportional to risk, the TechSpec quality profile over the touched files, and the `## Handoff` updated with evidence. Serialize builds and test runs that share `bin/`, `obj/`, or `TestResults/`; when available, use `dotnet-efficient-validation`. Scope and architecture deviations stop the task and go to the user as a decision with alternatives and impact.
   **Output:** implementation limited to the contract, with checks run on the current state.
6. **Review.** Re-read the task diff against the contract, acceptance criteria, `AGENTS.md` and the TechSpec quality profile, as if another author wrote it. Reuse validation proven in the same state; rerun only for a change, failure, uncovered risk, or project requirement. Essential manual validation still pending prevents completion.
   Run the profile's blocking commands over the task diff: it is a sweep whose cost is proportional to hits, not an audit. Discount what the Terrain baseline already recorded. A new or aggravated hit without a `DEC-NN` covering it prevents completion even with conformant acceptance and tests. Accumulate reservation hits per feature for the escalation trigger, without treating them as blocks. With budget `high` or a real risk, add one read-only explorer that checks the diff against the acceptance criteria and returns gaps with evidence; its findings are input, not approval.
   Fix findings and review again. After two attempts without progress, record a block and move on to independent work.
   **Output:** the task approved by evidence or pending with a concrete cause; no task completed with an unjustified blocking hit.
7. **Record and move on.** Move an approved task to `done/`, checking that resolved source and destination stay inside the feature. Update link and state in the manifest and check both. Record relevant problems and solutions at the end of the manifest. Recalculate the DAG. If interrupted between move and update, reconcile using handoff and review without assuming approval from location.
   Then wait for background processes and explorers to finish and run the session pause from [references/session-continuity.md](references/session-continuity.md) with stage `tasks`, `authored_code: yes`, and the next eligible task as next step; on the `Move on` destination, return to step 3. A blocked task stays recorded and execution moves on to the next eligible one; with none eligible, go to step 8.
   **Output:** file, link, and state consistent; the next task started, or the snapshot written before the question and the user's choice applied.
8. **Close.** When none remain eligible, check every obligation and integrated-set validation, serializing shared builds and test runs. Report task completion or blocks; global review belongs to `sdd-review-code`, in a session that did not write this code. Run the session pause with `sdd-review-code` as next step, which recommends ending this session.
   **Output:** all complete with valid integrated evidence, or pending items enumerated; no feature is claimed approved only because tasks moved.

Under `sdd-orchestrate-flow`, the caller runs the session pause and prints its own resume command.

## Budget

Preserve the inherited model by default. `economical`: explorers only for sweeps the session cannot answer in a few searches; `medium`: parallel explorers for disjoint questions; `high`: also the read-only diff check in step 6. Change model or capability only when authorized and available. Budget reduces redundant work, never acceptance criteria.
