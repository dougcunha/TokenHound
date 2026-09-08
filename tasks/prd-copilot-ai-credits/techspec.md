# TechSpec: Copilot AI credits

Feature slug: `copilot-ai-credits`
Status: Draft for technical review; external evidence gates remain open.
Operation: Create. No existing TechSpec was present at this destination.

## Sources and traceability

- Product authority: [PRD](prd.md), all FR-01 through FR-13 and NFR-01 through NFR-06, including its decisions D1-D3 and assumptions A1-A2.
- Supporting plan: [Copilot AI Credits adjustment plan](../../docs/COPILOT-AI-CREDITS-PLAN.md).
- Repository constraints: [architecture](../../ARCHITECTURE.md), [Copilot specification](../../docs/specs/11-PROVIDER-COPILOT.md), [reading and resilience specification](../../docs/specs/01-READING-STRATEGY-RESILIENCE.md), and [HUD design](../../docs/design/2026-08-28-usage-notch-design.md). The PRD resolves the old specification's internal-quota/billing terminology conflict for this feature.
- Applicable instructions: supplied repository instructions, `sdd-create-techspec` and its .NET profile/template, `repository-cli-efficiency`, and `dotnet-efficient-validation` with its MTP reference. No implementation or task plan is included.
- Code evidence: `Snapshot`, `LimitWindow`, `SnapshotRetentionPolicy.Apply`, `CopilotUsageProvider.GetSnapshotAsync`, `CopilotApiClient.GetQuotaAsync`, `UsageStore.RefreshProviderAsync`, `UsageArchive`, `ProviderRingViewModel.FindLimitWindow`, `TooltipCard`, and `ProviderRing.xaml` at the paths below.

### Primary integration references

Consulted on 2026-09-07. These validate endpoint contracts, not the current account's permissions or billing owner.

