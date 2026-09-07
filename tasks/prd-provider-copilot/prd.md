# PRD: GitHub Copilot provider

Feature slug: `provider-copilot`

## Problem and context

TokenHound needs to show GitHub Copilot quota and local agent activity in the standard Notch without creating a competing GitHub login or changing credentials owned by GitHub tools. The Copilot-specific source is an undocumented and unversioned internal endpoint. Its quota categories are an open map, and Copilot does not expose a PID-scoped busy flag for standalone polling. The product therefore needs explicit finite-quota selection, honest stale and unsupported states, persistent rate-limit handling, and a clearly bounded activity heuristic.

The requested outcome is a lightweight Copilot provider for Windows 11 that:

- identifies itself as `copilot` and presents the official monthly premium-interaction reading;
- borrows a usable OAuth token from the approved local sources in the specified precedence order;
- reads `https://api.github.com/copilot_internal/user` and preserves the response semantics without inventing quota data;
- reports `Ok`, `Stale`, `NeedsAuth`, or `Unsupported` according to the specified conditions;
- exposes Copilot `Busy` or `Idle` activity from local session writes and host-process liveness; and
- feeds the existing Snapshot, Provider Ring, status, and sign-in conventions used by TokenHound.

The product stage defines behavior and boundaries. Implementation structure, types, sequencing, and test design belong in the TechSpec and later task artifacts.

## Scope

In scope are the Copilot provider identity and fidelity, read-only credential discovery, the single lightweight HTTP quota path, defensive parsing of finite quota snapshots, reset and overage semantics, status and archive behavior, persistent 429 deadlines, local activity monitoring, standard polling cadence, and presentation through the existing TokenHound provider experience.

The provider must use the Copilot specification as the source of truth for field meaning. It must not convert the response into legacy request counts or infer a denominator that GitHub did not report.

## Outcomes and metrics

The specification provides behavioral evidence rather than launch KPIs. The following outcomes are the measurable product gate for this feature until product supplies additional metrics.

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | A developer can see the official Copilot monthly premium-interaction reading. | An `Ok` snapshot identifies Copilot, uses official fidelity, exposes the finite quota remainder, and includes the top-level UTC reset instant. |
| OBJ-02 | The Notch does not imply usage for categories that are uncapped or not entitled. | Categories with `unlimited == true` or `has_quota == false` are omitted; a present map with no finite category produces `Unsupported` with an empty ring instead of a fabricated zero. A missing or unusable map remains `Stale` when an archived reading exists. |
| OBJ-03 | TokenHound remains a read-only borrower of GitHub tool credentials. | Discovery follows the required precedence, performs no token refresh or write, and routes invalid token cases to the documented sign-in guidance. |
| OBJ-04 | Copilot activity reflects local evidence instead of an invented provider flag. | `Busy` occurs only when a qualifying Copilot session write is recent and a qualifying host process is alive; otherwise the state returns to `Idle`. |
| OBJ-05 | Temporary provider failures do not erase the last valid reading or bypass a rate-limit deadline. | 429, timeout, and schema-drift cases retain the archived reading as `Stale`; a persisted deadline prevents dispatch until it expires. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer using Copilot | See remaining monthly premium interactions and the reset time. | The developer can understand the current Copilot allowance from the same Notch used for other providers. | A finite `premium_interactions` snapshot is available and the provider is `Ok`. |
| US-02 | Developer running a Copilot session | See whether Copilot is active. | The developer can distinguish a currently active assistant from an idle one without a provider busy API. | A recent session write and a live Copilot host produce `Busy`; either signal going away produces `Idle`. |
| US-03 | Developer with an invalid or unsuitable token | Learn how to restore access without handing TokenHound a PAT. | The developer gets actionable guidance while TokenHound avoids modifying the account. | A 401 or PAT-shaped 403 produces `NeedsAuth`, discards history, and points to `gh auth login` or `copilot login`. |
| US-04 | Developer during a transient outage, schema change, or rate limit | Keep the last trustworthy reading and understand that it is not current. | A transient failure does not appear as zero usage or a false current value. | Timeout, schema drift, or 429 produces `Stale`; the archived reading remains and a 429 deadline is respected. |
| US-05 | Developer with an uncapped, missing, or unavailable quota | See an honest empty state. | The Notch does not represent an absent quota as exhaustion. | No finite snapshot produces `Unsupported` with an empty ring; an unlicensed 403 also produces `Unsupported` and discards history. |
| US-06 | Developer whose finite allowance is exhausted | Understand whether metered overage continues. | The product distinguishes continued metering from a hard quota block. | `remaining <= 0` with overage permitted creates a non-blocking overage usage block; otherwise the block lasts until reset. |

