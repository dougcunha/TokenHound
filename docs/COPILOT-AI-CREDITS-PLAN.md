# Copilot AI Credits: Adjustment Plan

Status: proposed implementation plan

## Purpose

Add GitHub Copilot AI-credit information to TokenHound without relabeling the existing internal quota response, mixing personal and managed billing, or inventing a denominator that GitHub has not returned.

The provider currently reads `https://api.github.com/copilot_internal/user`. Its `quota_snapshots.premium_interactions` object is an operational quota with fields such as `entitlement`, `remaining`, and `credits_used`. It must remain a separately labeled metric. The documented Copilot usage APIs use `ai_credits_used` for historical consumption reports, so the internal `premium_interactions` values must not be presented as the Business AI-credit pool until GitHub documents them as the same unit.

## Findings and source-of-truth rules

- GitHub documents plan allowances and promotional amounts in product documentation, but those published numbers are policy data, not a response field returned by the documented billing APIs. The current organization/enterprise page lists standard Business/Enterprise and promotional amounts, but the implementation must not embed those values or promotion dates as a code-level denominator. See GitHub's [usage-based billing documentation](https://docs.github.com/en/enterprise-cloud@latest/copilot/concepts/billing/organizations-and-enterprises/usage-based-billing).
- Personal Copilot plans are a different billing domain. A personal account has its own plan allowance and usage; it is not an organization/enterprise seat and must not be converted into a shared pool. See GitHub's [Copilot billing documentation](https://docs.github.com/en/billing/concepts/product-billing/github-copilot-billing).
- A managed license must be distinguished from a personal plan before choosing an endpoint. GitHub states that user-level billing endpoints apply to personally purchased plans; usage for a license managed and billed by an organization or enterprise belongs to the organization/enterprise endpoint instead. The [billing usage API](https://docs.github.com/en/rest/billing/usage?apiVersion=2026-03-10) documents this scope rule.
- The Copilot usage metrics API exposes per-user `ai_credits_used` in daily and 28-day reports. GitHub describes those reports as consumption analysis, not invoicing totals, and they may not represent a real-time balance. See the [usage metrics REST API](https://docs.github.com/en/rest/copilot/copilot-usage-metrics) and the [documented metrics fields](https://docs.github.com/en/copilot/reference/copilot-usage-metrics/copilot-usage-metrics).
- The documented billing APIs are candidates for the authoritative billing-period aggregate: `GET /organizations/{org}/settings/billing/ai_credit/usage` and `GET /enterprises/{enterprise}/settings/billing/ai_credit/usage`. Their `usageItems` data includes quantities such as gross, net, and discount quantities. Use the [organization billing API documentation](https://docs.github.com/en/rest/billing/usage?apiVersion=2026-03-10) and the [enterprise billing API documentation](https://docs.github.com/en/enterprise-cloud@latest/rest/billing/usage?apiVersion=2026-03-10) when implementing the response DTO.
- The Copilot seat/billing endpoint can provide plan and seat context at organization scope. Consult the [Copilot user-management API](https://docs.github.com/en/rest/copilot/copilot-user-management) when resolving the billing entity and assigned seats.
- The currently documented response schemas do not expose an included-pool total, per-license allowance, `quotaRemaining`, or `entitlement` field. `usageItems.discountQuantity` describes usage covered by included usage; it is not the allowance itself. Treat a future/undocumented allowance field as untrusted until its scope, period, and semantics are verified against a real response and official documentation.
- GitHub's [billing reports reference](https://docs.github.com/en/billing/reference/billing-reports) and [usage-reporting guide](https://docs.github.com/en/billing/tutorials/automate-usage-reporting) distinguish consumed quantity from discounted/included usage and billable net usage. Preserve those dimensions independently; do not add gross, discount, and net quantities together.
- Official API access is conditional: the relevant enterprise or organization policy must allow metrics access, and the token must have the required role and authorization scope. An unavailable report is not proof that usage is zero.

## Target information model

Keep two independent metrics in the provider result:

| Metric | Meaning | Preferred source | Missing-data behavior |
| --- | --- | --- | --- |
| Premium interactions quota | Operational quota returned by the current internal endpoint | `quota_snapshots.premium_interactions` | Preserve the existing nullable values and label the unit explicitly |
| Included AI credits | Credits included in the current billing period | An authoritative allowance/pool field from the selected billing source, if one exists | `null` when the API provides only plan/seat metadata or usage; never derive from a hard-coded plan table |
| AI credits used | Gross consumption for the same billing period | Billing usage API filtered to Copilot AI-credit items; otherwise a sum of per-user `ai_credits_used` reports | Preserve gross, discounted/included, and net/billable quantities as separate fields; do not merge with internal `credits_used` without unit validation |
| AI credits remaining | Included credits less usage for the same period | Compute only from compatible authoritative allowance and usage values | `null` when the allowance is not returned, estimated, or from a different period |

A dedicated optional `CopilotBillingStatus`/`CopilotCreditUsage` value is preferable to overloading a generic `LimitWindow` with fields that have different billing and freshness semantics. If the existing snapshot contract is extended instead, all credit fields must remain nullable and carry period/source metadata.

The model must carry billing scope and plan classification explicitly:

| Scope | Examples | Endpoint family | Pool rule |
| --- | --- | --- | --- |
| Personal | Copilot Free, Pro, Pro+, or Max purchased by the user | `/users/{username}/settings/billing/ai_credit/usage` where the token and account support it | Use the personal plan's reported allowance; never use `seats × 1,900` or a shared organization pool |
| Organization-managed | Copilot Business or Enterprise billed by one organization | `/organizations/{org}/settings/billing/ai_credit/usage` plus `/orgs/{org}/copilot/billing` | Use the organization's effective plan and included seats, subject to the billing entity and cycle rules |
| Enterprise-managed | Copilot usage billed by an enterprise | `/enterprises/{enterprise}/settings/billing/ai_credit/usage` | Use the enterprise billing pool; use `organization`, `user`, `model`, and cost-center filters when narrowing a report |
| Unknown or ambiguous | Membership is visible but billing ownership is not | No speculative endpoint selection | Keep the billing fields unavailable; do not infer personal or shared usage from `organization_login_list` alone |

`PlanType` and `BillingScope` should be separate fields. `Business` and `Enterprise` are managed-plan values; individual plan names must not enter the managed seat formula. The UI should identify a personal value as personal usage and a managed value as a shared organization/enterprise pool.

Suggested fields are:

- billing scope and identifier (`organization` or `enterprise`);
- plan type;
- billing-period start, end, and reset timestamp;
- allowance/pool total and its source field, when returned;
- allowance provenance (direct API value, documented policy, or unavailable);
- included total credits;
- credits used, with decimal precision preserved;
- included credits remaining;
- whether the value is estimated or directly reported;
- source and `asOf` timestamp.

## Implementation phases

### 1. Establish billing scope and contracts

1. Classify the account as personal, organization-managed, enterprise-managed, or unknown before selecting an endpoint. A personal plan must use personal billing semantics; it is not a seat in a shared Business/Enterprise pool.
2. For managed accounts, determine whether the assigned licenses are billed by an organization or an enterprise. Use `organization_login_list` from the internal response only as a candidate organization list: one unambiguous organization may be resolved; multiple organizations must not be summed automatically; an explicit enterprise scope takes precedence.
3. Record the authorization and policy prerequisites for each endpoint. The organization seat endpoint is public preview and requires an owner or appropriate Copilot/administration billing permission; billing AI-credit usage requires organization administration or enterprise billing access. Personal and managed tokens must not be interchanged silently.
4. Define the distinction between `premium_interactions` and AI credits in the provider contract and the Copilot specification.
5. Define billing-period semantics for personal allowances, managed seat changes, promotional allowances, overage, and incomplete reports before adding an aggregation formula. In particular, GitHub documents that adding managed licenses mid-cycle increases the pool immediately, while removing them does not shrink the pool until the next cycle.

### 2. Add billing API transport and parsing

1. Add a read-only billing client in Infrastructure with cancellation, bounded timeouts, the existing User-Agent convention, and rate-limit/deadline handling.
2. Add DTOs for organization/enterprise AI-credit usage, seat/plan context, and user metrics report rows. Preserve nullable and decimal values; do not coerce missing quantities to zero.
3. Send `Accept: application/vnd.github+json` and `X-GitHub-Api-Version: 2026-03-10` on documented requests. Pass the UTC billing cycle as `year` and `month`, and use `day`, `organization`, `user`, `model`, `product`, and `cost_center_id` only when the selected scope requires them.
4. Parse both direct usage responses and signed report downloads only where the endpoint contract requires them.
5. Preserve each `usageItem`'s `grossQuantity`, `discountQuantity`, and `netQuantity` independently. Filter to the Copilot AI-credit product/unit before aggregation. Use gross quantity for consumption, discount quantity for included/discounted coverage, and net quantity for billable additional usage only when the response semantics support that mapping; never sum the three fields into one number.
6. Keep credential discovery read-only. The new client must not write, refresh, or replace credentials owned by GitHub CLI or another tool.
7. Cache the last successful billing result independently from the internal quota result, with its source timestamp and period.

### 3. Aggregate compatible values

1. Prefer the billing usage endpoint for the current billing-period aggregate when its response identifies the same billing entity and period. For an organization, pair it with `GET /orgs/{org}/copilot/billing`; for an enterprise, use the enterprise billing endpoint and its organization filter where appropriate.
2. If only per-user metrics are available, request daily reports from the billing-period start through the newest available day and sum `ai_credits_used`. Treat the result as a consumption report, not an exact invoice balance.
3. Resolve the included total from an authoritative allowance/pool field when one is returned for the same billing entity and period. The public documentation currently exposes plan policy and seat context but not a dynamic allowance field; in that case, keep the total and remaining values `null`. Do not encode `1,900`, `3,000`, `3,900`, `7,000`, `5,700`, or promotion dates as the source of truth.
4. Calculate `AI credits remaining` from the compatible gross consumption and authoritative included pool only when total and used values use the same unit and period. Preserve discounted/included coverage and overage separately rather than hiding them by clamping total usage.
5. Keep internal `premium_interactions.entitlement`, `premium_interactions.remaining`, and internal `credits_used` in their own metric family. In particular, do not map `entitlement = 20,000` or `credits_used = 725` to the documented Business AI-credit values without evidence that GitHub uses the same units. The target `4,975 / 5,700` and `725 credits used` is valid only if the billing response independently reports both the applicable pool and 725 gross AI credits for that same scope and period.

6. Add an explicit allowance-discovery step before implementing the remaining calculation:

   - Capture the actual organization and/or enterprise AI-credit response for the authenticated account and inspect all documented fields, including `timePeriod` and `usageItems`.
   - Capture the matching Copilot seat response and verify whether it contains only `plan_type`/`seat_breakdown` or an allowance field as well.
   - If a response exposes an allowance/pool key, document its exact JSON path, unit, billing scope, period, and behavior during promotions and seat changes before parsing it.
   - If no authoritative key exists, show reported usage, discounted/included usage, additional usage, and reset date, but show total/remaining as unavailable rather than applying the documentation's standard or promotional numbers.

### 4. Integrate with the provider

1. Refresh billing data alongside the current Copilot quota, while allowing either source to succeed independently.
2. Apply the existing backoff and 429 deadline policy to every billing request and report download.
3. On authorization, policy, network, or schema failure, retain the last valid value only with its age/source metadata. If no valid value exists, expose `null`/unavailable rather than zero.
4. Make the provider snapshot distinguish direct, derived, estimated, stale, and unavailable values where the existing fidelity model supports those states.

Expected implementation touchpoints include `CopilotQuotaResponse`, `CopilotQuotaParser`, `CopilotUsageProvider`, and the core window/snapshot contract. The final design may use a dedicated billing model instead of forcing billing-specific fields into `LimitWindow`; either choice must expose an explicit used value and keep the AI-credit window separate from the monthly premium-interaction quota.

### 5. Replace hardcoded HUD rows with data-driven rows

The current generic tooltip maps provider windows into fixed `Current session (5h)` and `Weekly limit (7d)` slots. That mapping must not be used for monthly AI credits.

Add separate rows with explicit labels:

- `AI credits`: used, included total, remaining, billing-period reset, and an `estimated`/`as of` indicator when applicable;
- `Premium interactions`: the operational quota values and reset date from the internal endpoint;
- existing session/weekly rows only when the provider actually reports windows with those semantics.

For the Copilot billing case, the intended presentation is equivalent to:

```text
AI credits
4,975 / 5,700 remaining
725 credits used
Resets in ...
```

Those numbers are an example of a verified billing response, not a fallback interpretation of `premium_interactions.credits_used`.

The UI should show an unavailable state when a value is missing, not `0 / 0`. It should not imply that an approximate historical sum is a live balance. Any new tooltip or detail layout must preserve the HUD focus and geometry rules in `docs/design/`.

## Test plan

- DTO tests for missing fields, decimal quantities, signed-report rows, unknown plan types, and additional usage.
- Billing aggregation tests for multiple models proving that gross, discount, and net quantities are independently summed and never double-counted.
- Scope tests proving that personal plans use personal billing data, managed Business/Enterprise plans use shared billing data, and organization membership alone cannot promote a personal account into a seat pool.
- Scope-resolution tests for organization billing, enterprise billing, multiple organizations, and missing billing metadata.
- Aggregation tests using response-provided allowances, including promotional and standard fixtures, seat changes, partial reports, overage, mismatched periods, and missing denominators. A three-seat/5,700 fixture is valid only when `5,700` is supplied by the simulated authoritative response, not by production code.
- Regression tests proving that internal `premium_interactions` values are not converted into AI credits.
- Provider tests for independent source failure, stale-value metadata, 429 deadlines, cancellation, and no credential writes.
- View-model tests for the separate AI-credit and premium-interaction rows, unavailable states, estimated labels, and the existing session/weekly behavior.
- Manual HUD verification through the Windows App launch and primary-monitor screenshot flow after the data-driven layout is implemented.

## Acceptance criteria

1. The UI can show Business AI-credit usage and remaining credits only when their source, unit, and period are compatible.
2. The UI never labels the internal `premium_interactions` entitlement or internal `credits_used` as Business AI credits without a documented unit mapping.
3. The allowance/pool total is taken from a verified response field when available; no standard or promotional allowance is hard-coded, and total/remaining remain unavailable when the API exposes only usage and seat metadata.
4. Historical per-user metrics are visibly distinguished from a real-time or billing-derived balance.
5. Missing access, missing reports, rate limits, and partial data produce an honest unavailable/stale state and do not reset a valid value to zero.
6. Existing Copilot quota behavior and all non-Copilot providers remain unchanged.

## Validation commands

When implementation begins, use the repository's required MTP runner and minimum-test guard, for example:

```text
rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
```

The implementation should also build the Core, Infrastructure.Tests, and App projects with `rtk dotnet build --no-restore` before the no-build test runs. Filtered Copilot tests must still include `--minimum-expected-tests 1`. Preserve `$LASTEXITCODE` after every command and inspect failures directly from stdout.

Validate the desktop HUD separately through the Windows App launch and screenshot workflow. This document itself does not implement the billing client or change the provider/UI behavior.
