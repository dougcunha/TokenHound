---
name: sdd-create-techspec
description: SDD TechSpec when a PRD exists and the solution must be specified; does not create requirements or a task plan.
argument-hint: --prd feature-name [--update]
---

# Create SDD TechSpec

1. Resolve `tasks/prd-[slug]/prd.md` and `techspec.md`. Require the PRD; if missing, point to `sdd-create-prd`. Read the PRD once per version. Reuse an existing TechSpec; update only when authorized, preserving IDs.
   **Output:** exact sources and destination, with no implicit overwrite.
2. Map every PRD obligation to a technical consequence. Inspect only involved modules, callers, contracts, persistence, errors, tests, and configuration. Reuse existing patterns; justify new dependencies and components with a proven gap. Consult primary documentation for external technical questions.
   **Output:** every obligation has a decision or pending item; every component has a path, responsibility, and integration.
3. Identify the stack per affected project. For C#/.NET, read [references/dotnet.md](references/dotnet.md) in full before specifying validation. In desktop .NET, omit E2E execution and record `E2E: omitted by desktop .NET policy`; preserve acceptance with smaller tests and a manual script when needed.
   **Output:** validation profile with evidence of stack, runner, projects, commands, and limitations.
4. Read [assets/techspec.template.md](assets/techspec.template.md) in full when drafting. Use `DEC-01`, `CMP-01`, `TC-01`, and PRD IDs. Cover applicable contracts, errors, edges, security, concurrency, rollback, and rollout without duplicating requirements. Every obligation must have a test or other proportional evidence; remove inapplicable sections.
   **Output:** all obligations covered; unresolved decisions explicitly pending, without imposing a percentage of coverage.
5. Write only the TechSpec. Report decisions, gaps, and tasks invalidated by the update. In the orchestrated flow, return it for plan preparation and joint technical HIL.
   **Output:** reviewable artifact with identified impacts; conflicts between PRD/code/contract that require human decisions were not invented.

Keep stable sources ahead of recovered code and state; reread only changed versions. Reading order helps consistency but does not guarantee provider caching.
