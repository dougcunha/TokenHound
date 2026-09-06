# Stable execution context

Load in this exact order:

1. `tasks/prd-[slug]/prd.md`
2. `tasks/prd-[slug]/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# TXX — [outcome-oriented title]

## Outcome

[Observable behavior delivered when the task is complete.]

## Dependencies and boundaries

- Depends on: [IDs or —]
- Unblocks: [IDs or —]
- In scope: [changes in this delivery]
- Out of scope: [relevant boundaries]

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| RF-01 | `prd.md#functional-requirements` | [requirement] |
| CMP-01 | `techspec.md#components-and-flow` | [technical decision] |

## Context to recover on demand

- Applicable skills: [names recorded in the TechSpec]
- Existing code: `[file or module]` — [why it matters]
- Contract or integration: `[TechSpec section]`

## Work

- [ ] TXX.1 [small, verifiable change]
- [ ] TXX.2 [small, verifiable change]

## Acceptance criteria

- [Observable and measurable condition]
- [Error or edge behavior]

## Verification

- Unit: [scenario and expected result]
- Integration: [boundary, environment, and expected result; a double only when it represents the contract]
- E2E: [omitted by .NET desktop policy | relevant flow for another target]
- Manual: [script, expected result, and owner, if required]
- Commands: `[project's standard test command]`
- Environment dependency: [none | prerequisite, existing authorization, or open item]
- Expected evidence: [output, test, metric, or artifact]

## Affected files

- Modify: `[path]`
- Create: `[path, if needed]`

## Observability and recovery

- Operational signal: [log, metric, or health check, if applicable]
- Recovery: [rollback or reversal, if applicable]

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: Pending execution.

### ADR candidates

Pending execution. `sdd-execute-task` replaces this text with structured candidates or `None - direct TechSpec implementation or local decision`.
