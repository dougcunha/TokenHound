# Audit destinations — [project]

## Source

- Report: `.audits/architectural-analysis-[timestamp].md` (immutable)
- Code checked at: [resolved commit and pre-existing changes]

## Workstreams

| Order | Slug | Outcome | Findings | Route | Depends on | State |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | [`arch-[YYYYMMDD]-01-[workstream]`](../tasks/prd-arch-[YYYYMMDD]-01-[workstream]/tasks.md) | [reviewable outcome] | AA-01, AA-02 | refactoring / defect / reused | — | proposed / approved / deferred / contracts / planned / blocked |

## Findings

| ID | Dimension | Location | Finding | Class | Current evidence | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| AA-01 | [dead code / duplication / anti-pattern / types and nullability / smell] | `[file:line]` | [summary and recommendation from the report] | actionable / resolved / discarded / pending | [command or excerpt checked] | [`slug` T01, T02 / resolved / discarded / pending / deferred] |

## Pending items

- [AA-NN]: [decision or environment needed, owner, and affected workstream] | None.

## Execution

1. `sdd-orchestrate-tasks --prd [slug of workstream 1]`
2. [next workstream eligible by the Depends on column]
