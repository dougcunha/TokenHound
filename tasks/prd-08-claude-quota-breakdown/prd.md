# PRD — Claude quota breakdown

## Problem and context

TokenHound currently displays only Claude's five-hour session and seven-day overall usage. Claude Max and other eligible accounts can receive additional, separate usage windows for a model or scope. A user cannot see those limits in TokenHound even when the Claude usage response supplies them.

The current account's read-only usage response included `session`, `weekly_all`, and `weekly_scoped` entries in `limits`. It also included optional model-specific fields such as `seven_day_opus` and `seven_day_sonnet`; those fields were null in that reading. The response did not identify a quota named “Fable”. The feature must follow the quota data actually reported for each account rather than assume a fixed set of models or that every Max account has the same limits.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Users can see every identifiable Claude usage window reported for their profile. | A response containing session, overall weekly, and at least one additional model or scoped quota produces a distinct visible row for each applicable window. |
| OBJ-02 | The existing session reading retains its meaning. | The five-hour session remains the primary Claude usage window, including when its `limits` entry disappears at rollover but the top-level session value remains. |
| OBJ-03 | Optional quotas do not create misleading readings. | Null or absent quotas produce no row; a reported 0% quota produces a row; no count or limit is inferred from a percentage alone. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Claude Max user | See separate quotas for models or scopes available to the current account | Know which quota is approaching exhaustion | Open Claude details and see the session, overall weekly, and each reported additional quota with distinct labels. |
| US-02 | User whose account has no additional quota | See only applicable limits | Avoid empty or false model rows | Open Claude details when optional quota fields are null or absent and see only the reported session and overall weekly limits. |
| US-03 | User with more than one Claude profile | See the quota breakdown for the selected profile | Avoid confusing limits between accounts | Switch between Claude profiles and see each profile's own reported windows. |
| US-04 | User at a session rollover or during an API rate limit | Keep the correct context for the last valid reading | Avoid mistaking a weekly quota for the session quota | Observe the session in its usual position at rollover and retain the last successful windows while the provider is rate limited. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Show additional model-specific or scoped Claude quota windows when the usage response reports a usable utilization percentage. | A response with `weekly_scoped`, `weekly_opus`, or another identifiable quota entry shows each as a separate row alongside the session and overall weekly rows. A valid 0% is shown. |
| FR-02 | Use meaningful, distinct labels that reflect the reported quota kind and scope. | Users can distinguish the session, overall weekly, and additional quota rows; a model name is shown only when the response identifies that model. An unfamiliar but identifiable scope has a neutral label rather than a guessed model name. |
| FR-03 | Do not show quotas that are absent, null, or lack usable usage data. | A response with null `seven_day_opus` and `seven_day_sonnet` values has no Opus or Sonnet rows unless another valid response entry reports those quotas. |
| FR-04 | Combine duplicate representations of the same window into one reading. | A quota present in both `limits` and a top-level field appears once, with one percentage and reset time. The session and overall weekly windows remain visible if their `limits` entries are temporarily absent but their top-level values are available. |
| FR-05 | Preserve a stable display order and primary metric. | The five-hour session precedes the overall weekly limit; additional windows follow in a deterministic order and never replace the session as the primary metric. |
| FR-06 | Show only measurements supported by the response. | A reported percentage appears as percent used; a reset time appears only when supplied; an amount remaining or a total appears only when the API supplies that amount or total. |
| FR-07 | Apply the breakdown independently to every registered Claude profile. | Two profiles with different optional quotas show different rows without copying values or labels between profiles. |
| FR-08 | Preserve the existing degraded-state behavior for the full breakdown. | After a successful reading with additional windows, an HTTP 429 retains those windows as stale data and respects the existing retry deadline. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Credential safety | Read Claude credentials only with the repository's shared read access; do not write or refresh tokens. |
| NFR-02 | Network discipline | Obtain the extra windows from the existing usage response, without additional polling; respect persisted 429 deadlines before dispatch. |
| NFR-03 | Data integrity | Do not invent a quota denominator, remaining count, reset time, model identity, or available quota from a null field. |
| NFR-04 | Accessibility | Each visible quota has readable text identifying its scope and usage without relying on color alone; rows remain understandable in the existing HUD details view. |
| NFR-05 | Architecture | `TokenHound.Core` remains free of UI and OS dependencies. |

## User experience

Claude details continue to lead with the five-hour session and overall seven-day usage. Additional quota rows appear beneath them only when reported, each with its own label, used percentage, and reset countdown if available. A 0% reported quota is visible. No placeholder row appears for a null model quota. The profile name remains visible so the user can tell which account the breakdown belongs to.

## Constraints and dependencies

- The Claude provider behavior and session rollover rule are documented in `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3.
- The account's OAuth usage endpoint is undocumented and can change shape; optional response fields must be handled without assuming they exist for all users.
- Existing multi-profile behavior from `tasks/prd-07-claude-multi-profile/prd.md` remains in effect.
- Repository credential, rate-limit, and C# constraints in `AGENTS.md` apply.

## Out of scope

- Changing Claude subscription entitlements, model selection, or quota resets.
- Displaying monetary extra-usage credits or spend as model quota rows.
- Adding support for a model called “Fable” without an identifiable quota for it in the API response.
- Changing the HUD's overall layout or adding a new view.

## Assumptions and sources

- User request: Claude Max can have separate model quotas, and TokenHound should show other quotas when they exist. “Fable” is treated as an example, not a verified API identifier.
- Local observation on 2026-09-23: a single read-only request for the current account returned `limits` kinds `session`, `weekly_all`, and `weekly_scoped`; optional `seven_day_opus` and `seven_day_sonnet` fields were present but null. No credential or raw response is stored in this PRD.
- Repository source: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs` currently declares only `five_hour` and `seven_day`; `ClaudeOAuthProvider.MapLimitWindows` maps only those two fields.
- Repository source: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3 describes `limits`, model-specific weekly quotas, and the session rollover fallback.
- Product decision for approval: additional identifiable percentage-based quotas belong in the existing Claude details breakdown; monetary spend is a separate concern.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
