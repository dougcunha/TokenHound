---
name: sdd-plan-audit
description: SDD audit when an architectural-analysis report must become workstreams with TechSpec and tasks; does not audit or implement.
argument-hint: --report .audits/architectural-analysis-[timestamp].md [--update]
disable-model-invocation: true
---

# Plan SDD audit

An `architectural-analysis` report becomes **workstreams**: each workstream is an SDD feature in `tasks/prd-[slug]/`, executable by `sdd-orchestrate-tasks`. Contracts and tasks are written by the owning skills; this skill slices, delegates, and maintains the **destination map**, which links each finding to the workstream and task that resolves it. The report is immutable and no code is changed.

1. **Fix the report.** Resolve `.audits/architectural-analysis-[timestamp].md` by argument; without one, use the most recent by timestamp and report the choice. An argument that does not resolve while other reports exist: list them and request a choice. With no report at all, stop without writing anything and tell the user to run `architectural-analysis`, which only they invoke. Read the resolved report once. Look for the map `.audits/architectural-analysis-[timestamp].destinations.md`: if it exists, reuse IDs, workstreams, and states, and change it only with `--update`.
   Locate `sdd-plan-refactoring`, `sdd-create-prd`, `sdd-create-techspec`, and `sdd-plan-tasks` in the installed catalog or `SKILLS/`. A missing skill blocks only the route that depends on it.
   **Output:** exact report, previous map reconciled, and each route's skills located; or a stop with no files written and the missing report reported.
2. **Inventory findings.** Walk the five dimensions — dead code, duplication, anti-patterns, types and nullability, smells — by content, not by headings: real reports rename sections, write findings in prose, and record corrected false positives. Every table row, duplication group, or described hotspot gets `AA-NN` in order of appearance, with dimension, `file:line`, severity or confidence, and recommendation. An aggregate count beyond the listed items becomes a single finding for the rule, with the count as baseline; without named files, it enters a workstream only at the user's request and is otherwise deferred. Summary, statistics, impact, "None found", and corrected false positives are context and get no ID.
   Check each finding against the smallest excerpt of current code — the report ages. Classify: **actionable** (confirmed), **resolved** (no longer exists), **discarded** (contrary evidence cited), or **pending** (depends on a decision or environment). For `Possibly dead`, LOW confidence, and use through reflection or configuration, first run the verification the report names; only what it does not decide stays pending.
   **Output:** every finding has an ID, class, and current evidence.
3. **Slice workstreams.** Group actionable findings by cause, with one reviewable outcome per workstream: confirmed dead code; one duplication or contract group; one god object or layer boundary; defects with a concrete failure path (async misuse, captive dependency, swallowed exception); nullability posture per project; local smells per target file. Merge findings the same extraction resolves — `Prioritized Actions` and `Risk Areas` point to that coupling. Order removal before consolidation and consolidation before decomposition; a target file shared between workstreams becomes an explicit dependency. Name them `arch-[YYYYMMDD]-[NN]-[workstream]` with the report date.
   Decide each workstream's route — this is where it is decided whether a TechSpec is needed:

   | Workstream | Route |
   | --- | --- |
   | Preserves observable behavior: removal, consolidation, decomposition, coupling, layers, types, smells | `sdd-plan-refactoring` writes `prd.md` and `techspec.md` |
   | Fixes a defect and changes observable behavior: a failure becomes logged or propagated, synchronous blocking becomes async | `sdd-create-prd`, then `sdd-create-techspec` |
   | `tasks/prd-[slug]/` already has a PRD and TechSpec valid for the scope | Reused: no new contract |

   When writing the map, read [assets/destinations.template.md](assets/destinations.template.md) in full; workstreams enter as `proposed`.
   **Output:** every actionable finding belongs to exactly one workstream; each workstream has a route and dependencies without cycles; map written.
4. **Approve the slicing.** Present each workstream with slug, `AA-NN`, route, dependencies, and risk, and list resolved, discarded, and pending findings separately. Ask which workstreams to plan — through the available question tool (`AskUserQuestion`) when the options fit, in text when they do not — and accept regrouping. A workstream not chosen becomes `deferred` in the map with its findings.
   **Output:** each workstream `approved` or `deferred` by the user's answer; `tasks/` untouched until then.
5. **Generate contracts.** For each approved, non-reused workstream, in dependency order, delegate the route's skill with exact name and path, slug, the workstream's findings (ID, `file:line`, current evidence, recommendation), and sibling workstreams as boundaries. Require `AA-NN` preserved as the obligation ID in the PRD scope and in the TechSpec's `DEC-NN`/`QA-NN` that eliminate it: that is how tasks inherit traceability. On the refactoring route, the finding's counts are the quality profile baseline and the target is to eliminate them. Parallelize workstreams without a shared target file; without subagents, execute sequentially through the same skill and report the limitation.
   Present the written contracts at a single HIL — preserved behaviors, decisions, and pending items for all workstreams — before decomposing; correct whatever is refused through the owning skill.
   **Output:** each approved workstream has accepted `prd.md` and `techspec.md`, or a block with a concrete cause that affects only it.
6. **Plan tasks.** For each workstream with accepted contracts, delegate `sdd-plan-tasks --prd [slug]` with exact name and path. Rules, templates, validation profile, and coverage gate are its own, applied without local adaptation.
   **Output:** each workstream has `tasks.md` and `task_*.md` with the coverage gate filled in, or a block returned by the skill.
7. **Close the map.** Record on each `AA-NN` the workstream and tasks that cover it, read from the traceability matrix in `tasks.md`, or its final destination. Check that every actionable finding of a planned workstream appears in at least one task and that the map's links resolve. Present workstreams, execution order, risks, and pending items; implementation authorization stays with the user.
   **Output:** no finding without a destination in the map; execution indicated as `sdd-orchestrate-tasks --prd [slug]` in map order.

New report on code with a previous map: reconcile findings by file and rule, preserve workstreams in progress, and number new workstreams after the highest existing one. A workstream whose source changes during planning invalidates only its own derivatives.