- [GitHub user/organization billing API](https://docs.github.com/en/rest/billing/usage?apiVersion=2026-03-10): separate billing-owner routes, version/header conventions, period filters, and quantity fields. Personal endpoints apply to personally purchased plans; managed usage belongs to managed endpoints. Fine-grained permissions include user Plan read and organization Administration read.
- [GitHub enterprise billing API](https://docs.github.com/en/enterprise-cloud@latest/rest/billing/usage?apiVersion=2026-03-10): enterprise route, Enterprise billing read, and enterprise-specific filters.
- [Copilot user-management API](https://docs.github.com/en/rest/copilot/copilot-user-management): preview organization seat/plan context. Its example exposes plan and seat breakdown, which are not an allowance or proof of this user's billing owner.
- [Copilot report API](https://docs.github.com/en/rest/copilot/copilot-usage-metrics): daily user-report manifests, signed downloads, and separate organization/enterprise authorization.
- [Copilot report fields](https://docs.github.com/en/copilot/reference/copilot-usage-metrics/copilot-usage-metrics): per-user `ai_credits_used`, user identifiers, day, and scope fields. This consumption measure is not an invoicing total.

## Solution summary

Add an optional typed Copilot billing value to `Snapshot`, leaving operational quotas in `LimitWindows`. Use decimal quantities and explicit owner, reporting period, coverage, provenance, and freshness. A pure policy aggregates compatible credit data and conditionally calculates a balance. No currently inspected response establishes an authoritative allowance field, so the production path initially exposes usage and unavailable total/remaining. A test fixture can exercise a verified allowance without inventing a production JSON mapping.

Integrate an independently cached billing service into the existing Copilot provider. Retain the generic quota status/history behavior while ensuring it cannot discard an incoming billing result. Introduce an opt-in request-gating contract because Copilot now dispatches multiple requests per refresh and must persist a 429 before its next request. Render a collection of semantic detail rows in the existing tooltip, with no new account picker, billing administration, SDK, package, or project.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-04, FR-05, FR-07, FR-08, FR-09; NFR-04, NFR-06 | Add `Snapshot.CopilotBilling` with a dedicated immutable billing model and a pure `CopilotCreditPolicy`; leave `LimitWindow` unchanged. | `LimitWindow` uses integer totals, fractional double remainder, and one reset; it cannot represent billing dimensions or independent provenance. | Extending generic quota fields would spread billing semantics to unrelated providers. A provider-specific optional property is a deliberate small contract addition. |
| DEC-02 | FR-02, FR-03, FR-12; NFR-01 | Resolve a verified principal and billing context before requesting billing. Treat internal plan/org fields as hints until their mappings are evidenced. | `CopilotQuotaResponse` currently contains only reset and quota map. Membership and successful organization administration access do not prove who pays for the user's license. | Do not enumerate or combine arbitrary billing accounts, add a selector, or infer ownership from token capability. Missing production evidence is OI-01, not an implicit personal fallback. |
| DEC-03 | FR-04, FR-05, FR-06, FR-09 | Prefer direct current-period billing. Fall back to daily per-user reports only for a resolved scope that actually offers them. Keep these source families separate. | Gross billing quantities and per-user consumption reports have different fidelity. | No sum of billing plus reports, no subtraction of overlapping 28-day totals, and no invented personal metrics route. |
| DEC-04 | FR-07, FR-08, FR-09; NFR-04 | Enable allowance extraction only for a documented, response-verified mapping. Use total minus gross consumption with full compatibility checks. | Existing documentation exposes quantities and seat context but no validated allowance input. | A policy table or configuration override would violate the PRD. Unknown reset and allowance remain null, including at period rollover. |
| DEC-05 | FR-10, FR-11, FR-12; NFR-03, NFR-06 | Persist billing independently through `UsageArchive`; attach the latest billing state after quota retention. | `SnapshotRetentionPolicy` replaces an entire stale snapshot with the last good snapshot and clears quota history for NeedsAuth/Unsupported. `UsageStore` archives only Ok snapshots. | One combined last-good snapshot loses a successful billing fetch when quota fails. Broadly changing generic provider status semantics is unnecessary. |
| DEC-06 | FR-10, FR-12; NFR-02, NFR-03 | Copilot opts into request-level gating; all its network sends share a conservative persisted GitHub deadline. Reuse `RateLimitPolicy` and `BackoffCalculator`. | The store currently checks before the provider call and persists only after it returns; it cannot gate a second request following a 429. Its `ActiveBlock` also includes quota exhaustion, which is not an HTTP deadline. | Shared gating may temporarily defer both sources after a 429, but independently valid readings survive. Guessing that endpoints have independent rate-limit buckets is rejected. |
| DEC-07 | FR-06, FR-10; NFR-03 | Keep existing polling cadence; limit a billing pass to 15 seconds and four network dispatches, including context, manifests, and downloads. Resume partial report work on later scheduled refreshes. | Existing quota calls use a 15-second bound; `UsageStore` refreshes providers serially. An unbounded month backfill would delay every subsequent provider. | These are initial technical resource limits, not GitHub limits or product quotas. Validate progress and response sizes under OI-03; partial work must remain visible. |
| DEC-08 | FR-01, FR-09, FR-13; NFR-05, NFR-06 | Replace fixed tooltip slots with typed rows, while retaining the current primary quota ring selection. | `FindLimitWindow` falls back to window index 0/1 and the XAML labels both as session/weekly. | AI-credit consumption without an allowance never becomes a ring percentage. Do not redesign the HUD to solve a data-label issue. |
| DEC-09 | All FR and NFR | Use pure policy tests, fake-HTTP contract tests, real temporary-file persistence tests, linked ViewModel tests, and manual HUD evidence. | Existing Infrastructure tests link App ViewModels without referencing WPF; CI runs both test projects as MTP executables. | Desktop E2E is omitted by the skill's .NET profile. This does not waive layout/focus acceptance or real-response validation. |

## Components and flow

All paths are relative to the repository. New records/enums below each use their own file; no new package dependency is proposed.

| ID | Component | New or modified | Responsibility | Integration |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Core/Models/Snapshot.cs`; new `Models/CopilotBillingStatus.cs`, `CopilotCreditUsage.cs`, `CopilotBillingContext.cs`, `CopilotBillingPeriod.cs`, `CopilotReportCoverage.cs`, `CopilotAllowanceEvidence.cs`, and supporting enums | Modified/new | Nullable billing contract independent of quota, UI, HTTP, and OS. | Provider, archive, retention overlay, row projection. |
| CMP-02 | `src/TokenHound.Core/Policies/CopilotCreditPolicy.cs` | New | Validate dimensions, combine compatible amounts, enforce allowance evidence, calculate remaining/fraction. | Called after transport parsing; tested in Core.Tests. |
| CMP-03 | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingContextResolver.cs`; `CopilotQuotaResponse.cs` | New/modified | Normalize identity/plan hints and verified ownership evidence; return resolved/unknown/ambiguous context. | Reuses credential discovery and gated context reads; OI-01 governs concrete evidence mappings. |
| CMP-04 | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`, `CopilotBillingResponse.cs`, `CopilotBillingUsageItem.cs`, `CopilotSeatResponse.cs` | New | Versioned GET requests, nullable DTOs, scope/period validation, billing and seat responses. | Gate CMP-06; service CMP-05; pure policy CMP-02. |
| CMP-05 | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.cs`, `CopilotMetricsClient.cs`, `CopilotMetricsReportParser.cs`, `CopilotMetricsManifest.cs`, `CopilotMetricsUserRow.cs` | New | Source preference, bounded report progress, streamed NDJSON, independent cache and error state. | Consumed by existing `CopilotUsageProvider`; uses CMP-02/03/04/06/07. |
| CMP-06 | `src/TokenHound.Core/Contracts/IRequestGatedUsageProvider.cs`; `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`; `CopilotApiClient.cs`; `Engine/UsageStore.Refresh.cs` | New/modified | Opt-in dispatch ownership, shared deadline, persistence before subsequent sends, existing policy math. | Only Copilot opts in. Other providers keep existing store gating. |
| CMP-07 | `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBilling.cs`; `UsageArchive.cs`; `UsageArchive.Persistence.cs` | New/modified | Atomic billing/day-summary state and gate state using the existing archive instance and lock. | Inject same instance into store and Copilot service/gate at composition. |
| CMP-08 | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs`; `src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs`; `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs` | Modified | Keep quota mapping, attach independently retained billing, avoid generic snapshot replacement of new billing. | Exposes one provider ID/event; no second Copilot ring. |
| CMP-09 | `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `ProviderUsageRowFactory.cs`; `ProviderRingViewModel.cs`; `ProviderRingViewModel.Status.cs` | New/modified | Pure typed row projection with an injected clock, scope/source/error text, and existing primary-ring behavior. | `NotchViewModel` already marshals snapshot updates via its dispatcher callback. |
| CMP-10 | `src/TokenHound.App/UI/Controls/TooltipCard.xaml`, `TooltipCard.xaml.cs`, `ProviderRing.xaml`; `src/TokenHound.App/App.xaml.cs` | Modified | Bind row collection; compose owned dependencies and disposal; preserve tooltip host and interop. | CMP-09 rows; same archive instance shared with CMP-07. |

Flow: scheduled/manual refresh enters Copilot, reads the borrowed credential, and establishes an attempt identity. A gated quota attempt produces its ordinary success/error result. The billing service independently loads eligible cached data, resolves billing context, and performs a bounded billing/report pass. The provider returns the quota result plus billing state. Generic retention operates on quota; an explicit overlay preserves incoming billing. The existing snapshot event reaches the dispatcher and replaces the row collection atomically.

Quota transport/parse failure must not skip billing when a previously verified context is still valid for the same credential generation. A cold start without verifiable identity/context cannot retrieve billing speculatively. Shared 429 gating may stop subsequent calls in this flow; that is a rate-limit condition, not source-result coupling.

## Contracts and data

### Public domain contract

`Snapshot.CopilotBilling` is an optional `CopilotBillingStatus?` init property. Null means no billing capability/state was attached, as in old archives and non-Copilot providers. On a new Copilot attempt it is populated even when unavailable, so the UI can explain the failure. `Snapshot.Status`, `Fidelity`, `FetchedAtUtc`, `LimitWindows`, and `ActiveBlock` retain operational-quota meaning; they are not the billing freshness/status authority.

| Record | Fields and types | Requiredness and validation |
| --- | --- | --- |
| `CopilotBillingStatus` | `State` enum (Available, Stale, Unavailable); `Reason` enum (None, MissingCredential, UnknownScope, AmbiguousScope, AccessDenied, ReportUnavailable, RateLimited, NetworkFailure, InvalidData, PersistenceFailure); `Usage: CopilotCreditUsage?`; `AttemptedAtUtc: DateTimeOffset`; `NextRequestAtUtc: DateTimeOffset?` | State, reason, and attempt time required. Available requires a valid usage object, which may explicitly have partial coverage. Stale preserves original usage metadata. No raw response body in diagnostics. |
| `CopilotBillingContext` | `PrincipalId: string`; `Scope: CopilotBillingScope` (Personal, Organization, Enterprise, Unknown); `OwnerId: string?`; `OwnerName: string?`; `Plan: CopilotPlanType`; `RawPlan: string?`; `EvidenceKey: string?` | Principal is verified before reuse of cached values. Resolved context requires stable owner identity and verified evidence. Plan is independent of scope, with Unknown for unrecognized values. Unknown context has no requestable billing owner. |
| `CopilotBillingPeriod` | `StartUtc`, `EndExclusiveUtc`, `ResetUtc`: nullable `DateTimeOffset`; `RequestedYear`, `RequestedMonth`: integers; `IsVerified: bool` | Record the requested window separately from what the response proves. Known start must precede end. A year-only response must not become a verified monthly interval unless the documented filter contract plus captured response establishes that interpretation. Reset is not automatically end. |
| `CopilotCreditUsage` | `Context`, `Period`, `Coverage`; `GrossUsed`, `DiscountedUsed`, `NetUsed`, `IncludedTotal`, `Remaining`, `UsedFraction`: nullable decimal; `Allowance: CopilotAllowanceEvidence?`; `Source: CopilotCreditSource` (BillingApi, DailyUserReport); `SourceAsOfUtc: DateTimeOffset?`; `FetchedAtUtc: DateTimeOffset`; `IsEstimated: bool` | Context, source, fetch time, period, and coverage required. Each absent quantity stays null. Monetary amount fields are not quantities. Daily reports set estimated fidelity; derived remaining inherits input limitations. Never substitute fetch time for source reporting time. |
| `CopilotReportCoverage` | `StartDay`, `EndDay`: nullable `DateOnly`; `IsComplete: bool`; `MissingDays: IReadOnlyList<DateOnly>`; `Filters: IReadOnlyDictionary<string,string>`; `HasMissingPartitions`, `HasInvalidRows`: bool | Completeness describes the requested reporting coverage, not real-time currency. Known valid subsets may be reported as partial; incomplete inputs cannot calculate a full-period remaining value. |
| `CopilotAllowanceEvidence` | `JsonPath`, `DocumentationUrl`, `EvidenceKey`, `Unit`: strings; `Context`, `Period`; `Value: decimal` | Construct only from an approved verified mapping, never merely an extension-data key or a test-only fixture. Context/period/unit must match usage; negative allowance invalid. Production mapping is absent; current evidence closes OI-02 as consumption-only, and any future allowance requires a newly documented and verified mapping before balance activation. |

Use sealed records with init properties and required members, nullable transport numbers, invariant JSON parsing, checked decimal arithmetic, and case-insensitive ordinal identifier comparison. Overflow, inconsistent dates, and unsupported quantities produce InvalidData for the affected dimension/source. Zero is valid only when explicitly supplied or when a verified complete empty-report contract establishes zero; an empty or absent payload alone does not do so.

Policy behavior: aggregate gross, discount, and net separately. If a dimension is missing in any included billing item, its complete aggregate is null; other complete dimensions remain usable. Do not deduplicate legitimate billing rows merely because numeric values match. A billing response replaces the prior aggregate, never adds to it. For compatible verified allowance and complete gross usage, remaining is their signed difference; overage can produce negative remaining. Fraction is null unless total is positive. Keep the unbounded numerical ratio as data; any visual progress fill can clamp to its track without altering displayed usage or remaining. Never calculate a fraction through generic `LimitWindow.TotalUnits` for AI credits.

Example: a verified allowance fixture of 5,700 and gross usage 725 yields remaining 4,975. With the same usage and no approved allowance mapping, IncludedTotal, Remaining, and UsedFraction are null. Internal quota fixtures cannot construct `CopilotAllowanceEvidence` or credit consumption.

### Transport and context contracts

Billing DTOs preserve `timePeriod`, the returned owner field, and `usageItems` with `product`, `sku`, `model`, `unitType`, and nullable decimal `grossQuantity`, `discountQuantity`, `netQuantity`. Only verified Copilot AI-credit product/SKU/unit combinations enter the policy. Preserve unknown fields for diagnostics only when sanitized; do not use them to discover allowances automatically.

The internal quota DTO may add nullable `login`, `copilot_plan`, `access_type_sku`, and `organization_login_list` hints, using captured field names and types. Existing quota parsing is unchanged. A plan name, organization list, seat breakdown, or successful billing-admin request alone cannot satisfy ownership proof. The resolver returns unknown until a documented mapping plus real account evidence can bind the user's licensed scope. For multiple candidates, no selected owner is returned. Explicit, verified enterprise ownership supersedes organization hints. Exact production evidence mappings are OI-01; adding an unsupported `enterprise` JSON key or a hidden user configuration is not an acceptable implementation.

Keep credentials out of all domain models and persisted cache keys. Read once per attempt from `CopilotCredentialDiscovery`; do not walk lower-priority credentials after a billing 403. A credential generation change invalidates in-memory context until principal/ownership is revalidated. A failed discovery never reuses another account's cached billing reading.

## Integrations and interfaces

### Read-only API routes

| Purpose | GET route | Input/output and authorization |
| --- | --- | --- |
| Personal billing | `/users/{username}/settings/billing/ai_credit/usage` | Verified personal owner; year/month plus supported product/model filters; direct billing DTO. User Plan read applies to fine-grained access. |
| Organization billing | `/organizations/{org}/settings/billing/ai_credit/usage` | Verified organization billing owner; direct billing DTO. Organization Administration read applies to fine-grained access. |
| Enterprise billing | `/enterprises/{enterprise}/settings/billing/ai_credit/usage` | Verified enterprise owner; direct DTO. Enterprise billing read applies; enterprise-only organization/cost-center narrowing must remain in coverage metadata. |
| Organization plan context | `/orgs/{org}/copilot/billing` | Seat/plan metadata only. Preview endpoint; organization ownership/appropriate Copilot or administration permission required. Failure does not discard independently valid usage. |
| Organization daily user metrics | `/orgs/{org}/copilot/metrics/reports/users-1-day?day=YYYY-MM-DD` | Manifest with `report_day` and `download_links`; organization metrics authorization is distinct from billing authorization. |
| Enterprise daily user metrics | `/enterprises/{enterprise}/copilot/metrics/reports/users-1-day?day=YYYY-MM-DD` | Same manifest pattern under enterprise metrics authorization. Do not infer access from the billing call. |

Versioned API requests send Bearer authorization, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2026-03-10`, and the existing `TokenHound/1.0` User-Agent. The internal quota request keeps its current contract. Use encoded path segments and endpoint-specific query builders. Default to the full verified owner scope and explicit UTC year/month. No narrowed consumption is combined with a full-pool allowance. Personal has no assumed historical user-report fallback; managed fallback remains required where accessible.

Signed downloads use a separate HTTP request without GitHub bearer headers, cookies, or credential-bearing default headers. Accept HTTPS report URLs issued by the trusted manifest; reject local/private destinations and unsafe redirects. Never log or persist signed URLs. Stream and dispose responses/NDJSON, propagate cancellation, and count every redirect/download against the pass budget. Host validation and payload limits must be validated against real manifests under OI-03, not bypassed to accept arbitrary endpoints.

### Historical report processing

Parse per-user `day`, `user_id`, `user_login`, `enterprise_id`, optional `organization_id`, and decimal `ai_credits_used`. Do not sum feature/model breakdowns with this already-total quantity. Each day is committed only after every manifest partition has completed and scope/date checks have passed. Deduplicate identical rows by scope/day/user; conflicting duplicates invalidate that day. Missing user rows cannot be interpreted as zero without a verified report-completeness contract.

Persist normalized day aggregates and coverage, not signed manifests or raw user rows. Replace a day's result atomically when refreshed. Sum only distinct compatible day aggregates. Resume oldest missing days first, then revalidate completed days in a persisted rotating order so corrections eventually replace cached figures. Allocate pending report work a dispatch before reissuing an already-failed billing probe on later passes; manifests with several partitions must progress across passes. Preserve incomplete partition work as non-displayable temporary aggregate state and never mark it complete until all partitions for the same manifest generation validate. If a manifest expires or changes, discard only that uncommitted work and restart that day.

No whole 28-day total is converted into a monthly total. Daily reports are the selected fallback integration; unsupported or unavailable days remain gaps. A complete set through the latest processed day still carries its historical source and reporting cutoff. It is not described as an invoice or live balance.

### Gating, persistence, and retention

`IRequestGatedUsageProvider : IUsageProvider` is an opt-in marker contract: the provider owns persistent checks before every network send and must not rely on `ActiveBlock` for HTTP dispatch. In `UsageStore`, bypass outer `IsRateLimited` and `PersistRateLimitDeadlineAsync` only for this contract. Copilot's gate must be present in its default and injected construction paths; other providers retain the current behavior. This avoids blocking billing until a premium quota reset simply because the operational quota is exhausted.

`CopilotRequestGate` serializes check/send/429-update for Copilot requests. Load and honor legacy `backoffUntil.copilot` deadlines conservatively until expiry. Persist new gate state in `state.json` under `copilotHttp`, with `deadlineUtc` and `consecutiveFailures`, through the same `UsageArchive` lock. Existing archive writes must preserve this sibling property. Use `RateLimitPolicy.CalculateDeadline`, its existing minimum floor and jitter; never lower an active deadline. On 429, persist before releasing the gate or dispatching a fallback request. No immediate retries. If deadline persistence fails, retain the in-memory block and disable further Copilot sends for that process while surfacing PersistenceFailure; do not claim restart durability in that case. Success after the deadline resets the streak. Forced refresh cannot bypass the gate.

Add versioned `%LOCALAPPDATA%/TokenHound/copilot_billing.json` through a new `UsageArchive` partial. Store the latest valid reading and completed-day summaries keyed by principal, owner scope/identifier, exact period, and normalized filters. Reuse `System.Text.Json`, the archive semaphore, same-directory temporary write, and atomic replacement patterns. Store only the current and immediately preceding period per active verified context; never substitute a prior owner's state. Persistence failures preserve in-memory valid values with an explicit diagnostic. Unknown/corrupt billing schema disables only billing-cache restore, without erasing quota/deadline files.

The archive remains a single owned instance shared by the store, gate, and billing service. Child services do not dispose that instance. `App.xaml.cs` composes it before starting refresh and owns HTTP/service lifetime through the existing disposal list; cancellation and shutdown finish before disposal.

Quota retention must explicitly preserve `incomingSnapshot.CopilotBilling` after applying the old status/window rules in every branch, including Stale, NeedsAuth, and Unsupported. The prior generic snapshot must never resurrect a billing payload when incoming billing is absent. `UsageStore.CreateErrorSnapshot` can retain known billing only as stale for the same context. Strip the optional billing payload when writing generic `last_readings.json`: the independent billing archive is authoritative. On startup, old quota archives deserialize with a null billing property; the first Copilot attempt attaches independently restored, identity-eligible billing. Billing service commits a successful result independently of whether quota status is Ok.

Caller cancellation propagates, but a completed source is committed before abandoning unfinished work. Keep the last successful quota result and its credential/context generation in private provider state before starting billing; when cancellation arrives after quota success but before billing returns, the next eligible attempt can recover it without fabricating freshness. Clear that state on generation change and preserve existing quota auth/history suppression. Local billing-budget expiration is a billing outcome, not caller cancellation, and returns the already-completed quota result. No fire-and-forget report workers or WPF thread work are introduced.

## Errors, security, and recovery

| Condition | Billing outcome | Quota/provider consequence |
| --- | --- | --- |
| No credential, unknown principal, or owner change | Unavailable; suppress any prior account/context reading. | Existing quota authentication behavior remains. |
| Unknown/ambiguous owner | Unavailable with scope reason; no speculative billing request. | Valid quota stays visible. |
| Billing 401/403 or policy denial | Same-context last value may remain explicitly stale; otherwise unavailable. No credential replacement and no assertion of revoked subscription. | Do not rewrite a successful quota status. |
| Report 204/404, missing partitions, partial days | Report unavailable or partial coverage; no fabricated zero. | Other source results remain usable. |
| 429 from any Copilot request/download | Persist shared gate; retain valid data stale with next-request time. | Subsequent network calls defer; previously obtained quota is retained. |
| Timeout, network error, schema mismatch | Preserve eligible last success as stale; unavailable without one. Invalid new data does not replace valid history. | An independent successful source remains successful. |
| Period mismatch or missing allowance | Usage may remain visible with its actual period/coverage; no total-dependent calculation. | Internal quota reset never repairs billing metadata. |
| Overage or zero allowance | Preserve actual gross/net values and signed remaining when calculable; fraction null for zero total. | Do not create an operational hard block from credit consumption. |

Log structured reason, endpoint family, HTTP status, duration, coverage state, and whether a reading was retained. Avoid bearer tokens, raw error bodies, signed URLs, and per-user report rows. File access to credentials continues through existing read-only discovery. API and report parsing must remain in Infrastructure with `.ConfigureAwait(false)`; Core contains only records/contracts/policies. No SQLite or credential format migration is needed.

Concurrency is bounded by the existing refresh lock plus the Copilot request gate and archive lock. Acquire the gate before sending, then persist through the archive; archive operations must never call back into the gate. Use request-local linked cancellation sources and dispose them. A response for an outdated credential/context generation is discarded before cache commit or presentation.

## Presentation

`ProviderUsageRow` is a WPF-free immutable view model with key, label, optional fraction, primary/secondary quantity text, scope text, reset text, source/coverage/freshness text, and unavailable/error text. `ProviderUsageRowFactory` receives snapshot and clock. `ProviderRingViewModel` exposes a replaceable `IReadOnlyList<ProviderUsageRow>` and raises one property notification on update through the existing dispatcher path.

One row is generated per actual operational window, using the window name/period rather than positional session/weekly guesses. For the finite Copilot premium category use `Premium interactions`; preserve the actual category label for a fallback non-premium quota. Add a distinct `AI credits` row with independent status. Existing primary quota ring selection stays intact; AI-credit usage is not a new ring denominator. Remove fixed tooltip session/weekly properties and update their only inspected caller, `ProviderRing.xaml`, together; retain unrelated public ViewModel compatibility only where a caller/test still needs it.

Use an `ItemsControl` in `TooltipCard` with templates for quantities, provenance, optional progress, and errors. Keep current tooltip host, colors, width bounds, and interaction settings; use wrapping and measured content height. Unknown fractions omit the bar fill and show an explicit unavailable label. Display complete decimal values without converting them to integer credits. Long owner names and status text wrap. Supply text/automation names for row labels and values; do not add focusable controls solely to display numbers.

`WM_MOUSEACTIVATE` must continue returning `MA_NOACTIVATE` (3). Keep `SWP_NOZORDER` and omit `SWP_SHOWWINDOW` in `WindowStyles.EnableNonActivating`. New rows must not introduce window activation, clipping, or hidden overflow. Manual geometry evidence is required; no UI redesign or animation change is selected here.

## Test approach

### Validation profile

- Effective local SDK: `rtk dotnet --version` returned `10.0.400`. `global.json` requests `10.0.400` with `latestFeature` roll-forward and Microsoft.Testing.Platform.
- Core and Infrastructure: `net10.0` class libraries. App: `net10.0-windows`, `OutputType=WinExe`, `UseWPF=true`, with WPF startup/composition in `App.xaml.cs`. This is a desktop application, not a web target.
- Both test projects target `net10.0`, set `OutputType=Exe` and `UseMicrosoftTestingPlatformRunner=true`, and reference `xunit.v3.mtp-v2` 4.0.0. Infrastructure tests already link App ViewModel source; add explicit compile links for new WPF-free row files. No App/WPF project reference is needed in tests.
- No repository `Directory.Build.props`, `Directory.Build.targets`, or `Directory.Packages.props` was found in the inspected source/test/workflow scope; the root `Directory.Build.props` is absent. Existing project files use SDK imports. Recheck evaluated properties if these inputs change.
- `.github/workflows/ci.yml` uses project-scoped MTP executable runs because native `dotnet test` orchestration previously discovered zero tests with this host. Preserve the established `dotnet run` route; do not migrate runners or repeat that failed route by default.
- `E2E: omitted by desktop .NET policy`. The profile reference also phrases this as `E2E: omitted by .NET desktop policy`. No full-application, browser, WebView, or automated desktop E2E execution is selected. Do not delete existing suites.
- Current evidence is source inspection and SDK detection only. No build, test execution, account billing capture, or manual HUD acceptance was performed for this documentation change.

### Test matrix

Project abbreviations: Core = `tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj`; Infra = `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`.

| ID | Obligations | Level | Scenario and expected evidence | Project/filter |
| --- | --- | --- | --- | --- |
| TC-01 | FR-01; NFR-06 | Unit/regression | Internal entitlement 20,000/credits used 725 never populate billing; existing finite quota selection, reset, overage, and unknown-category behavior stay intact. | Infra, `*Copilot*` |
| TC-02 | FR-02, FR-03, FR-12 | Unit + real-response review | Personal/org/enterprise verified evidence, unknown plan, membership-only, multiple orgs, explicit enterprise precedence, credential generation change. Invalid evidence dispatches no billing call. Synthetic cases do not close OI-01. | Infra, `*Copilot*`; OI-01 evidence |
| TC-03 | FR-04, FR-05; NFR-04 | Contract/unit | Correct scope route/headers/filters; direct decimal DTOs; mixed products/units; gross/discount/net independently summed; missing one dimension; year-only/mismatched period; overflow. | Infra, `*Copilot*`; Core, `*Copilot*` |
| TC-04 | FR-06, FR-09; NFR-04 | Contract + file integration | Multiple NDJSON partitions, duplicate/conflicting users, missing rows/days, wrong scope/day, 204/404, expired links, and revised reports. Commit complete days once; show partial coverage; never merge daily/billing/28-day totals. | Infra, `*Copilot*` |
| TC-05 | FR-07, FR-08; NFR-04 | Unit + external evidence | No allowance, undocumented key, seat-only, policy-only, verified fixture 5,700/725, promotional/seat changes, overage, zero/negative allowance, incompatible scope/coverage/period. Only verified compatible inputs yield a balance. | Core, `*Copilot*`; OI-02 evidence |
| TC-06 | FR-09, FR-10, FR-12 | Provider/retention integration | Quota success+billing failure and the reverse, including quota NeedsAuth/Unsupported/Stale; stale billing retains original dates; generic quota retention cannot overwrite fresh billing. | Infra, `*Copilot*`; Core, `*SnapshotRetentionPolicyTests*` |
| TC-07 | FR-10, FR-11; NFR-01, NFR-04 | Real temporary-file integration | Restart, owner change, credential change, rollover, old JSON without billing, malformed independent billing cache, write failure, unknown version. No cross-owner/period reuse or collateral quota/deadline loss. | Infra, `*Copilot*` and `*UsageArchiveTests*` |
| TC-08 | NFR-02; FR-10, FR-12 | Fake-clock/provider/store integration | 429 mid-pass, zero/date/missing Retry-After, increasing failures, restart, forced refresh, legacy deadline, persistence failure. No later send before the deadline; quota exhaustion alone does not block billing. | Infra, `*Copilot*`, `*UsageStoreTests*`; Core, `*RateLimitPolicyTests*` |
| TC-09 | NFR-01, NFR-03 | Contract/integration | No credential write/refresh, no token on report hosts, unsafe redirect rejected, cancellation during quota/billing/download, 15-second/four-dispatch budget, multi-pass progress without starvation, completed source preserved. | Infra, `*Copilot*` |
| TC-10 | FR-01, FR-09, FR-13; NFR-05, NFR-06 | Linked ViewModel unit | Separate rows; real labels for arbitrary windows; missing values; decimals; source vs fetch time; stale/partial/prior-period labels; no AI fraction without total; current session/weekly and derived request providers remain correct. | Infra, `*ProviderRingViewModelTests*`, `*ProviderUsageRow*`, `*NotchViewModelTests*` |
| TC-11 | FR-13; NFR-03, NFR-05 | Manual | M-01 through M-05 below, real WPF layout and focus evidence. | Manual owner: implementing engineer |
| TC-12 | NFR-06 | Build/static review | Core has no WPF/OS dependency; App bindings compile; no added packages; all new files obey repository C# structure, XML comments, cancellation, and disposal rules. | Scoped builds and relevant diff review |

Outcome/story trace: OBJ-01 and US-06 use TC-01/10/11; OBJ-02 and US-01/02/03 use TC-02/03/04; OBJ-03 uses TC-05; OBJ-04 and US-04/05 use TC-06/07/08/09; OBJ-05 uses TC-10/11/12. Every FR/NFR appears in the matrix; no coverage percentage is imposed.

### Commands for implementation validation

Run from repository root in PowerShell. These commands are planned, not claimed as executed. Use one configuration, Release, matching CI. Restore each listed project only if its assets are missing or dependency inputs changed. Build test projects to build their referenced Core/Infrastructure dependencies, then App; do not separately rebuild Core. Serialize builds/tests that share output directories.

```powershell
rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal
$validationExit = $LASTEXITCODE
if ($validationExit -ne 0) { throw "Core test build failed: $validationExit" }

rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal
$validationExit = $LASTEXITCODE
if ($validationExit -ne 0) { throw "Infrastructure test build failed: $validationExit" }

rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal
$validationExit = $LASTEXITCODE
if ($validationExit -ne 0) { throw "App build failed: $validationExit" }

rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$validationExit = $LASTEXITCODE
if ($validationExit -ne 0) { throw "Core Copilot tests failed: $validationExit" }

rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$validationExit = $LASTEXITCODE
if ($validationExit -ne 0) { throw "Infrastructure Copilot tests failed: $validationExit" }
```

Execute other matrix filters through the same project route, one proven class filter at a time. For example, replace the last filter with `*ProviderUsageRow*`, `*ProviderRingViewModelTests*`, `*NotchViewModelTests*`, `*UsageStoreTests*`, or `*UsageArchiveTests*` in Infra; use `*SnapshotRetentionPolicyTests*` and `*RateLimitPolicyTests*` in Core. New tests must use the matrix naming families or the filters must be updated before execution. If discovery is needed, use the same command with `--list-tests --minimum-expected-tests 1`; listing is not a pass. Inspect executed counts, failures, and skips. Zero execution is never acceptance. If RTK hides failure details, rerun only that filter with `rtk proxy dotnet run`.

Before broader regression, inspect the selected test classes for full-application launch and exclude desktop E2E. Full solution build/publish and installer workflows are not necessary to validate this feature. The supplied plan's native `dotnet test` example is replaced by the repository's evidenced MTP executable route; the minimum-test guard is preserved.

### Manual acceptance script

Owner: implementing engineer; reviewer checks saved evidence. Use a controlled development fixture transport for otherwise unavailable billing states, injected only in a test/development composition, never fabricated production values. Confirm at least one permitted real response separately under OI-01/OI-03. Do not copy live credentials or signed links into screenshots or fixtures.

| Step | Action | Expected result |
| --- | --- | --- |
| M-01 | Launch the Release App executable using Windows MCP `App`, `mode="launch_executable"`; keep an editor active and hover Copilot. | Correct labels and the editor retains focus; no shell-launched invisible desktop process. |
| M-02 | Exercise usage-only, verified allowance fixture, unavailable access, historical partial, and stale prior-period cases. | Values, owner, source, period, reset limitations, and errors remain readable; no `0 / 0`, false session label, or live-balance claim. |
| M-03 | Exercise maximum row content, long owner name, and supported display scaling/edge positions; capture Windows MCP Screenshot with `display: [2]`. | Tooltip stays within supported bounds without clipped values or obstructed HUD placement. |
| M-04 | Click and drag the HUD while editor focus is active; hover in/out and inspect existing click-through regions. | No activation theft, layered-composition regression, or broken hover/click-through behavior. |
| M-05 | Compare a provider with actual session/weekly windows and one with only remaining requests. | Existing metric values/ring semantics remain correct and rows use the actual available data. |

Until performed, essential manual acceptance remains pending. This is a manual checklist, not an automated desktop E2E suite under another name.

## Observability and rollout

Use existing Serilog infrastructure for structured billing outcomes and gate transitions. Emit no adoption telemetry or invented SLA. Distinguish missing ownership evidence, lack of access, unsupported response semantics, and transport failure so consumption-only behavior can be diagnosed.

Rollout gates are the mapped tests, manual evidence, and production scope mappings under OI-01. The current OI-02 finding keeps balance activation disabled; a future authoritative allowance/reset mapping must be separately evidenced before enabling totals, remaining, or calculated fractions. The current scope includes resolved personal, organization, and enterprise paths plus available managed reports; permanently returning Unknown for every account does not count as completion. A legitimately unresolved runtime account still gets the PRD's unavailable behavior.

Compatibility is additive for domain snapshots and isolated for billing cache. Old `last_readings.json` data must deserialize without the optional property; the generic archive does not become the billing source of truth. No credential, SQLite, or settings migration occurs. Roll back by reverting the implementation commit and retaining existing quota archives/deadlines. The old executable ignores the separate billing file; do not delete user-owned state as part of rollback. During an in-process transport-disable rollback, continue attaching stale/unavailable billing state and obey all persisted deadlines.

## Risks and open items

| ID | Gap or risk | Owner and closure evidence | Impact |
| --- | --- | --- | --- |
| OI-01 | Read-only production evidence for principal-to-owner billing mapping. | Closed for Organization scope: verified via `GET /orgs/{owner}/copilot/billing/seats` (`seats[].assignee.login` matches principal with `plan_type=business`) and corroborated by local Copilot CLI session workspaces (`~/.copilot/session-state/`). Personal (404) and Enterprise remain inactive. | Closed for Organization scope. Unblocks T04 direct organization billing. Personal and Enterprise paths remain inactive without blocking organization activation. |
| OI-02 | Current real and documented responses expose no authoritative dynamic allowance or reset field. | T01's sanitized contract dossier records the observed billing and seat fields and the absence of an included-pool, remaining, or reset mapping. If a future response exposes one, record its exact field, official source, scope/unit/period, and promotion/seat-change semantics before enabling it. | Closed for the current contract as consumption-only. Usage-only behavior can ship; balance fields remain unavailable until a future authoritative mapping is evidenced. |
| OI-03 | T01 observed one report manifest and signed download, but real partition sizes, revisions, freshness, date completeness, signed-host behavior across reports, and borrowed-token access remain unexercised. | Implementing engineer: representative sanitized billing/report captures and bounded-progress evidence; choose explicit stream size/row guards based on those observations and validate multi-pass progress. | Report reliability and real-access acceptance remain pending; fake HTTP proves application logic only. |
| OI-04 | Manual WPF geometry/focus evidence is absent. | Implementing engineer and reviewer: M-01 through M-05 results and primary-monitor screenshots. | Blocks final HUD acceptance, not creation of this document. |
| OI-05 | Shared retention/gating changes can regress quota behavior or lose a result during cancellation. | Implementing engineer: TC-06/07/08/09/12, including outer-store behavior for non-opt-in providers. | Medium likelihood, high impact; restrict the request-gating opt-in to Copilot and retain existing policy math. |

No existing feature task plan was present in `tasks/prd-copilot-ai-credits`; none is invalidated or created here. Before planning, revalidate retained provider artifacts and the old Copilot specification's billing-fidelity text against DEC-01/05/06. The PRD remains unchanged. This artifact does not claim implementation approval, completed tests, or completed external evidence gates.

## Relevant files

Modification/create paths and responsibilities are listed once in CMP-01 through CMP-10. Test additions belong in `tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicyTests.cs` and `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/`, with integration cases in the existing archive/store/retention test classes and new `ViewModels/ProviderUsageRowFactoryTests.cs`. Modify `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` only to link the new WPF-free presentation source files. No test framework or dependency update is required.
