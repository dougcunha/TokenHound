# Implementation plan — [feature name]

## Stable sources

- PRD: `tasks/prd-[slug]/prd.md`
- TechSpec: `tasks/prd-[slug]/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | [observable outcome] | — | T02 |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| RF-01 | `prd.md#functional-requirements` | [requirement] | T01 | [verification] |
| TC-01 | `techspec.md#test-approach` | [scenario] | T01 | [test] |

## Tasks

- [T01 — title](task_01.md): [result in one sentence].

## Coverage gate

- Coverage: [pass/fail and gaps]
- Traceability: [pass/fail and gaps]
- Dependencies: [pass/fail and gaps]
- Atomicity: [pass/fail and gaps]
- Executability: [pass/fail and gaps]
- Validation profile: [pass/fail, E2E omitted for .NET desktop, environment, and gaps]
- Idempotency: [pass/fail and gaps]

## Assumptions and open items

- Assumption: [decision needed to interpret the sources]
- Open item: [blocking question, affected tasks, and decision owner]
- Required environment: [scenario, source obligation, prerequisite, and authorization/open item] | None.

## State

- [ ] T01 — pending

## Problems and solutions

- None.
