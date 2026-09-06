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

## Risks and open items

- Risk: [probability, impact, and mitigation]
- Open item: [decision and affected items]
