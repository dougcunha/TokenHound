# TechSpec — [feature name]

## Sources and traceability

- PRD: `tasks/prd-[slug]/prd.md`
- Applicable instructions and skills: [names]
- Evidence in existing code: [paths and symbols]

## Solution summary

[Approach and boundaries in up to two paragraphs.]

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | RF-01, RNF-01 | [decision] | [evidence] | [alternatives] |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `[name/path]` | [state] | [function] | [IDs] |

[Describe the flow between components without transcribing code.]

## Contracts and data

[Include only changed contracts. For each one, document fields, types, requiredness, validation, compatibility, and necessary examples. Remove the section when it does not apply.]

## Integrations and interfaces

[Include only affected endpoints, events, UI, files, database, or services. Document applicable input, output, errors, authentication, timeout, and idempotency.]

## Errors, security, and recovery

- Errors and edges: [behavior]
- Authorization and sensitive data: [control, if applicable]
- Concurrency and idempotency: [guarantee, if applicable]
- Rollback or reversal: [procedure]

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| [step] | [IDs or —] | [evidence] |

## Test approach

- Profile: [stack per project and evidence, SDK/TFM, runner, and execution route]
- E2E: [omitted by .NET desktop policy | relevant scenario for another target]
- Command prerequisites and exclusions: [environment, filters/projects without desktop E2E]
- Manual acceptance: [script, expected result, and owner, when needed]

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | RF-01 | [level allowed by profile] | [scenario] | [result] | `[command or script]` |

## Quality profile

Rules this feature can violate. A blocking hit prevents task completion and rejects the review; a reservation becomes an optional improvement and counts toward escalation. A hit covered by `DEC-NN` is expected, not a finding.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | [rule] | blocking/reservation | `[rg command scoped to the diff]` | `DEC-NN` or — |

- Verification scope: [files in the task diff]
- Escalation trigger: [8+ reservations, file above 500 lines, or duplication in 3+ places]

### Terrain baseline

Hits that already existed in the target files before implementation. A hit listed here is not a task finding; a new hit is. A target file without a row in this table counts as unmeasured, and every hit in it will be treated as new.

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `[path]` | [n] | [n] | [n] | [n] | `QA-NN: file:line` | recorded / absorbed in `DEC-NN` / prior refactoring |

- Preparatory refactoring: [not recommended | recommended — minimal scope, reason, and what it makes easy]

## Observability and rollout

- Signals: [applicable logs, metrics, or health checks]
- Migration and compatibility: [strategy, if applicable]
- Rollout and rollback: [steps and gates]

## Risks and open items

- Risk: [probability, impact, and mitigation]
- Open item: [decision, owner, and affected items]

## Relevant files

- Modify: `[path]`
- Create: `[path, if needed]`
