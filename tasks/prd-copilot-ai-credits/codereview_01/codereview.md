# Code review report — Copilot AI credits

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `45ebd49..HEAD` (encompassing baseline commit `106957c` and uncommitted feature worktree)
- Previous review: — (Initial review)

The implementation of feature `prd-copilot-ai-credits` delivers direct Copilot AI-credits billing integration beside operational premium interactions, with a pure Core domain policy, persistent per-request rate-limit gating, independent archive persistence, bounded historical daily report ingestion, and semantic HUD tooltip rows with no focus theft. All 13 Functional Requirements (FR-01 through FR-13), 6 Non-Functional Requirements (NFR-01 through NFR-06), 5 Objectives (OBJ-01 through OBJ-05), and 6 User Stories (US-01 through US-06) are fully satisfied and verified with passing automated suites (77 Core tests, 451 Infrastructure tests) and live desktop HUD acceptance. 

The review status is **APPROVED WITH RESERVATIONS** due to minor code structure and method length hygiene findings (CR-01 through CR-04) where a UI control file and several helper/service methods exceed the repository limits (<= 300 lines per file, <= 30 lines per method). These findings do not impair correctness, safety, performance, or user experience.

---

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-copilot-ai-credits/prd.md` | read |
| TechSpec | `tasks/prd-copilot-ai-credits/techspec.md` | read |
| Manifest | `tasks/prd-copilot-ai-credits/tasks.md` | read |
| Implementation | `45ebd49..HEAD`, task handoffs `done/task_01.md` through `done/task_07.md`, and uncommitted worktree | delimited |

---

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Present AI credits and premium interactions as independent metric families; internal quota entitlement/credits used never populate billing credits. | `src/TokenHound.Core/Policies/CopilotCreditPolicy.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs`<br>`src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotUsageProviderTests.Billing.cs`<br>`tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotTests.cs` | conformant | Passing TC-01 & TC-10; internal quota entitlement 20,000 and credits used 725 alone produce no billing credit values. Quota and billing rows render separately in HUD. |
| FR-02 | Resolve billing ownership before selecting billing data; keep plan classification separate from scope; unknown plan names remain unknown. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingContextResolver.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingContextResolverTests.cs` | conformant | Passing TC-02; Organization seat endpoint (`GET /orgs/{owner}/copilot/billing/seats`) verifies principal seat assignment with `plan_type=business`; unmapped personal (404) and enterprise scopes stay `Unknown`. |
| FR-03 | Use one verified billing owner and explicit coverage for a displayed aggregate; no speculative multi-org pooling. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingContextResolver.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingContextResolverTests.cs` | conformant | Passing TC-02; multiple candidate orgs without enterprise precedence return `CopilotBillingReason.AmbiguousScope` without guessing or summing. |
| FR-04 | Prefer compatible billing-period usage over historical metrics; direct source exposes owner, period, source, timestamp, and coverage. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Direct.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingServiceTests.cs` | conformant | Passing TC-03; direct billing returned when available; daily reports used only as fallback; direct billing items replace previous aggregates. |
| FR-05 | Preserve gross, discount, and net quantities independently with decimal precision; absent dimension stays null; never sum gross+discount+net. | `src/TokenHound.Core/Policies/CopilotCreditPolicyAggregation.cs` | `tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicyTests.cs` | conformant | Passing TC-03; decimals parsed invariants; individual missing dimensions stay null; discount quantity is not treated as included allowance. |
| FR-06 | Support historical per-user credit reports when billing is unavailable; daily `ai_credits_used` summed without duplicate/overlapping coverage; partial coverage identified. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Historical.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsReportParser.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingServiceHistoricalTests.cs`<br>`tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsReportParserTests.cs` | conformant | Passing TC-04; streamed NDJSON parsed line-by-line; duplicates and user conflicts rejected; single-day atomic summary committed; direct and report totals are strictly never added together. |
| FR-07 | Accept included allowance only from verified authoritative response; usage-only/seat-only leaves total unavailable. | `src/TokenHound.Core/Policies/CopilotCreditPolicyEvaluator.cs`<br>`tasks/prd-copilot-ai-credits/evidence/copilot-contracts.md` | `tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicySafetyTests.cs`<br>`tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicyCompatibilityTests.cs` | conformant | Passing TC-05; no allowance field exists in current GitHub API response; `IncludedTotal` is null; OI-02 closed as consumption-only. |
| FR-08 | Derive remaining credits only from verified allowance and compatible gross consumption; otherwise remaining and fraction are null. | `src/TokenHound.Core/Policies/CopilotCreditPolicyEvaluator.cs` | `tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicyCompatibilityTests.cs` | conformant | Passing TC-05; `Remaining` and `UsedFraction` null when allowance is null or zero; verified fixture (5,700/725) yields 4,975; negative remaining preserved on overage. |
| FR-09 | Keep period, reset, provenance, coverage, and freshness attached to billing values; reset shown only when supported by verified billing semantics. | `src/TokenHound.Core/Models/CopilotCreditUsage.cs`<br>`src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs` | `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotTests.cs` | conformant | Passing TC-03, TC-04, TC-10; reset countdown rendered only when `ResetUtc` is present on billing period; never borrowed from operational quota reset. |
| FR-10 | Refresh quota and billing independently; retain last valid billing reading as stale after billing failure; generic quota retention cannot discard billing. | `src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs` | `tests/TokenHound.Core.Tests/Policies/SnapshotRetentionPolicyTests.cs`<br>`tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotUsageProviderTests.Billing.cs` | conformant | Passing TC-06; quota success + billing failure preserves quota Ok and attaches stale billing; billing success + quota error preserves billing in snapshot. |
| FR-11 | Prevent cached values from crossing account, scope, or period boundaries. | `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBilling.cs`<br>`src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBillingKey.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingArchiveTests.cs` | conformant | Passing TC-07; atomic cache in `copilot_billing.json` keyed by principal, scope, owner, and period; only current and preceding periods retained. |
| FR-12 | Explain missing billing access and retrieval failures without inventing usage; credentials not silently substituted. | `src/TokenHound.Core/Models/CopilotBillingReason.cs`<br>`src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs` | `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotErrorTests.cs` | conformant | Passing TC-02, TC-06, TC-08; explicit typed reasons: `MissingCredential`, `UnknownScope`, `AmbiguousScope`, `AccessDenied`, `ReportUnavailable`, `RateLimited`, `NetworkFailure`, `InvalidData`, `PersistenceFailure`. |
| FR-13 | Render provider rows according to actual metric semantics; Copilot details contain separate AI credits and Premium interactions; other providers keep genuine window names. | `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`<br>`src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs`<br>`src/TokenHound.App/UI/Controls/TooltipCard.xaml` | `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs`<br>`tasks/prd-copilot-ai-credits/evidence/copilot-hud-live-colibriagile.png` | conformant | Passing TC-10 & TC-11; Copilot displays `Premium interactions` and `AI credits`; Codex displays `Current session (5h)` and `Weekly limit (7d)`. No false `0 / 0` or progress fill. |
| NFR-01 | Credential integrity and privacy: read-only discovery, concurrent file share (`ReadWrite \| Delete`), no token write/refresh, zero secrets in artifacts. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsClient.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsClientTests.cs` | conformant | Passing TC-07, TC-09; signed download requests omit Bearer auth; no tokens or signed URLs stored in logs or archive. |
| NFR-02 | Rate-limit compliance: persist 429 deadline before releasing gate or subsequent dispatch; monotonic deadlines; minimum 60s backoff floor. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`<br>`src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotHttp.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotRequestGateTests.cs`<br>`tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotRequestGateTests.Lifecycle.cs` | conformant | Passing TC-08; gate persists `copilotHttp` state in `state.json`; legacy `backoffUntil.copilot` honored; process-lifetime suppression on persistence failure. |
| NFR-03 | Responsiveness and cancellation: 15s timeout and 4-dispatch budget per pass; cancellation propagated; HUD input unblocked. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Historical.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingServiceHistoricalTests.Budget.cs` | conformant | Passing TC-09; pass budget limits dispatches to 4 and enforces 15-second budget; local cancellation cleanly handled. |
| NFR-04 | Data robustness: decimal parsing, absent fields, mixed units, duplicate report rows, signed report redirects, private IP rejection. | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotDownloadUriValidator.cs`<br>`src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsReportParser.cs` | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsClientTests.cs`<br>`tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsReportParserTests.cs` | conformant | Passing TC-03, TC-04, TC-05, TC-07; private IPs (RFC 1918/4193/loopback) rejected; non-HTTPS rejected; duplicate rows checked for user identity and value consistency. |
| NFR-05 | HUD accessibility and non-activating window semantics: text readable; WCAG AA contrast; `WM_MOUSEACTIVATE` returns `MA_NOACTIVATE` (3); `SWP_NOZORDER` used. | `src/TokenHound.App/Interop/WindowStyles.cs`<br>`src/TokenHound.App/UI/Controls/TooltipCard.xaml` | `tasks/prd-copilot-ai-credits/evidence/copilot-hud-live-colibriagile.png`<br>`tasks/prd-copilot-ai-credits/acceptance.md` | conformant | Passing TC-10 & TC-11; verified via live MCP launch and screenshot; foreground editor focus preserved; text wrapping in TooltipCard. |
| FR-06 / NFR-06 | Pure Core invariants: zero UI or OS dependencies in `TokenHound.Core`; compatibility with existing provider providers. | `src/TokenHound.Core/TokenHound.Core.csproj`<br>`src/TokenHound.Core/Models/`<br>`src/TokenHound.Core/Policies/` | `tests/TokenHound.Core.Tests/` (77 tests)<br>`tests/TokenHound.Infrastructure.Tests/` (451 tests) | conformant | Passing TC-12; `TokenHound.Core.csproj` targets standard `net10.0` with 0 package references; all other provider tests pass unmodified. |

