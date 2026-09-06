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
