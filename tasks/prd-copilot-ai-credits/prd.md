# PRD: Copilot AI credits

Feature slug: `copilot-ai-credits`
Status: Draft for product review
Operation: Create a separate feature PRD from the supplied adjustment plan.

## Problem and context

TokenHound users need to understand Copilot AI-credit consumption without confusing it with the operational premium-interaction quota. The supplied [adjustment plan](../../docs/COPILOT-AI-CREDITS-PLAN.md) identifies two risks: treating internal quota fields as documented billing credits, and deriving a credit allowance from published plan prices, seat counts, or promotions that are not an authoritative response for the account and billing period.

This feature adds billing information for personal, organization-managed, and enterprise-managed Copilot accounts. It preserves the existing premium-interaction metric and distinguishes personal consumption from a shared billing pool. Missing permission, incomplete reports, and absent allowance data must remain visible limitations rather than appear as zero consumption or an exhausted allowance.

The plan is the product scope authority for this addition. Existing code behavior described by the plan is source-reported context, not a claim of runtime verification during PRD preparation. This PRD specifies observable behavior; architecture, DTOs, endpoint wiring, sequencing, and implementation tasks belong in the TechSpec.

## Outcomes and metrics

These are release acceptance measures, not adoption targets or a requirement for new telemetry.

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Users distinguish AI credits from premium interactions. | Every supported Copilot presentation scenario uses separate labels and values; no internal quota fixture populates billing-credit fields. |
| OBJ-02 | Displayed consumption belongs to the correct billing owner and period. | Personal, organization, enterprise, ambiguous-owner, and period-mismatch acceptance cases satisfy FR-02 through FR-08. |
| OBJ-03 | Users see a balance only when its inputs justify it. | Missing allowance, seat-only, policy-only, and incompatible-period cases all leave total, remaining, and calculated usage fraction unavailable. |
| OBJ-04 | Failures preserve useful information without implying freshness. | Independent-source failure, partial-report, period rollover, and rate-limit cases satisfy FR-09 through FR-12 and NFR-02. |
| OBJ-05 | The HUD remains usable and other providers retain correct window semantics. | Presentation checks cover Copilot credit/quota rows, existing session/weekly rows, focus retention, and unavailable states. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer with a personally purchased Copilot plan | See personal credit consumption. | Understand usage attributable to the personal account. | Resolve personal billing, show available usage and period, and leave an unreported allowance unavailable. |
| US-02 | Developer with an organization-managed license and sufficient access | See the organization's shared consumption. | Understand that the figure belongs to the shared pool. | Resolve billing ownership, display its identifier, and show a compatible balance only when the allowance is verified. |
| US-03 | Developer with an enterprise-managed license and sufficient access | See consumption for the enterprise billing scope. | Avoid mistaking an organization subset for the enterprise pool. | Use the verified enterprise scope; identify any narrowed coverage and avoid combining it with an incompatible total. |
| US-04 | Developer without billing access or with ambiguous membership | Understand why credits are unavailable. | Continue using valid premium-interaction data without misleading billing values. | Display a specific access or scope limitation; do not guess the owner or replace missing usage with zero. |
| US-05 | Developer with delayed reports or a temporary outage | See the last valid reading and its age. | Recognize the limits of historical consumption. | Show report coverage, source, and freshness; distinguish partial or stale data from current billing data. |
| US-06 | Developer monitoring several providers | Read each provider's actual metric names. | Avoid interpreting monthly credits as a five-hour or weekly limit. | Hover Copilot for separate billing/quota rows; other providers retain their actual session and weekly rows. |

## Functional requirements

Origins below refer to the sources and product decisions listed under Assumptions and sources. IDs are scoped to this PRD.