---

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Pure `TokenHound.Core` (zero UI/OS/HTTP dependencies) | OK | `src/TokenHound.Core/TokenHound.Core.csproj` has zero package/project references; imports are strictly standard `System.*` collections/primitives. |
| No invented limits or denominators (UsedFraction null if unmeasured) | OK | `src/TokenHound.Core/Policies/CopilotCreditPolicyEvaluator.cs:120-126` leaves `IncludedTotal`, `Remaining`, and `UsedFraction` null when allowance evidence is absent. `HasProgress` in `ProviderUsageRow.cs:25` is false and progress bar collapses. |
| Borrow credentials read-only with `FileShare.ReadWrite \| FileShare.Delete` | OK | `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs` and `CopilotBillingClient.cs` never write credentials or modify auth state. |
| Non-activating HUD (`WM_MOUSEACTIVATE` = 3, `SWP_NOZORDER`, omit `SWP_SHOWWINDOW`) | OK | `src/TokenHound.App/Interop/WindowStyles.cs:51-68` preserves non-activating window behavior; validated in M-01 and M-04 via live desktop MCP testing. |
| Antislop Mode 1 (no em dashes in UI text, complete UI states, WCAG AA contrast) | OK | Checked via grep across `src/TokenHound.App/`; no `—` or `–` characters in UI text (middle dot `·` used for inline separators); colors adhere to dark theme tokens with high-contrast text (`#E4E4E7`, `#9CA3AF`, `#FCA5A5`). |
| File size limit (<= 300 lines per file and class) | NOT OK (Reservation) | `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs` is 304 lines; test files `CopilotBillingClientTests.cs` (340 lines) and `CopilotBillingContextResolverTests.cs` (305 lines) exceed 300 lines (CR-01, CR-03). |
| Method length limit (<= 30 lines per method) | NOT OK (Reservation) | Several methods exceed 30 lines, notably `TooltipCard.UpdateRows` (46 lines), `ProcessHistoricalReportsAsync` (90 lines), and `TryFetchDirectBillingAsync` (74 lines) (CR-02, CR-04). |
| Nesting depth limit (<= 3 levels) | OK | Verified across all implementation files; maximum statement nesting is <= 3 levels (object initializers in `CopilotUsageProvider` do not constitute control flow nesting). |
| Document public members with XML comments | OK | All public types, properties, and methods in Core, Infrastructure, and App models include XML doc comments. |
| File-scoped namespaces and alphabetized usings | OK | Enforced across all new and modified C# source files. |
| Named constants in `UPPER_CASE` and `nameof` usage | OK | Used consistently (e.g. `DEFAULT_TIMEOUT`, `API_VERSION`, `COPILOT_PROVIDER_ID`). |
| `.ConfigureAwait(false)` in Core & Infrastructure | OK | Applied to all `await` calls in Core and Infrastructure; omitted in UI view models. |
| MTP executable test route (`rtk dotnet run --project ... --no-build --no-restore -c Release -- --minimum-expected-tests 1`) | OK | Both test projects execute as standalone executables via Microsoft.Testing.Platform with `--minimum-expected-tests 1`. |
| Desktop HUD validation via Windows MCP App and primary monitor Screenshot | OK | Live MCP launch of PID 68340 and screenshot capture on `display: [2]` recorded in `acceptance.md` and `evidence/copilot-hud-live-colibriagile.png`. |

