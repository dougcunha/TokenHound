---
name: sdd-execute-corrections
description: SDD execution when tasks from a code review need correction; implements each correction task in this session with read-only explorers and moves on between tasks until the context threshold; does not create or reclassify findings.
argument-hint: --prd feature-name --num review-number
disable-model-invocation: true
---

# Execute SDD corrections

The session that runs this skill is the only writer: it implements every correction task itself and owns the moves inside the report folder. Subagents are read-only explorers; they never edit files, run builds or tests that write shared resources, or talk to the user. Execution is one task at a time; between tasks the session moves on by itself until the session pause says stop. The session that corrects the code does not issue the re-review.

If the caller limits execution to one task, return after step 6 instead of running the session pause: `task-completed` with the next eligible IDs, or `blocked` with evidence. When all tasks are complete, run step 7 before returning. A task return does not end the review cycle.

1. **Resume.** Read `.agents/skills/sdd-orchestrate-tasks/references/session-continuity.md` in full once per session: its context measurement applies to every task. If `tasks/prd-[slug]/context-snapshot.md` exists, load it through the Load branch of `.agents/skills/sdd-snapshot/SKILL.md`, validating `covers_through` against the report folder.
   **Output:** snapshot applied, partially trusted with the suspect entries named, or absent.
2. Fix one report folder by argument, snapshot, or context and require its `codereview.md`. Inventory root and `done/` tasks by ID, findings, acceptance, dependencies, and files. If ambiguous, request a choice; if the plan is missing, direct to `sdd-plan-corrections`.
   **Output:** exact report, with no duplicates or missing or circular dependencies. If all tasks are complete, continue to step 7.
3. Select one eligible task whose dependencies are complete, preferring the snapshot's `next_step` when it is still eligible. Confirm correction authorization.
   **Output:** one task with its findings, limits, and matching snapshot entries loaded.
4. Read the stable report and task once per version; then retrieve relevant PRD and TechSpec sections, code, `AGENTS.md`, and skills. Send a read-only explorer only for sweeps across many files, such as every caller of a symbol a finding names, and verify the lines it cites. Trace the cause and implement within the limits. Apply the TechSpec validation; in desktop C#/.NET, omit E2E even in legacy commands. Record unit, integration, and required manual checks and limitations; when available, use `dotnet-efficient-validation`. Run the quality profile's blocking commands over the files the correction touched: fixing one finding without introducing another is part of the task's acceptance. Serialize builds and test runs that share `bin/`, `obj/`, or `TestResults/`.
   Update only the task and its single `## Handoff` with result, files, commands, validated version, and pending items. Preserve the report, other tasks, and global state.
   **Output:** implementation and evidence, or a reproducible block.
5. Re-read the diff and handoff against the finding, acceptance, and rules as if another author wrote them. Reuse proven tests from the same state; run only missing or invalidated checks. Fix what fails; after two attempts without new evidence, record a block and advance independent work. An architecture or scope divergence requires HIL when not covered by existing authorization.
   **Output:** acceptance of the task proven, or a specific pending item; unexecuted essential manual work prevents approval.
6. Move an approved task to `done/`, preserving its name and checking absolute paths inside the report folder. Preserve the immutable report. Recalculate the DAG from the remaining files. Then, with no explorer or process running, run the session pause from `session-continuity.md` with stage `corrections`, `authored_code: yes`, and the next eligible task as next step; on the `Move on` destination, return to step 3.
   **Output:** approved task moved, pending tasks at the root; the next task started, or the snapshot written before the question and the user's choice applied. On interruption, check report and handoff before inferring completion from the folder.
7. Validate the integrated set without repeating already valid commands. Check every actionable finding and its evidence. The re-review runs in a session that did not make these corrections: in the orchestrated flow, return the execution report so the caller schedules `sdd-review-code`; in standalone use, run the session pause with `sdd-review-code` as next step, which recommends ending this session.
   **Output:** tasks complete with integrated evidence and the re-review left to an independent session or explicitly returned to the caller; persistent or new findings remain open until a decision.

If the environment prevents acceptance, keep the affected task pending with command, error, and impact. Pre-existing or foreign changes that collide with the task's files stop the task until they are reconciled.
