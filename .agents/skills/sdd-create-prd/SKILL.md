---
name: sdd-create-prd
description: SDD PRD when asked to create or update product requirements; does not define architecture or tasks.
argument-hint: --prompt "feature description" [--update]
disable-model-invocation: true
---

# Create SDD PRD

1. Extract the problem, outcome, and slug from the request. Resolve `tasks/prd-[slug]/prd.md`; if it exists, reuse it without overwriting. Update only when authorized; preserve unchanged IDs.
   **Output:** unambiguous destination and operation; request only missing information that prevents identifying them.
2. Inventory users, journeys, metrics, FR, NFR, constraints, dependencies, accessibility, and out of scope. Consult local evidence before researching public rules or integrations in primary sources. Record origin and distinguish fact, assumption, and product decision; ask only about gaps that change scope or acceptance.
   **Output:** every obligation has evidence or an explicit assumption; blocking decisions are identified.
3. When drafting, read [assets/prd.template.md](assets/prd.template.md) in full. Use stable IDs `FR-01`, `NFR-01`, `US-01`, and observable acceptance. Keep architecture and sequencing in the TechSpec. Record supplied stack constraints without assuming every repository is desktop.
   **Output:** all applicable sections filled; unique IDs; no requirement without acceptance.
4. Write only the PRD and report path, pending items, and obligations added, changed, or removed. Point out derived artifacts to revalidate without editing them. In the orchestrated flow, return the artifact for product HIL; previous authorization applies to the scope it covers.
   **Output:** reviewable PRD on disk; source divergences and conflicting IDs resolved or explicitly pending.

Read each source once per version; use links for extensive evidence. On updates, preserve unchanged sections to reduce diff and context.