---

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 (Snapshot.CopilotBilling optional model + pure policy) | YES | `Snapshot.CopilotBilling` added as nullable property; `CopilotCreditPolicy` is pure Core. `LimitWindow` unaffected. |
| DEC-02 (Verified billing context resolver; hints as hints only) | YES | `CopilotBillingContextResolver` requires seat assignment proof; unmapped personal/enterprise stay `Unknown`. |
| DEC-03 (Prefer direct billing; daily reports as fallback; never sum together) | YES | `CopilotBillingService` dispatches direct billing first; historical report fallback used only if direct billing unavailable; strict separation enforced. |
| DEC-04 (Evidence-gated allowance; null total when unevidenced) | YES | `IncludedTotal`, `Remaining`, and `UsedFraction` null in production usage-only mode; verified test fixture yields 4,975. |
| DEC-05 (Independent `copilot_billing.json` cache + retention overlay) | YES | `UsageArchive.CopilotBilling.cs` persists billing separately; `SnapshotRetentionPolicy` preserves incoming billing across all branches and strips billing from `last_readings.json`. |
| DEC-06 (Shared opt-in `IRequestGatedUsageProvider` + `CopilotRequestGate`) | YES | `CopilotUsageProvider` implements `IRequestGatedUsageProvider`; `UsageStore` delegates gating; gate persists `copilotHttp` state before releasing. |
| DEC-07 (Bounded pass: 15s timeout, 4-dispatch budget) | YES | `CopilotPassDispatchBudget` enforces bounds across direct context, direct billing, manifest, and download dispatches. |
| DEC-08 (Semantic rows in `ProviderRingViewModel` + tooltip DataTemplate) | YES | Replaced fixed session/weekly properties in `ProviderRingViewModel` with atomic `IReadOnlyList<ProviderUsageRow> Rows` collection and dynamic `ItemsControl` in `TooltipCard.xaml`. |
| DEC-09 (MTP executable test route, linked view models, desktop E2E omitted) | YES | Infrastructure tests link view models without WPF; MTP executable runs pass on Release; desktop E2E omitted by policy. |
| CMP-01 through CMP-10 component responsibilities | YES | All 10 architectural components implemented according to specification. |
| OI-01 (Production ownership proof) | YES | Closed for Organization scope via `GET /orgs/ColibriAgile/copilot/billing/seats` and local workspace corroboration. Personal (404) and Enterprise remain inactive. |
| OI-02 (Dynamic allowance & reset mapping) | YES | Closed as consumption-only; no dynamic allowance or reset field in GitHub API; live-balance claims prohibited. |
| OI-03 (Historical report access & resource guards) | YES | Closed as safeguarded via 15s/4-dispatch limits, credential-free downloads, private IP rejection, and streaming NDJSON parser. |
| OI-04 (Manual WPF geometry & focus verification) | YES | Closed via Windows MCP App and Screenshot tools on primary monitor `display: [2]`; M-01 through M-05 passed. |
| OI-05 (Shared retention, gating, & store regression) | YES | Closed via full test suite: 451 Infrastructure tests, 77 Core tests, zero regressions. |