| ID | Requirement | Acceptance criterion | Origin |
| --- | --- | --- | --- |
| FR-01 | Present AI credits and premium interactions as independent metric families. | Internal `entitlement`, `remaining`, and `credits_used` never populate AI-credit usage or allowance. An internal fixture with entitlement 20,000 and credits used 725 alone produces no billing-credit value. Existing valid operational quota remains available under its own label. | S1 |
| FR-02 | Resolve billing ownership before selecting billing data; keep plan classification separate from scope. | Cases distinguish personal, organization-managed, enterprise-managed, and unknown scope. Organization membership alone does not establish billing ownership. Unknown plan names remain unknown and do not imply an allowance or personal/managed conversion. | S1; S4 |
| FR-03 | Use one verified billing owner and explicit coverage for a displayed aggregate. | Multiple candidate organizations are not automatically summed or selected. A verified explicit enterprise billing scope takes precedence. Unresolved ownership yields unavailable billing values. A filtered report identifies its coverage and cannot masquerade as the full shared pool. | S1 |
| FR-04 | Prefer compatible billing-period usage over historical metrics. | A valid billing aggregate for the selected owner, unit, and period is used in preference to per-user reports. The result exposes owner, period start/end when known, source, reporting timestamp, and any narrowing of coverage. Unknown metadata stays unavailable and prevents dependent calculations. | S1 |
| FR-05 | Preserve gross consumption, discounted/included coverage, and net/additional consumption independently. | Compatible Copilot AI-credit items across models are aggregated once per dimension with decimal precision. Other products or units are excluded. Gross, discount, and net are never added together; discount quantity is never treated as the included allowance. A dimension absent from the response does not become zero. Labels follow verified source semantics. | S1 |
| FR-06 | Support historical per-user credit reports when compatible billing usage is unavailable and reports are accessible. | Daily `ai_credits_used` rows from the billing-period start through the newest available day are summed without duplicate or overlapping coverage. Missing days/users or unavailable earlier data are identified as partial coverage, not zero. A 28-day report is not treated as a complete billing month unless coverage actually matches. Historical totals are visibly identified as consumption reports, not invoice totals or live balances. | S1 |
| FR-07 | Accept an included allowance only from a verified authoritative response for the applicable owner and period. | Before enabling an allowance, evidence records the response field path, unit, scope, period, official semantics, and behavior relevant to promotions and seat changes. Usage-only, seat-only, policy-only, or undocumented-field responses leave total unavailable. No standard or promotional amount or date supplies a production denominator. | S1 |
| FR-08 | Derive remaining credits only from a verified allowance and compatible gross consumption. | Remaining equals included total minus compatible gross usage only when scope, unit, period, coverage, and allowance provenance agree. Otherwise remaining and calculated fraction are unavailable. A zero denominator yields no calculated fraction. Over-allowance usage remains visible without clamping away consumption; discounted coverage and additional usage remain separate. Historical estimates, if inputs are otherwise compatible and complete, retain their estimated/derived qualification. | S1; D1 |
| FR-09 | Keep period, reset, provenance, coverage, and freshness attached to billing values. | Directly reported usage, derived remaining, historical estimates, partial coverage, stale values, and unavailable fields are distinguishable. Source reporting time is not replaced by fetch time. A reset is shown only when supported by verified billing-period semantics, never borrowed from the internal quota. | S1 |
| FR-10 | Refresh quota and billing independently and retain the last valid billing reading with its context. | Billing failure does not erase a successful quota reading, and quota failure does not erase successful billing. After a billing failure, an existing valid billing reading is marked stale with its original source, owner, and period; without one, the reading is unavailable. | S1 |
| FR-11 | Prevent cached values from crossing account, scope, or period boundaries. | Changing billing owner does not show the previous owner's reading as the new owner's value. On period rollover, an old reading may appear only as explicitly stale prior-period data and cannot participate in current-period calculations. | D2, derived from S1 compatibility and caching rules |
| FR-12 | Explain missing billing access and retrieval failures without inventing usage. | Authorization/policy denial, unresolved scope, unavailable reports, network failure, rate limiting, and incompatible schema/data produce distinguishable explanations where the cause is known. A billing-only denial does not claim the Copilot license was revoked or invalidate a working quota source. Credentials are not silently substituted between personal and managed scopes. | S1 |
| FR-13 | Render provider rows according to actual metric semantics. | Copilot details contain separate `AI credits` and `Premium interactions` sections. Billing displays available used, total, remaining, discounted/included, additional, reset, scope, and freshness information. Missing values use an unavailable state, never `0 / 0`. Monthly credits never occupy a row labeled as a five-hour or weekly limit; other providers keep genuine session/weekly rows. | S1; S3 |

## Non-functional requirements

