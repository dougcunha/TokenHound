---
name: sdd-orchestrate-prds
description: Slice SDD when a broad request must become several cohesive PRDs under a shared prefix; for a single PRD, use sdd-create-prd.
argument-hint: --prompt "broad description" [--prefix common-slug]
disable-model-invocation: true
---

# Orchestrate SDD PRDs

A broad request becomes a set of slices: each slice has one primary outcome, its own PRD, and a TechSpec that fits without branching. The user approves the slicing before any PRD exists on disk.

1. **Understand the request and fix the prefix.** Gather objective, users, journeys, surfaces, integrations, constraints and declared out-of-scope. Consult local evidence before public sources and mark each obligation as fact, assumption or product decision. Derive the prefix in kebab-case from the request's domain, or use `--prefix` when supplied. List existing `tasks/prd-[prefix]-*` and reuse the matching slug when the scope coincides.
   **Output:** prefix resolved, obligations inventoried with their origin, and already-written slices identified; ask only for the missing information that blocks the inventory.
2. **Design the slicing.** Group the obligations by cohesion: one primary outcome per slice, minimal coupling between slices, and a scope one TechSpec covers within a single subsystem. Separate independent journeys, distinct surfaces and layers deliverable in different orders; keep together whatever shares a contract and would fail alone. Assign each cross-cutting obligation (authentication, observability, NFR) to one owning slice and cite it as a constraint in the others. Number in dependency order and name each slug `[prefix]-[NN]-[slice]`.
   **Output:** every obligation from step 1 belongs to exactly one slice; each slice has a single primary outcome; dependencies between slices form an order without cycles. A single slice is a valid result when the request has one primary outcome.
3. **Approve the slicing.** Present each slice with slug, outcome, covered obligations and dependencies. Use the available question tool (`AskUserQuestion`) or, without it, a textual question carrying the same options, each naming the slugs it produces: the proposed slicing, a more granular alternative, and a more grouped one. Rely on the automatic "Other" for the user to type whatever split they want, including a different prefix. Remap the obligations onto the chosen split and ask again only when it leaves an obligation without an owner or creates a cycle.
   **Output:** slicing approved in the conversation and remapped onto the inventory; disk untouched until that answer.
4. **Delegate the drafting.** For each approved slice, delegate `sdd-create-prd` with the skill's exact name and path, the final slug, that slice's obligations and evidence, the sibling slugs as boundaries, and the instruction to record cross-slice dependencies under `Constraints and dependencies` and boundaries under `Out of scope`. Send minimal context per agent: the slice's obligations, cited sources and boundaries. Parallelize slices with no contended source; without subagents, draft sequentially through the same skill and report the limitation.
   **Output:** each approved slice has `tasks/prd-[prefix]-[NN]-[slice]/prd.md` written, or a blocker with a concrete cause that stops only that slice.
5. **Check the set and report.** Check coverage against step 1's inventory, unique IDs within each PRD, each functional requirement appearing in a single slice, and dependencies declared on both sides. Return a divergence to the owning slice before reporting. Report paths, the suggested order for `sdd-create-techspec`, and pending items inherited from the PRDs.
   **Output:** reviewable set on disk; obligations without an owner and repeated requirements resolved or explicitly pending.

Read each source once per version. When a request extends an existing prefix, preserve the written PRDs and treat the new scope as additional slices starting from the next free number.