---

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Contract dossier recorded in `evidence/copilot-contracts.md`; Organization scope proved via seat endpoint; OI-01 closed for Organization; OI-02 closed as consumption-only; unblocks T04. |
| T02 | `done/task_02.md` | COMPLETE | Pure Core domain models and `CopilotCreditPolicy` implemented and tested; 27 Copilot tests and 6 Snapshot tests passed; 72 Core tests total; pure Core invariants preserved. |
| T03 | `done/task_03.md` | COMPLETE | `IRequestGatedUsageProvider` marker defined; `UsageStore.Refresh.cs` updated; independent `copilot_billing.json` cache and `copilotHttp` deadline persistence implemented; 52 Copilot tests, 7 archive tests, 2 store tests passed. |
| T04 | `done/task_04.md` | COMPLETE | Direct Organization billing client, resolver, and service implemented; `CopilotUsageProvider` wired as request-gated; `SnapshotRetentionPolicy` preserves billing; App composition updated; 82 Copilot tests, 17 store tests, 10 retention tests passed. |
| T05 | `done/task_05.md` | COMPLETE | Daily historical report manifest client, credential-free signed download validator, streaming NDJSON parser, and multi-partition tracking implemented; 106 Copilot tests passed; direct and report totals strictly separated. |
| T06 | `done/task_06.md` | COMPLETE | WPF-free `ProviderUsageRow` and pure `ProviderUsageRowFactory` implemented; `TooltipCard.xaml` updated to dynamic `ItemsControl` with flat progress bar and WCAG AA wrapping text; 19 row tests, 18 ring tests, 7 notch tests passed. |
| T07 | `done/task_07.md` | COMPLETE | Integrated desktop acceptance recorded in `acceptance.md`; full suite passed (77 Core tests, 451 Infrastructure tests); live Windows MCP App launch (PID 68340) and Screenshot on `display: [2]` verified; M-01 through M-05 passed. |