| ID | Attribute | Limit or criterion | Origin |
| --- | --- | --- | --- |
| NFR-01 | Credential integrity and privacy | Credential discovery remains read-only and follows existing approved sources. No token write, refresh, replacement, or new login flow occurs. Borrowed files allow concurrent owner access with `FileShare.ReadWrite | FileShare.Delete`. Verification artifacts contain no live tokens or signed download secrets. | S1; S2; repository instructions |
| NFR-02 | Rate-limit compliance | Every billing request and report download respects the existing persisted 429 deadline, including forced refresh and restart. `Retry-After: 0` never triggers an immediate retry. Acceptance exercises the existing deadline/backoff policy instead of introducing a separate retry policy. | S1; repository instructions |
| NFR-03 | Responsiveness and cancellation | Billing retrieval and report downloads support cancellation and bounded timeouts, following existing scheduling conventions. A cancelled, slow, or failed billing operation does not block HUD input or discard an independently successful quota result. Exact transport budgets belong in the TechSpec. | S1 |
| NFR-04 | Data robustness | Acceptance fixtures cover absent fields, decimals, unknown plans, extra response fields, signed reports where applicable, mixed units, duplicate report coverage, partial periods, promotions, seat changes, and overage. Invalid or incompatible data cannot produce a valid-looking zero or balance. | S1 |
| NFR-05 | HUD accessibility and behavior | Metric names, unavailable states, scope, and freshness are readable as text rather than communicated only by color. New rows remain within the supported HUD/tooltip geometry and do not obscure values. Hover, click, and drag preserve the active application's focus and existing interaction behavior. | S1; S3; repository instructions; D3 |
| NFR-06 | Architectural and provider compatibility | Core retains zero UI/OS dependencies. Existing Copilot operational quota, local activity, and other providers retain their established semantics. Regression evidence covers quota selection/reset/overage plus existing session/weekly presentation. | S1; S2; repository instructions |

## User experience

The developer opens the existing Copilot details through the HUD. `AI credits` identifies personal usage or the named shared organization/enterprise scope. It shows reported consumption even when the allowance is unavailable. Discounted/included usage and additional usage appear as separate quantities when their semantics are known. `Premium interactions` retains its operational quota values and independent reset.

A verified fixture with an authoritative included total of 5,700 and compatible gross usage of 725 can display `4,975 / 5,700 remaining` and `725 credits used`. These are acceptance example inputs, never defaults or plan policy. With usage alone, show `725 credits used` and unavailable total/remaining. With no successful reading, show unavailable values and the known reason. An initial refresh may show loading; a refresh with prior data retains that data and its freshness indication.

Historical or incomplete reports state their covered dates and estimated/partial status. Stale values retain their original source and period. A missing reset is unavailable. Existing reset formatting and HUD interaction conventions apply. Layout dimensions, animation changes, and bindings are deferred to the TechSpec and design review.

## Constraints and dependencies

- Preserve the existing C#/.NET Windows HUD product and repository invariants. This feature introduces no required SDK runtime or platform migration.
- Access depends on the selected billing owner's permissions, token authorization, and organization/enterprise reporting policy. A borrowed token usable for internal quota is not assumed to authorize billing or metrics.
- The plan specifies documented GitHub requests using API version `2026-03-10`, the GitHub JSON media type, UTC period filters, existing User-Agent conventions, and read-only access. The TechSpec must validate endpoint-specific support, permissions, and direct versus signed-report contracts.
- Allowance discovery is a conditional dependency for displaying a balance, not a prerequisite for shipping honest consumption-only behavior. Actual authenticated response evidence was not collected for this PRD.
- Seat changes and promotions must be represented by authoritative effective-period values. The plan's seat/promotion policy observations do not authorize a seat-times-plan calculation.
- No new polling interval or performance target is invented here. Existing provider cadence and backoff govern the addition; report cost and bounded request budgets require technical validation.

## Out of scope

- Hard-coded allowances, promotional calendars, seat-derived pools, or conversion of internal quotas into billing credits.
- Billing writes, payment or license administration, token refresh, credential provisioning, and a new sign-in flow.
- Automatic cross-organization pooling, speculative billing ownership, and a new account/scope selection interface.
- Invoice reconciliation, currency cost estimates, predictive usage, and real-time balance claims for historical reports.
- New activity detection, changes to other providers' quota semantics, and a general HUD redesign.
- Implementing transport, selecting model types, rewriting existing specifications, creating a TechSpec, or planning implementation tasks in this artifact.

