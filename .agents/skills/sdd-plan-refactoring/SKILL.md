---
name: sdd-plan-refactoring
description: SDD refactoring when a structural change must preserve behavior; does not implement or add functionality.
argument-hint: --slug [NN]-refactoring-name [--update]
disable-model-invocation: true
---

# Plan SDD refactoring

1. Fix target, limits, and slug. A new slug is `[NN]-[name]`, numbered after the highest folder under `tasks/`. Resolve `tasks/prd-[slug]/prd.md` and `techspec.md`. When `context-snapshot.md` exists there, apply the load protocol in `.agents/skills/sdd-orchestrate-tasks/references/session-continuity.md` first. Preserve both documents when they exist without update authorization; with it, read current versions and keep IDs.
   **Output:** unambiguous operation without implicit overwrite.
2. Trace inputs, outputs, errors, effects, callers, persistence, integrations, and edges of the target. Use code, tests, logs, fixtures, and contracts as evidence. When the target has many callers or integrations, send read-only explorers in parallel with one question each and verify the lines they cite. Number behaviors `R-01`, `R-02`; distinguish intent, apparent defect, and gap.
   **Output:** every in-scope behavior has an origin and verification, or a concrete question that changes acceptance.
3. Plan characterization only for behavior without protection; prefer semantic assertions. A snapshot test requires a reviewed baseline. Sequence characterization before mutation and reversible slices before irreversible changes.
   When the target includes provider adapters, credential or database readers, rate-limit handling, or HUD windows, read [references/providers-and-hud.md](references/providers-and-hud.md) in full. For WPF views, view models, or bindings, read [references/wpf.md](references/wpf.md) in full. In desktop C#/.NET, omit E2E; record existing unit and integration projects, runner, and commands, plus a manual script for uncovered visual behavior. Record an unavailable environment as pending.
   **Output:** every `R-NN` has a proportional safety net and safe sequence; new behavior is separate from scope.
4. When drafting, read [assets/TEMPLATE_PRD_REFACTOR.md](assets/TEMPLATE_PRD_REFACTOR.md) and [assets/TEMPLATE_TECHSPEC_REFACTOR.md](assets/TEMPLATE_TECHSPEC_REFACTOR.md) in full. Write only the two artifacts: behavior and acceptance in the PRD; decisions, components, tests, and rollback in the TechSpec. Reference IDs without copying requirements.
   Fill in the quality profile with today's measure and the target at the end — in a refactoring the baseline is the target to reduce, and without a number there is no way to prove the structure improved. When this refactoring prepares the terrain for a feature, the target is what makes that change easy, not perfection of the target code.
   **Output:** documents cover every `R-NN`, have no placeholders, include a measured profile and a declared target, and serve `sdd-plan-tasks`.
5. Report paths, risks, gaps, and impacts on derivatives. In standalone use, point to task planning and run the session pause from that reference with `sdd-plan-tasks` as next step; in the orchestrated flow, return to behavior HIL before decomposition.
   **Output:** reviewable contracts, with no code change or silent promotion of decisions.

Read sources once per version and code only in scope. A behavior contradiction or missing critical verification blocks affected items until a human decision; preserve evidence.