## Functional requirements

| ID | Requirement | Acceptance criterion | Source |
| --- | --- | --- | --- |
| FR-01 | The provider shall use ID `copilot`, display name `Copilot`, official fidelity, and `Monthly Premium Interactions` as its headline metric. | A successful provider result contains the specified ID, name, fidelity, and headline metric. The headline is sourced from the finite premium-interaction category when it is available. | [Copilot specification, section 1](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-02 | The provider shall discover a usable credential in this exact order: `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`, `gh auth token`, and the Copilot CLI keychain with the plaintext `%USERPROFILE%\\.copilot\\config.json` fallback or `COPILOT_HOME` override. It may inspect VS Code extension state for an identity hint only, never as a credential. | When more than one credential source is usable, the first source in the order wins. The VS Code state is never used as a bearer token, and a missing higher-priority source causes the next credential source to be considered. | [Copilot specification, section 2](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-03 | The provider shall query the lightweight path with `GET https://api.github.com/copilot_internal/user`, sending `Authorization: Bearer <githubOAuthToken>` and `Accept: application/json`, with the specification's recommended 15-second request timeout. | Request inspection shows the method, URL, authorization scheme, accept header, and the specification's recommended timeout. The feature makes no Copilot SDK RPC call. | [Copilot specification, sections 3 and scope note](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-04 | The provider shall select a finite quota from the open `quota_snapshots` map by preferring `premium_interactions` when it has `has_quota == true`, then falling back to any snapshot with `has_quota == true`. It shall not require a fixed category key. | With a finite premium category, that category is selected. With no finite premium category but another finite map entry, the other entry is selected. With a present map containing no finite entry, the result is `Unsupported` with an empty ring. A missing or unusable map follows schema-drift handling and is `Stale` when an archived reading exists. Uncapped or non-entitled categories are omitted rather than rendered as zero. | [Copilot specification, section 4](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-05 | The provider shall calculate `usedFraction` as `1.0 - (percent_remaining / 100.0)`, display the reported `quota_remaining` float for the remainder, and use top-level `quota_reset_date_utc` for the reset. It shall use `entitlement - remaining` only as the specified integer cross-check and ignore a per-snapshot `quota_reset_at` value of `0`. | A fixture with reported percentage, fractional remainder, integer values, and a top-level ISO-8601 reset produces the stated fraction, fractional display value, and reset instant. No denominator or credit conversion is supplied by TokenHound. | [Copilot specification, sections 3 and 4](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-06 | The provider shall represent overage according to the response: when `remaining <= 0` and `overage_permitted == true`, it shall record that metered overage continues without creating a hard block; when overage is not permitted and the quota is exhausted, it shall create a blocking usage block until `ResetsAt`. | The two response variants produce distinct usage-block semantics. The permitted-overage case does not prevent continued metering, and the non-permitted case remains blocking through the reported reset instant. | [Copilot specification, section 4](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-07 | The provider shall map responses and parsing outcomes as follows: success to `Ok` and archive update; 401 to `NeedsAuth` with history discarded; PAT-shaped 403 to `NeedsAuth` with history discarded; unlicensed or no-seat 403 to `Unsupported` with history discarded; 429 to `Stale` with history retained; timeout and schema drift to `Stale` with history retained. | A response/status matrix demonstrates each mapping and its history action. A 401 or 403 does not leave an old quota reading visible as current and uses the standard unavailable-value marker, while a 429, timeout, or schema-drift result retains the archived reading as stale. | [Copilot specification, sections 4 and 6](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-08 | For `NeedsAuth`, the provider shall guide the developer to run `gh auth login` or `copilot login` in a terminal and shall explicitly avoid requesting a pasted PAT. Persistent 403 results shall also identify an unrelated `GH_TOKEN` or `GITHUB_TOKEN` environment override as a possible cause. | The sign-in route contains the documented commands and does not instruct the developer to paste a PAT. A persistent 403 exposes the environment-override warning without claiming that the Copilot subscription was revoked. | [Copilot specification, sections 2 and 6](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-09 | The activity monitor shall inspect `LastWriteTimeUtc` for `%USERPROFILE%\\.copilot\\session-state\\*\\events.jsonl` and `%USERPROFILE%\\.copilot\\logs\\` every 2 seconds. It shall classify Copilot as `Busy` only when a matching file was written within the last 30 seconds and a live `copilot`, `gh`, or `Code.exe` host with the Copilot extension is present. | A recent write without a live host is `Idle`; a live host without a recent write is `Idle`; both signals together are `Busy`. After 30 seconds without writes, the state returns to `Idle`. | [Copilot specification, section 5](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-10 | The activity monitor shall use a 120 ms debounce over the Copilot session-state directory to consolidate burst writes between polls, and all activity files shall be read without modifying them. | Burst writes within the debounce interval produce one consolidated activity update, and a file-content or timestamp comparison shows no monitor write. | [Copilot specification, section 5](../../docs/specs/11-PROVIDER-COPILOT.md) |
| FR-11 | Standard quota polling shall use the existing active cadence of 60 seconds when any activity monitor reports `Busy` and the idle cadence of 300 seconds otherwise. A monthly quota shall not cause a faster cadence, and forced refreshes shall still honor an active 429 deadline. | A `Busy` and an `Idle` scenario schedule the respective cadence. No quota request is sent before an active rate-limit deadline, including after a forced refresh request. | [Copilot specification, section 6](../../docs/specs/11-PROVIDER-COPILOT.md) and [repository invariant](../../AGENTS.md) |
| FR-12 | Copilot results shall feed the existing Snapshot, Provider Ring, status, activity, and sign-in presentation. Finite quota data shall show the reported remainder and reset; `Busy` and `Idle` shall remain distinguishable; stale, unsupported, empty, overage, and sign-in states shall remain explicit. | A provider snapshot can drive the existing Notch presentation for the success, activity, stale, unsupported, overage, and sign-in journeys without converting an unavailable state into a zero or a current value. | [Copilot specification, sections 1, 4, 5, and 6](../../docs/specs/11-PROVIDER-COPILOT.md), [architecture](../../ARCHITECTURE.md), and [context](../../CONTEXT.md) |

## Non-functional requirements

| ID | Attribute | Requirement and limit or criterion | Acceptance criterion | Source |
| --- | --- | --- | --- | --- |
| NFR-01 | Credential security | TokenHound shall never refresh, re-mint, overwrite, or otherwise write a credential owned by `gh`, Copilot CLI, or VS Code. Credential-store reads shall not write back. Local config and state files shall be opened with `FileShare.ReadWrite | FileShare.Delete`. | A discovery run leaves borrowed file contents, metadata, and credential-store state unchanged. No flow requests a PAT or starts a competing login. | [Copilot specification, section 2](../../docs/specs/11-PROVIDER-COPILOT.md) and [repository invariants](../../AGENTS.md) |
| NFR-02 | Fidelity and honesty | The provider shall report official fidelity only for the specified Copilot billing reading, use the values supplied by the response, and avoid invented denominators, credit math, zeros, or percentages. `token_based_billing == true` shall not be converted into hard-coded credit math. | Review of finite, unlimited, absent, and malformed fixtures finds no synthetic quota value. An unavailable category is omitted or represented by the specified empty/unsupported state. | [Copilot specification, sections 1, 3, and 4](../../docs/specs/11-PROVIDER-COPILOT.md) and [architecture](../../ARCHITECTURE.md) |
| NFR-03 | Rate-limit resilience | A 429 deadline shall be persisted and respected before every subsequent network dispatch, including after application restart. `Retry-After: 0` may raise the stored floor but shall never cause an immediate retry. | After a 429, a forced refresh and a restarted process both defer the request until the persisted deadline. A zero retry value does not produce an immediate dispatch. | [Copilot specification, section 6](../../docs/specs/11-PROVIDER-COPILOT.md), [repository invariants](../../AGENTS.md), and [architecture](../../ARCHITECTURE.md) |
| NFR-04 | Schema resilience | The provider shall treat `quota_snapshots` as an open map and tolerate new keys. A missing map, moved or unusable finite key, schema drift, or unhandled non-200 response shall degrade to `Stale` with the archived reading retained, never to fabricated zeros. The explicit 401, 403, and 429 mappings remain authoritative exceptions. | Fixtures for missing maps, renamed categories, unknown categories, malformed fields, and unhandled non-200 responses produce `Stale` with the last valid reading retained. Explicit auth and rate-limit fixtures still produce their specified statuses. | [Copilot specification, section 4](../../docs/specs/11-PROVIDER-COPILOT.md) |
| NFR-05 | Responsiveness and cadence | Activity observation shall use the specified 2-second poll, 30-second freshness threshold, and 120 ms write debounce. Quota requests shall use the 60-second active or 300-second idle cadence and the recommended 15-second HTTP timeout. | Timing tests observe the stated thresholds and cadence, and a slow request terminates according to the 15-second recommendation without bypassing status or archive rules. | [Copilot specification, sections 3, 5, and 6](../../docs/specs/11-PROVIDER-COPILOT.md) |
| NFR-06 | Architecture | Copilot-specific external access belongs in `TokenHound.Infrastructure`; `TokenHound.Core` remains pure and has no UI or OS dependency. | Architecture inspection shows no Copilot network, credential, file, process, or WPF dependency in Core. | [architecture](../../ARCHITECTURE.md) and [repository instructions](../../AGENTS.md) |
| NFR-07 | HUD non-interference | Copilot presentation shall preserve the existing Notch behavior: non-activating, click-through behavior and no foreground-focus theft. | Inspecting or dragging the Copilot indicator does not activate the Notch or change the foreground application, and the existing HUD composition remains intact. | [repository instructions](../../AGENTS.md) and [architecture](../../ARCHITECTURE.md) |

## Fidelity, security, and rate-limit invariants

These invariants are binding product constraints, not implementation suggestions:

- Official fidelity means the provider may expose the authoritative Copilot billing reading delivered by the specified internal transport, while the transport remains undocumented. It does not authorize claims about fields that the response does not provide.
- Borrow-Don't-Own means TokenHound reads credentials and session files owned by official tools, uses the specified precedence, and never refreshes, re-mints, overwrites, or creates a competing login.
- A reported finite quota is the only source for a ring fraction. Unlimited, non-entitled, absent, stale, and unsupported states must remain distinguishable from zero usage.
- A 429 deadline is durable state. Every dispatch, including a forced refresh after restart, must wait for the deadline. `Retry-After: 0` is not permission to retry immediately.
- Stale data retains its last valid reading and is labeled stale. Authentication failures and unsupported access discard history as specified.

## User experience

The Copilot provider uses the existing Notch and Provider Ring rather than defining a new surface. In the normal journey, the ring is backed by the selected finite snapshot, the remainder comes from the reported `quota_remaining` float, and the reset comes from `quota_reset_date_utc`. The activity state is available as `Busy` or `Idle` under the existing liveness presentation.

Failure feedback follows the provider status rather than hiding it:

- `Stale` keeps the last valid reading and makes its freshness state explicit.
- `Unsupported` presents an empty ring when no finite quota or Copilot seat is available.
- `NeedsAuth` presents the specified terminal sign-in route and does not ask for a PAT.
- Permitted overage indicates that metering continues; disallowed exhaustion indicates a blocking usage state through reset.

The Copilot specification does not define exact Notch layout, fractional-value rounding, accessibility labels, announcements, or visual treatment for each status. The implementation must follow existing Notch conventions, and the unresolved product choices are listed for HIL 1 rather than being inferred here.

## Constraints and dependencies

- Windows 11 is the supported environment for the credential paths, local session files, process liveness, and Notch behavior.
- The provider is limited to the lightweight `copilot_internal/user` HTTP path and its borrowed `gh` or Copilot CLI OAuth token.
- The Copilot endpoint is undocumented and unversioned. The `quota_snapshots` map is open and may gain or rename categories.
- The standard rate-limit policy from provider specification 01 supplies persistent deadline evaluation and the `Retry-After` floor behavior. This PRD does not redefine that policy.
- Existing TokenHound contracts and presentation concepts, including Snapshot, Limit Window, Usage Block, Provider Status, Activity Monitor, Usage Archive, Sign-In Route, Notch, and Provider Ring, are dependencies described by the architecture and context documents.
- The repository's Core, Infrastructure, and App boundaries remain in force. The provider must not move UI or OS concerns into Core.
- The product does not define a new archive expiry period here. Existing archive policy applies until product approves a different policy.

## Product gaps and decisions requiring HIL 1

The following questions affect scope or acceptance and are intentionally unresolved:

| ID | Decision needed | Why it cannot be inferred from the specification |
| --- | --- | --- |
| HIL1-01 | Confirm the exact Notch treatment and copy for `Ok`, `Stale`, `Unsupported`, `NeedsAuth`, empty-ring, and overage states. | The specification defines status semantics and sign-in guidance, but not layout, priority, or user-facing status copy. |
| HIL1-02 | Confirm how the fractional `quota_remaining` value is rounded or formatted, and whether the product shows the raw remainder, a percentage, or both. | The specification requires the float as the display source and supplies `percent_remaining`, but it does not define presentation precision or combination. |
| HIL1-03 | Decide whether plan, SKU, login identity, entitlement, `overage_count`, or `token_based_billing` should appear in the user experience. | These fields are described in the response, but only monthly premium interactions, remainder, reset, and status behavior are required by the product input. |
| HIL1-04 | Decide what the user sees when VS Code provides only an identity hint and no usable bearer token exists. | The source explicitly limits VS Code state to an identity hint and does not define the resulting user journey. |
| HIL1-05 | Confirm the product treatment and expiry policy for an archived stale reading across long outages or a reset boundary. | The source requires retaining stale history but gives no retention duration or reset-boundary presentation rule. |
| HIL1-06 | Confirm whether launch success needs a metric beyond the observable acceptance evidence in this PRD. | No product KPI or telemetry requirement is supplied by the authoritative sources. |

Until HIL 1 resolves these choices, this PRD does not assume a rounding rule, add metadata to the Notch, assign a status to an identity-only state, invent archive expiry, or claim a launch KPI.

## Out of scope

- The official Copilot SDK path, including `account.getQuota` RPC and `session.usage.getMetrics`.
- Any Copilot endpoint other than `GET https://api.github.com/copilot_internal/user` for this lightweight provider path.
- Creating, refreshing, re-minting, overwriting, or storing a GitHub credential owned by another tool.
- Browser sign-in loops, TokenHound-owned GitHub accounts, or requests for classic or fine-grained PATs.
- Hard-coded category names as the only parsing path, hard-coded credit conversion, invented denominators, fabricated zero values, or synthetic activity events.
- A provider busy API or PID-scoped activity claim that Copilot does not expose.
- A new Notch design system, new global HUD interaction model, or provider-specific accessibility policy beyond existing Notch conventions.
- A new archive retention period or a new product KPI before HIL 1 approval.

## Assumptions and sources

### Assumptions

- Assumption: The existing Snapshot, Limit Window, Usage Block, Provider Status, Activity Monitor, Usage Archive, Sign-In Route, Notch, and Provider Ring concepts are available for the provider feature. Source: [architecture](../../ARCHITECTURE.md) and [context](../../CONTEXT.md). Impact if wrong: the TechSpec must stop and reconcile the contract before task planning.
- Assumption: Provider specification 01 remains the source of the shared rate-limit policy referenced by the Copilot specification. Source: [Copilot specification, section 6](../../docs/specs/11-PROVIDER-COPILOT.md). Impact if wrong: rate-limit requirements and acceptance must be revalidated before implementation.
- Assumption: Existing Notch conventions include the non-activating and click-through behavior required by the repository. Source: [repository instructions](../../AGENTS.md). Impact if wrong: HIL 1 must define the product interaction and the architecture must be revisited.

### Authoritative sources

- [Copilot provider specification](../../docs/specs/11-PROVIDER-COPILOT.md): provider identity, credential discovery, request and response semantics, parsing, activity monitoring, status mapping, sign-in route, and polling cadence.
- [Architecture](../../ARCHITECTURE.md): layer boundaries, domain concepts, Borrow-Don't-Own, honest telemetry, archive behavior, and Notch context.
- [Repository instructions](../../AGENTS.md): credential read-only behavior, rate-limit deadline invariant, Core purity, and HUD non-activation constraints.
- [Domain context](../../CONTEXT.md): canonical TokenHound terminology and meanings for Provider, Snapshot, Limit Window, Activity Monitor, Notch, Provider Ring, and Borrow-Don't-Own.
- [SDD workflow](workflow.md): feature slug, product-stage boundary, and HIL 1 as the next authorization gate.

## Acceptance criteria

The following scenarios provide the product-level acceptance matrix for the requirements above.

| ID | Scenario | Observable result | Linked requirements |
| --- | --- | --- | --- |
| AC-01 | Finite `premium_interactions` response | The result is `Ok`, identifies Copilot with official fidelity, shows the reported fractional remainder, calculates the used fraction from `percent_remaining`, and uses `quota_reset_date_utc`. | FR-01, FR-04, FR-05, FR-12, NFR-02 |
| AC-02 | Premium category unavailable but another finite open-map category exists | The alternate finite category is selected without relying on a fixed key. | FR-04, NFR-04 |
| AC-03 | Only unlimited or non-entitled categories exist | Those categories are omitted and the provider returns `Unsupported` with an empty ring rather than a zero value. | FR-04, FR-12, NFR-02 |
| AC-04 | 401, PAT-shaped 403, and unlicensed or no-seat 403 | 401 and PAT-shaped 403 produce `NeedsAuth`, discard history, and show the terminal guidance. Unlicensed or no-seat 403 produces `Unsupported` and discards history. | FR-07, FR-08, NFR-01 |
| AC-05 | Timeout, missing map, moved key, malformed schema, or unhandled non-200 | The provider returns `Stale`, retains the archived reading, and never substitutes fabricated zeros. | FR-07, NFR-02, NFR-04 |
| AC-06 | 429 with a positive or zero retry value | The provider returns `Stale`, retains the reading, persists a deadline, and dispatches no request before that deadline. A zero retry value never causes an immediate retry. | FR-07, FR-11, NFR-03 |
| AC-07 | Activity signal combinations | Recent write plus live qualifying host is `Busy`; either condition absent is `Idle`; no-write activity becomes `Idle` after 30 seconds; burst writes are debounced at 120 ms. | FR-09, FR-10, NFR-05 |
| AC-08 | Exhausted finite quota with and without permitted overage | Permitted overage produces a non-blocking metered-overage usage block. Disallowed exhaustion produces a blocking usage block through the reported reset. | FR-06, FR-12 |
| AC-09 | Multiple credentials and read-only discovery | The required precedence selects the first usable token, VS Code state remains an identity hint, and no credential or local state file is changed. | FR-02, NFR-01 |
| AC-10 | Active, idle, forced-refresh, and Notch interaction states | Quota polling uses 60 seconds when busy and 300 seconds when idle, forced refresh respects a deadline, and interacting with the Copilot indicator does not steal foreground focus. | FR-11, FR-12, NFR-03, NFR-05, NFR-07 |

## PRD acceptance gate

- [x] Every functional and non-functional requirement has a stable ID and an observable acceptance criterion.
- [x] Outcomes, boundaries, failure states, fidelity, security, rate-limit, activity, and out-of-scope behavior are explicit.
- [x] Repository rules are attributed to the repository instructions or architecture, and Copilot behavior is attributed to the authoritative provider specification.
- [x] Implementation structure and sequencing remain deferred to the TechSpec and task stage.
- [ ] HIL 1 product decisions are resolved. The coordinator must present HIL 1 and record the decision before downstream artifacts are created.
