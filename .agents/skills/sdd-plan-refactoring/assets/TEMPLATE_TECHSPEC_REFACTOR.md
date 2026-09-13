# TechSpec — Refactoring [target]

## Sources and traceability

- PRD: `prd.md`
- Current code and tests: [paths]
- Applicable instructions and skills: [names]

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-01 | [decision] | [evidence] | [alternatives] |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `[name/path]` | [responsibility] | [change] | [risk/IDs] |

## Safety net

- Profile: [stack per project and evidence, SDK/TFM, runner, and existing commands]
- E2E: [omitted by .NET desktop policy | relevant scenario for another target]
- Command prerequisites and exclusions: [environment and projects/filters without desktop E2E]
- Manual acceptance: [script, expected result, and owner, when needed]

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-01 | [level allowed by profile] | [scenario] | [expected result] | `[verification]` |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| [step] | [IDs or —] | [action] | [gate] | [procedure] |

## Compatibility and rollout

- Preserved contracts: [IDs]
- Migration or coexistence: [if applicable]
- Observability: [regression signal]
- Rollback: [procedure]

## Quality profile

Rules this refactoring must satisfy at the end. Here the baseline is the target to reduce, not debt to tolerate: a pre-existing hit the refactoring sets out to eliminate is an obligation, and a new hit is a regression in any class.

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | [rule] | blocking/reservation | `[rg command scoped to the target]` | [n hits today] | [n hits at the end, or zero] |

- Target measures today: [lines, public members, constructor deps, cases]
- Expected measures at the end: [the same, after the refactoring]

## Risks and open items

- Risk: [probability, impact, and mitigation]
- Open item: [decision and affected items]
