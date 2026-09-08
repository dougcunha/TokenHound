# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T01: Establish real scope and report contract evidence

## Outcome

A sanitized contract dossier establishes which personal, organization, and enterprise scope mappings can be activated, and records whether allowance/reset evidence exists. Missing access is recorded as a concrete evidence block, never converted into a synthetic success.

## Dependencies and boundaries

- Depends on: None.
- Unblocks: T04 for proven ownership paths; T05 for report contracts and resource limits; T07 for real-response acceptance.
- In scope: read-only official documentation and already-authorized account response inspection; exact JSON paths, token capability requirements, period/coverage semantics, signed download behavior, and stream bounds.
- Out of scope: credential provisioning, login, permission changes, billing writes, new selection UI, production implementation, and inferred allowance values.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-02, FR-03, FR-04, FR-07, FR-09, FR-12; NFR-01, NFR-04 | PRD functional/non-functional requirements | Verifiable owner, period, allowance, access, and safe evidence. |
| DEC-02, DEC-03, DEC-04, DEC-07; CMP-03/04/05 | TechSpec decisions/components | Concrete mappings and integration prerequisites. |
| TC-02, TC-03, TC-04, TC-05; OI-01/02/03 | TechSpec test matrix/open items | Real contract evidence beyond fake HTTP tests. |

## Context to recover on demand

- Skills: repository-cli-efficiency; primary API sources linked in the TechSpec.
- Existing code: CopilotQuotaResponse, CopilotCredentialDiscovery, CopilotConfigReader, and CopilotApiClient under `src/TokenHound.Infrastructure/Providers/Copilot/`.
- Integration: TechSpec Contracts and data; Integrations and interfaces; Risks and open items.

## Work

- [x] T01.1 Record available authorization and evidence sources without printing credentials. Reuse borrowed credentials only for an already-authorized account; request no new scopes or login.
- [x] T01.2 Record exact principal and billing-owner evidence for each billing scope. Organization scope is verified via `GET /orgs/{owner}/copilot/billing/seats` (principal confirmed in `seats[].assignee.login` with `plan_type=business`) and corroborated by local Copilot CLI session workspaces in `~/.copilot/session-state`. OI-01 is closed for Organization scope. Personal (404) and Enterprise remain inactive.
- [x] T01.3 Inspect matching billing and seat responses; record response owner, timePeriod, product/SKU/unit, dimension semantics, and source timestamp/reset limitations.
- [x] T01.4 Search actual documented response fields for an authoritative allowance. No such field was found, so OI-02 is closed for the current contract as consumption-only; no production mapping was created from an illustrative fixture.
- [x] T01.5 Establish daily report completeness, partition identity/revision semantics, signed hosts, and permitted tokens. Observed organization report manifest and signed download established NDJSON format, single-day route, and schema; OI-03 remains open specifically for T05 multi-pass/stream guard calibration.
- [x] T01.6 Write sanitized evidence and permitted fixture excerpts where safe; reconcile only the affected TechSpec mappings/open items, then record affected plan gates. Dossier and TechSpec reconciled.

## Acceptance criteria

- Each scope has either an actionable mapping with official/real evidence or a named blocking evidence item. T01 remains blocked for any indispensable unresolved mapping; partial progress remains documented.
- No secret, signed URL query, raw user roster, or private account value unnecessary for contract validation is committed.
- Absence of an allowance is a valid finding and does not block usage-only delivery.
- Missing real report access cannot be closed by fake HTTP fixtures. No personal report endpoint is invented.

## Verification

- Unit: not applicable to an evidence-only delivery.
- Integration: inspect authorized real responses for the identified scope and date; store sanitized shape and evidence provenance.
- E2E: omitted by .NET desktop policy.
- Manual: engineer cross-checks each exact JSON mapping against its official source and captured response.
- Commands: scoped `rtk git diff --check -- tasks/prd-copilot-ai-credits/evidence tasks/prd-copilot-ai-credits/techspec.md tests/TokenHound.Infrastructure.Tests/Providers/Copilot/Fixtures`; inspect only sanitized files. No .NET build/test is required until executable code changes.
- Environment dependency: GitHub account access and reporting policy are OI-01/OI-03. This planning request does not itself authorize credential changes or external writes. Continue independent tasks if access is absent.
- Expected evidence: `evidence/copilot-contracts.md`, sanitized fixtures where permitted, exact OI statuses, and no fabricated successful account validation.

## Affected files

- Create: `tasks/prd-copilot-ai-credits/evidence/copilot-contracts.md`.
- Create as evidence permits: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/Fixtures/` sanitized JSON/NDJSON.
- Modify: only evidence/mapping/open-item portions of `tasks/prd-copilot-ai-credits/techspec.md`; update gate state in `tasks.md` with affected IDs.

## Observability and recovery

- Signal: evidence ledger distinguishes verified, unavailable, and incompatible contracts.
- Recovery: remove only incorrectly captured material through a reviewed correction; never alter credentials or billing state. Revalidate dependent mappings if a source changes.

## Handoff

- Produced result: Complete T01 contract evidence delivery for Organization scope. The sanitized dossier establishes official seat-assignment ownership proof via `GET /orgs/{owner}/copilot/billing/seats`, corroborated by live Copilot CLI session workspaces in `%USERPROFILE%\.copilot\session-state\`. Organization billing returns HTTP 200 with 7 monthly usage items. OI-01 is closed for Organization scope, unblocking T04 direct organization billing integration. OI-02 is closed as consumption-only. Personal (404) and Enterprise remain inactive. OI-03 remains open for T05 historical report fallback calibration.
- Changed files: `tasks/prd-copilot-ai-credits/evidence/copilot-contracts.md`, `tasks/prd-copilot-ai-credits/techspec.md`, `tasks/prd-copilot-ai-credits/task_01.md`, and `tasks/prd-copilot-ai-credits/tasks.md`.
- Checks: `rtk git diff --check -- tasks/prd-copilot-ai-credits/` passed without errors or warnings.
- Validated state: Sanitized responses only; zero credentials, secrets, tokens, or signed URLs committed.
- Open items: OI-01 closed for Organization scope; OI-02 closed as consumption-only; OI-03 remains attached to T05. T04 is now unblocked.

### ADR candidates

None - direct evidence reconciliation; no durable contract, boundary, or quality-attribute decision was introduced.