## Assumptions and sources

### Evidence and origin

- **S1, supplied product source:** [Copilot AI Credits: Adjustment Plan](../../docs/COPILOT-AI-CREDITS-PLAN.md). Its purpose, source-of-truth rules, target model, implementation phases, test plan, and acceptance criteria establish the feature obligations. Technical suggestions remain inputs to the future TechSpec.
- **S2, existing repository specification:** [Copilot provider specification](../../docs/specs/11-PROVIDER-COPILOT.md). Establishes operational quota, borrowed credentials, activity, and status/cadence context. Its billing-fidelity language conflicts with S1 as described below.
- **S3, existing design source:** [Usage Notch design, hover state and interaction rules](../../docs/design/2026-08-28-usage-notch-design.md). Describes one block per actual provider window and the existing tooltip interaction.
- **S4, official API documentation checked on 2026-09-07:** [GitHub billing usage](https://docs.github.com/en/rest/billing/usage?apiVersion=2026-03-10) distinguishes personally purchased usage from organization/enterprise-billed usage. Its organization AI-credit example contains consumption dimensions and period information, but does not establish an included-pool allowance. [Copilot usage metrics REST API](https://docs.github.com/en/rest/copilot/copilot-usage-metrics) provides the report integration reference. Endpoint details remain subject to implementation-time validation.
- **Repository instructions supplied by the user:** establish English repository text, pure Core, nullable unknown fractions, credential sharing, persisted deadlines, non-activating HUD behavior, and the required desktop verification workflow.

### Explicit product decisions and assumptions

- **D1:** A derived balance must retain the weakest relevant qualification of its inputs. This makes S1's historical fallback compatible with its prohibition on presenting a historical estimate as a live balance.
- **D2:** Cached readings remain associated with owner and period. This is a derived acceptance obligation needed to enforce S1's compatibility rules during account changes and rollover.
- **D3:** Text identifies unavailable, stale, and partial states. This is an accessibility acceptance decision for the requested labels, not a new navigation design.
- **Assumption A1:** The intended release includes all three resolved billing scopes and the historical-report fallback described by S1, subject to available authorization. If a scope lacks access, unavailable behavior satisfies that runtime case; it does not remove the scope from implementation acceptance.
- **Assumption A2:** Unresolved scope stays unavailable as S1 directs. A new manual selector is not required; adding one later changes product scope.

### Conflicts, pending evidence, and derived artifacts

The existing Copilot specification calls internal data an authoritative billing total and describes `token_based_billing` as AI credits/premium interactions. For this feature, S1 and FR-01 take precedence: operational premium interactions are not documented AI-credit billing values. Preserve operational behavior while revalidating that terminology and fidelity claim. The plan's phrase “two independent metrics” denotes two metric families; allowance, consumption, and remaining are distinct fields within the AI-credit family.

Pending implementation evidence: an authorized real billing response and matching seat context; whether an authoritative allowance field exists; verified field semantics for scope, period, seat changes, promotions, discounted coverage, and additional usage; and report coverage/access for supported scopes. Absent allowance evidence requires the defined unavailable behavior and does not block this PRD. No unresolved product choice blocks creation of this artifact.

Revalidate the Copilot provider specification, any retained provider PRD/TechSpec/task artifacts, snapshot/fidelity contracts, and HUD presentation acceptance against this PRD before implementation approval. These artifacts are not edited here. New obligations cover scoped billing, historical fallback, evidence-gated balances, independent freshness/failure handling, and semantic rows. The misleading billing interpretation is replaced for this feature; no existing operational quota or activity obligation is removed.

## PRD acceptance gate

- [x] Every requirement has a stable, unique ID and an observable criterion.
- [x] Outcomes, boundaries, assumptions, and out-of-scope items are explicit.
- [x] Internal rules are attributed to the user or identified repository sources.
- [x] External facts are bounded by consulted documentation; authenticated allowance evidence is explicitly pending.
- [x] Source conflicts have a stated resolution for this feature and artifacts to revalidate are identified.
- [x] Architecture and implementation sequencing remain in the future TechSpec.
- [ ] Product review of this draft is recorded; creation of the PRD does not claim implementation approval or completed runtime validation.
