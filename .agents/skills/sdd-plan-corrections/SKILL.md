---
name: sdd-plan-corrections
description: SDD corrections when a code review must become traceable tasks; does not implement or alter the report.
argument-hint: --prd feature-name --num review-number
disable-model-invocation: true
---

# Plan SDD corrections

1. Fix one `tasks/prd-[slug]/codereview_[num]/` by argument or context. When `tasks/prd-[slug]/context-snapshot.md` exists, apply the load protocol in `.agents/skills/sdd-orchestrate-tasks/references/session-continuity.md` first. Search only for missing values; multiple candidates require a choice. Require and read its `codereview.md` once; preserve it.
   **Output:** exact, readable report, without mixing IDs between reports.
2. Classify every item as actionable, informational, or pending. In `APPROVED`, plan only what was requested; in `APPROVED WITH RESERVATIONS`, improvements require authorized scope; in `REJECTED`, cover violations, incompleteness, and failures. An unknown status allows only explicitly actionable findings.
   Preserve `CR-NN`; in legacy reports without IDs, assign a local ID by order and section. Inventory root and `done/` metadata to reuse tasks; finding identity is report path + ID.
   **Output:** every item assigned; no duplicate finding or invented decision.
3. Check evidence in the smallest necessary code or TechSpec section; send read-only explorers in parallel only when several findings each need a sweep across many files. Group only the same cause with a reviewable result. Model an acyclic DAG, limits, files, and tests; number new tasks after the largest number in the root and `done/`.
   For desktop C#/.NET, omit E2E; preserve the obligation with relevant unit, integration, or manual acceptance. A missing environment or decision becomes an explicit pending item, not a discarded finding.
   **Output:** every actionable finding has a new or existing task, acceptance, and verification; no orphan task.
4. Read [references/TEMPLATE_TASK.md](references/TEMPLATE_TASK.md) in full when writing. Create reviewable `task_[num].md` files with at least two digits in the report folder, without reusing numbers or overwriting existing tasks. Reference sources by ID and section; preserve handoffs. Do not create a correction manifest: dependencies belong in tasks.
   **Output:** contracts on disk, report intact, and no code changes.
5. Check coverage, traceability, DAG, atomicity, commands, and idempotency. Report files, reuse, pending items, and impact. Return to the caller; request execution approval only where scope is not already authorized. In standalone use, run the session pause from that reference with `sdd-execute-corrections` as next step.
   **Output:** concrete plan for HIL or already authorized execution; contradictions linked to affected tasks.

A task in `done/` whose finding persists is an incomplete correction: keep history and return to the caller to create work in the new report. An unreadable report, conflicting numbering, or cause without evidence blocks only the dependent planning.
