# Stable execution context

Load in this exact order:

1. `tasks/prd-[slug]/codereview_[num]/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# TXX — [outcome-oriented title]

## Outcome

[Observable behavior corrected when the task is complete.]

## Dependencies and boundaries

- Depends on: [IDs or —]
- Unblocks: [IDs or —]
- In scope: [changes in this correction]
- Out of scope: [relevant boundaries]

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_[num]/CR-01 | `codereview.md#[section]` | [nonconformity] |

## Requirements

- [Behavior expected by the review or TechSpec]
- [Rule and error or edge condition]

## Context to recover on demand

- TechSpec: `[section, if applicable]`
- Rules/skills: [names]
- Code: `[file or symbol]` — [relevance]

## Work

- [ ] TXX.1 [small, verifiable change]
- [ ] TXX.2 [small, verifiable change]

## Acceptance criteria

- [Observable and measurable condition]
- [Nonconformity removed without regression]

## Verification

- Unit: [scenario and result, if applicable]
- Integration: [boundary and result, if applicable]
- E2E: [omitted by .NET desktop policy | relevant flow for another target]
- Manual: [script, expected result, and owner, if required]
- Environment dependency: [none | prerequisite, existing authorization, or open item]
- Commands: `[real command]`
- Expected evidence: [output, test, metric, or artifact]

## Affected files

- Modify: `[path]`
- Create: `[path, if needed]`

## Observability and recovery

- Operational signal: [log, metric, or health check, if applicable]
- Recovery: [rollback or reversal, if applicable]

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: Pending execution.
