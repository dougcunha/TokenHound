# Code review report — [feature name]

## Summary

- Status: APPROVED / APPROVED WITH CAVEATS / REJECTED
- Git scope: `[base..current state]` or `Not delimited — see limitations`
- Previous review: `[path or —]`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-[slug]/prd.md` | read |
| TechSpec | `tasks/prd-[slug]/techspec.md` | read |
| Manifest | `tasks/prd-[slug]/tasks.md` | read |
| Implementation | `[diff, handoffs, and files]` | [delimited/limited] |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| RF-01 | [requirement] | `[file:symbol]` | `[test]` | conforming/nonconforming/pending/not verifiable | [evidence] |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| [name] | OK/NOT OK/N/A | `[file:line or command]` |

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| [source] | YES/NO/PARTIAL | [evidence] |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE/INCOMPLETE | [summary] |

## Executed validations

- Profile and exclusions: [stack per target; E2E omitted by .NET desktop policy, when applicable]
- Validated state: [code/diff, configuration, projects, and environment]
- Reused evidence: [handoff/report and why it remains valid | none]
- Manual acceptance: [evidence or open item, when essential]

| Command | Result | Obligations covered |
| --- | --- | --- |
| `[command]` | passed/failed/blocked | [IDs] |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Critical/High/Medium/Low | [ID or rule] | `[file:line]` — [fact] | [effect] | [action or `Cause still pending`] |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_[previous]/CR-01 | resolved/persistent/not verifiable | [file/test or limitation] |

## Limitations and open items

- [unavailable evidence, impact on status, and required decision]

## Conclusion

[Opinion derived from the matrix, findings, and validations.]
