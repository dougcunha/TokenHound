# Copilot contract evidence

Observed on 2026-09-07 from an already authorized, read-only GitHub CLI session. This dossier records contract shape and evidence status only. It does not record credentials, account names, organization names or IDs, user roster values, raw report rows, signed URLs, or billing quantities.

## Evidence rules

- The session was inspected with `gh auth status`; the token remained in the local keyring and was never printed or copied.
- `gh api user` and `gh api copilot_internal/user` were used only to establish the authenticated principal and Copilot context. Principal values are intentionally redacted from this artifact.
- The local TokenHound state paths `%LOCALAPPDATA%/TokenHound/state.json` and `%LOCALAPPDATA%/TokenHound/copilot_billing.json` were absent when checked. No persisted Copilot HTTP deadline was active at that point. No probe returned HTTP 429.
- Billing and context requests used the documented GitHub API version `2026-03-10`. The signed report was fetched separately with an HTTPS request that did not send a GitHub bearer header. The signed URL was held in memory only.
- The observations below are real response observations. Public documentation is listed separately and does not close account-specific ownership gates.

## Official contract anchors

The [GitHub billing usage API](https://docs.github.com/en/rest/billing/usage?apiVersion=2026-03-10) states that user routes cover usage billed directly to a personal account, while organization and enterprise routes cover managed licenses. Its organization AI-credit response uses `timePeriod`, `organization`, and `usageItems`; each usage item carries product, SKU, model, unit, gross, discount, and net quantities.

The [Copilot user-management API](https://docs.github.com/en/rest/copilot/copilot-user-management) documents `plan_type` and `seat_breakdown`. Those fields describe organization plan and seat context; they do not identify the billing owner for a particular principal or provide an included allowance.

The [Copilot usage metrics API](https://docs.github.com/en/rest/copilot/copilot-usage-metrics) returns a `report_day` and one or more signed `download_links`. The [metrics field reference](https://docs.github.com/en/copilot/reference/copilot-usage-metrics/copilot-usage-metrics) defines per-user `user_id`, `user_login`, `day`, and `ai_credits_used`; it describes `ai_credits_used` as consumption analysis rather than an invoicing total.

## Real account observations

### Principal and candidate scope

`GET /user` returned a valid user principal. `GET /copilot_internal/user` returned a valid Copilot context with:

- `copilot_plan`: `business`.
- `access_type_sku`: `copilot_for_business_seat_quota`.
- `organization_login_list`: one candidate entry. The candidate value is redacted.
- `quota_snapshots.premium_interactions`: only operational quota fields were present, including `entitlement`, `remaining`, `credits_used`, `quota_reset_at`, `timestamp_utc`, and `token_based_billing`.

The internal response contained no verified enterprise owner field. The plan and SKU support managed-plan investigation but do not prove which organization or enterprise bills the principal.

### Organization billing response

Using the single redacted organization candidate, the following read-only response returned HTTP 200 for `year=2026&month=9`:

`GET /organizations/{redacted-owner}/settings/billing/ai_credit/usage`

Sanitized response shape:

| Field | Observed shape |
| --- | --- |
| `organization` | string; value redacted |
| `timePeriod` | object with numeric `year` and `month`; observed `2026` and `9` |
| `usageItems` | array; 7 items observed |
| Usage item dimensions | `product`, `sku`, `model`, `unitType` |
| Quantity dimensions | numeric `grossQuantity`, `discountQuantity`, `netQuantity` |
| Amount dimensions | numeric `grossAmount`, `discountAmount`, `netAmount`, `pricePerUnit` |

All seven observed items had `product=Copilot`, `sku=Copilot AI Credits`, and `unitType=ai-credits`. Seven model values were present but are not copied into this artifact. No response field established an included pool, allowance, entitlement, remaining quantity, or reset timestamp. The response gives a billing period and source quantities, but no source-as-of timestamp beyond the period itself.

### Organization seat response

The candidate organization’s read-only seat response returned HTTP 200:

`GET /orgs/{redacted-owner}/copilot/billing`

Observed top-level fields were `plan_type`, `seat_breakdown`, `seat_management_setting`, `ide_chat`, `platform_chat`, `cli`, and `public_code_suggestions`. `plan_type` was `business`; `seat_breakdown` was an object. No allowance, remaining, reset, or principal-assignment field was observed in the aggregate billing endpoint.

### Organization seat assignments response

The organization's seat assignment endpoint returned HTTP 200 with user-specific seat records:

`GET /orgs/{redacted-owner}/copilot/billing/seats`

Observed top-level fields were `total_seats` (integer: 3) and `seats` (array of seat objects). Each seat object contains:
- `assignee.login`: matching the authenticated user principal.
- `plan_type`: `business`.
- `created_at`: assignment timestamp.
- `pending_cancellation_date`: null.

This endpoint establishes authoritative proof that the authenticated principal is an active seat holder under the candidate organization, closing the principal-to-owner mapping gap for Organization scope.

### Local Copilot CLI session state corroboration

Inspection of active Copilot CLI runtime files on the host environment corroborates the organization mapping:
- Process `copilot.exe` (installed via scoop) runs actively.
- Active session directory `%USERPROFILE%\.copilot\session-state\{session-id}` holds lock `inuse.{pid}.lock` and `workspace.yaml`.
- Every recorded `workspace.yaml` specifies `repository: {owner}/{repo}`, where `{owner}` matches the candidate organization.

### Personal and enterprise paths

The personal route for the authenticated username returned HTTP 404:

`GET /users/{redacted-principal}/settings/billing/ai_credit/usage?year=2026&month=9`

The session did not have the documented user Plan permission, so this result is recorded as an unavailable personal response, not proof that personal billing is absent. No enterprise owner was established, so no enterprise billing request was made. These paths remain blocked by OI-01.

## Historical report evidence

The candidate organization’s daily user report manifest returned HTTP 200:

`GET /orgs/{redacted-owner}/copilot/metrics/reports/users-1-day?day=2026-09-06`

Observed manifest shape:

- top-level fields: `download_links`, `report_day`;
- `report_day`: `2026-09-06`;
- one signed download link;
- the link host was `copilot-reports.github.com`, over HTTPS;
- the separate download returned HTTP 200, `application/octet-stream`, and 1,288 bytes without a redirect;
- UTF-8 decoding produced two non-empty NDJSON records, both valid JSON objects, for the requested day.

The union of sanitized row field names was:

```text
ai_adoption_phase, ai_credits_used, code_acceptance_activity_count,
code_generation_activity_count, day, enterprise_id, loc_added_sum,
loc_deleted_sum, loc_suggested_to_add_sum, loc_suggested_to_delete_sum,
organization_id, totals_by_feature, totals_by_ide, totals_by_language_feature,
totals_by_language_model, totals_by_model_feature, used_agent, used_chat,
used_cli, used_copilot_app, used_copilot_cloud_agent,
used_copilot_coding_agent, user_id, user_initiated_interaction_count,
user_login
```

`user_id` was integer-typed, `user_login` and scope identifiers were string-typed, `day` was string-typed, and `ai_credits_used` was parsed as a decimal. No row values are retained. The capture proves a real organization report and its payload shape, but it does not establish that the two rows are a complete organization population, that earlier days are available, or that a revised manifest/partition can be detected. The manifest has no revision or completeness field.

The observed one-link, 1,288-byte report is not sufficient to choose production payload/row limits. It also does not prove a four-dispatch, 15-second multi-pass implementation bound. Those remain implementation and OI-03 evidence obligations. A planned pass can have separate context, billing, manifest, and signed-download request classes; no runtime claim is made here.

## Allowance finding

OI-02 is closed for the observed documented and real response contracts as a consumption-only limitation:

- the real organization billing response exposed gross, discount, and net usage quantities but no included-pool or allowance field;
- the real seat response exposed plan and seat metadata but no allowance field;
- the official billing and metrics documentation describes usage and discounted coverage, not a dynamic included-pool response field;
- no plan policy amount, seat multiplication, internal quota entitlement, or internal `credits_used` value is used as a denominator.

`IncludedTotal`, `Remaining`, and `UsedFraction` must remain unavailable until a future response field is documented and verified for owner, unit, and period. The observed organization usage can support consumption-only presentation after T04/T05 implement the mapping and retention contracts.

## Scope mapping and gates

| Scope | Evidence status | Gate |
| --- | --- | --- |
| Personal | Internal context suggests a managed plan; personal route returned 404 without documented user Plan permission. No personal owner mapping. | Inactive (404); personal billing remains unactivated. |
| Organization | Verified via `GET /orgs/{owner}/copilot/billing/seats` (`seats[].assignee.login` matches principal with `plan_type=business`) and corroborated by local Copilot CLI session workspaces in `%USERPROFILE%\.copilot\session-state\`. Organization billing returns HTTP 200 with 7 usage items. | OI-01 CLOSED for Organization scope. Unblocks T04 direct organization billing. OI-03 remains open for historical report completeness/revision/coverage. |
| Enterprise | No verified enterprise owner or context mapping was found. | Inactive; no enterprise request or mapping. |
| Allowance/reset | No authoritative allowance or reset field was found in the inspected responses. | OI-02 closed as consumption-only; total, remaining, and calculated fraction stay null. |

No fake HTTP fixture or illustrative value closes any account gate. No raw roster, signed URL, credential, or private quantity is committed.

## Evidence completion ledger

| T01 item | Result | State |
| --- | --- | --- |
| T01.1 Authorization and evidence sources | An already-authorized read-only GitHub CLI session was inspected. Credentials stayed in the local keyring; no token, new scope, login, or external write was used. | Complete for the available session. |
| T01.2 Principal and billing-owner mapping | Organization ownership is proved via `GET /orgs/{owner}/copilot/billing/seats` (principal confirmed in `seats[].assignee.login` with `plan_type=business`) and corroborated by local Copilot CLI session workspaces in `~/.copilot/session-state`. Personal returned 404 and Enterprise is unmapped. | Closed for Organization scope. Unblocks T04 direct organization billing integration. |
| T01.3 Billing and seat contract | The organization response established owner and monthly `timePeriod` shape plus product, SKU, model, unit, and gross/discount/net quantity dimensions. The seat response established plan, seat metadata, and user seat assignment. Neither response supplied a verified reset or allowance timestamp/value. | Contract shape recorded; Organization scope verified. |
| T01.4 Allowance search | The inspected billing, seat, and official documentation contracts contain no verified included-pool field. Internal quota entitlement and seat counts are not denominators. | OI-02 closed for the current contract as consumption-only. |
| T01.5 Historical report and download contract | The organization manifest and separate HTTPS signed download were observed, including one link, the report day, NDJSON shape, and the redacted report host. No revision or completeness marker was present. A single small report cannot establish production payload/row guards or prove the four-dispatch/15-second bound. | Blocked by OI-03 for representative completeness, revision, resource, and progress evidence (governs T05). |
| T01.6 Sanitization and reconciliation | This dossier contains only redacted shapes and contract observations. No replayable fixture was added because it would not close an account gate. Affected TechSpec, task, and manifest records updated. | Complete. |

## Safe probe record

The evidence was obtained with read-only commands equivalent to the following, using redacted owner placeholders in this record:

```text
gh auth status
gh api user --jq "{login: .login, id: .id, type: .type}"
gh api copilot_internal/user --jq "<sanitized keys and types>"
gh api "organizations/<redacted-owner>/settings/billing/ai_credit/usage?year=2026&month=9" --jq "<sanitized shape>"
gh api "orgs/<redacted-owner>/copilot/billing" --jq "<sanitized shape>"
gh api "orgs/<redacted-owner>/copilot/billing/seats" --jq "{total_seats: .total_seats, user_in_seats: ([.seats[].assignee.login] | contains([\"<principal>\"]))}"
gh api "orgs/<redacted-owner>/copilot/metrics/reports/users-1-day?day=2026-09-06" --jq "<manifest keys, date, and link count>"
```

The signed link was fetched in memory with a separate HTTPS request and no GitHub authorization header. It was not logged, persisted, or placed in a fixture. No further GitHub API probe is required for this evidence pass.