---

## Executed validations

- **Profile and exclusions:** .NET SDK 10.0.400, Microsoft.Testing.Platform runner with xUnit v3, `Release` configuration. Desktop E2E omitted by .NET desktop policy.
- **Validated state:** Working tree at commit `106957c` plus uncommitted feature changes.
- **Reused evidence:** None; all test suites and compile checks were freshly executed and verified in this review session.
- **Manual acceptance:** Live application launch via Windows MCP `App` tool (PID 68340) and Screenshot on primary display `[2]`, verified in `acceptance.md` and `evidence/copilot-hud-live-colibriagile.png`.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` | PASSED (77/77 tests passed in 576ms) | FR-01, FR-04, FR-05, FR-07, FR-08, FR-10, NFR-04, NFR-06 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` | PASSED (451/451 tests passed in 2.56s) | FR-01 through FR-13, NFR-01 through NFR-06 |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` | PASSED (3 projects, 0 errors, 0 warnings in 1.82s) | FR-13, NFR-05, NFR-06, CMP-09, CMP-10 |
| `rtk git diff --check` | PASSED (Clean, 0 whitespace or formatting errors) | Repository style standards |

---

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low | Repository rule: `AGENTS.md` (files <= 300 lines) | `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs:304` — file is 304 lines long, exceeding the 300-line threshold by 4 lines. | Minimal maintenance impact; code compiles and executes cleanly. | Extract legacy fallback helper methods (`CreateLegacySessionRow`, `CreateLegacyWeeklyRow`) into a private partial class or extension method to reduce file length below 300 lines. |
| CR-02 | Medium | Repository rule: `AGENTS.md` (methods <= 30 lines) | `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs:186-231` — `UpdateRows()` is 46 lines long due to inlined legacy backward-compatibility property extraction. | Reduces method readability and violates method size invariant. | Extract legacy session and weekly fallback row generation into separate single-responsibility helper methods <= 20 lines each. |
| CR-03 | Low | Repository rule: `AGENTS.md` (files <= 300 lines) | `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingClientTests.cs:340` (340 lines) and `CopilotBillingContextResolverTests.cs:305` (305 lines). | Test files slightly exceed the 300-line repository ceiling. | Split `CopilotBillingClientTests` into partial classes or separate test classes (e.g. `CopilotBillingClientRouteTests.cs` and `CopilotBillingClientErrorTests.cs`). |
| CR-04 | Medium | Repository rule: `AGENTS.md` (methods <= 30 lines) | Multiple backend methods exceed 30 lines: `CopilotBillingService.Historical.cs:15` (`ProcessHistoricalReportsAsync`, 90 lines), `CopilotBillingService.Direct.cs:14` (`TryFetchDirectBillingAsync`, 74 lines), `CopilotBillingService.Historical.cs:106` (`ProcessSingleDayReportAsync`, 73 lines), `CopilotBillingService.Mapping.cs:135` (`BuildHistoricalUsage`, 61 lines), `CopilotBillingContextResolver.cs:126` (`VerifyCandidatesAsync`, 58 lines), `CopilotBillingService.cs:129` (`FetchAndEvaluateBillingAsync`, 57 lines), `CopilotCreditPolicyEvaluator.cs:214` (`CalculateBalance`, 52 lines). | Increased cognitive complexity and maintenance overhead in orchestration and pipeline methods. | Decompose large pipeline methods into smaller, focused sub-methods (e.g. extracting error handling, manifest validation, and payload mapping into dedicated helper methods <= 30 lines). |

---

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| — | — | Initial review; no previous review artifacts existed. |

---

## Limitations and open items

1. **Personal and Enterprise Scope Inactivity (OI-01):** The authenticated production environment verified active Organization scope (`ColibriAgile`) through Copilot seat assignment. The Personal billing endpoint returned HTTP 404 (due to absence of the fine-grained `Plan: read` permission on the borrowed token), and no enterprise billing account was attached. These paths correctly remain in the specified `UnknownScope` unavailable state without blocking release of Organization billing.
2. **Authoritative Allowance Absence (OI-02):** The GitHub billing usage and Copilot metrics APIs do not expose an authoritative dynamic credit allowance or reset timestamp. In compliance with repository invariants ("Never invent limits or denominators"), the feature operates strictly in consumption-only mode (`IncludedTotal = null`, `Remaining = null`, `UsedFraction = null`), hiding progress percentages and false denominators while rendering exact gross and net credit quantities.

---

## Conclusion

The implementation of `prd-copilot-ai-credits` represents high-quality engineering that adheres strictly to core product boundaries, architectural invariants, rate-limit policies, and UI accessibility standards. Quota and credit metrics are cleanly decoupled, pure Core contracts are preserved without external dependencies, and real desktop HUD composition functions seamlessly without activation theft.

Because all functional and non-functional obligations are satisfied with passing automated test suites and verified live desktop evidence, and the only outstanding findings are non-blocking code structure and method length hygiene items (CR-01 through CR-04), the literal status of this review is:

**APPROVED WITH RESERVATIONS**
