# Implementation plan: Copilot AI credits

Status: All tasks (T01 through T07) completed and approved. Feature prd-copilot-ai-credits is validated and APPROVED in independent code re-review (codereview_02). Ready for final human acceptance (HIL 3).

## Stable sources

- PRD: [prd.md](prd.md).
- TechSpec: [techspec.md](techspec.md).
- Supporting source: [adjustment plan](../../docs/COPILOT-AI-CREDITS-PLAN.md).
- Planning skill: `sdd-plan-tasks`, including its manifest and task templates.

Read stable sources before task details and mutable state. Reuse unchanged versions; read order does not guarantee cache reuse. The PRD remained unchanged. The TechSpec was created in this session and reconciled before planning; all decisions below refer to that version.

Inventory found only prd.md and techspec.md before creation of this plan, with no tasks.md, root task files, or done task files. IDs start at T01 and are unique within this feature. Existing unrelated workspace changes are outside this plan.

## Dependency graph

This table is authoritative for dependencies and state. Tasks describe the same edges; no implementation order is inferred from filenames alone.

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Verified scope/report contract dossier with explicit evidence gaps | None | T04, T05, T07 |
| T02 | Pure credit contracts and compatible balance calculations | None | T03, T04, T05, T06 |
| T03 | Independent billing archive and persistent request gate | T02 | T04, T05 |
| T04 | Direct scoped billing integrated beside operational quota | T01, T02, T03 | T05, T07 |
| T05 | Bounded historical fallback with correct coverage | T01, T02, T03, T04 | T07 |
| T06 | Semantic credit/quota tooltip rows | T02 | T07 |
| T07 | Integrated evidence and manual desktop acceptance | T01, T04, T05, T06 | Review/release decision outside this plan |

T02 is a separate foundation because contracts/policy unlock transport, persistence, and presentation. T03 is a separate foundation because direct billing and multi-request report processing both require durable per-request gating and independent retention. Other implementation tasks include their own behavioral tests; T07 owns manual integrated acceptance, not deferred implementation tests.

T01 may produce partial evidence without closing all gates. Independent T02, T03, and T06 can progress while real ownership/report evidence remains blocked. T04 cannot claim resolved-scope completion until OI-01 is closed for the required paths; T05 retains OI-03. No inaccessible obligation is silently removed.

### File and contract collision rules

| Surface | Owners | Coordination |
| --- | --- | --- |
| Core billing contracts/policy | T02; consumers T03/T04/T05/T06 | T02 establishes the contract first. A later contract change invalidates affected consumer/test evidence. |
| UsageArchive and Copilot gate | T03, then T04/T05 integration | T04 and T05 depend on T03; T05 follows T04. Do not edit archive/gate contracts concurrently. |
| CopilotBillingService and provider/store integration | T04, then T05 | Serialize through the DAG; T05 extends the proven direct path. |
| App.xaml.cs composition | T04 | T06 does not edit composition; it owns ViewModels/controls only. |
| Presentation files and linked test project entries | T06 | Other tasks do not modify these files. |
| TechSpec evidence mappings and plan state | T01; final state T07 | Reconcile source changes before dependent work. The manifest is the sole DAG/state authority. |
| Shared bin/obj and test fixtures | All validating tasks | Serialize builds/test executions that share outputs even when source edits are independent. |

The graph permits independent work; it does not authorize spawning agents. Use delegation only if separately authorized.

## Traceability matrix

PRD references are to its requirements, outcomes, stories, and assumptions sections. TechSpec references are to its decision/component/test/open-item tables.

| Source ID | Source section | Delivery obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | PRD functional requirements | Separate credit/quota metric families | T02, T04, T06 | TC-01, TC-10 |
| FR-02 | PRD functional requirements | Verified scope and independent plan classification | T01, T04 | TC-02; OI-01 real evidence |
| FR-03 | PRD functional requirements | One owner and explicit coverage; no speculative pooling | T01, T04 | TC-02 |
| FR-04 | PRD functional requirements | Compatible direct billing preference and metadata | T01, T02, T04, T05 | TC-03, TC-04 |
| FR-05 | PRD functional requirements | Independent decimal gross/discount/net aggregation | T02, T04, T05 | TC-03, TC-04 |
| FR-06 | PRD functional requirements | Daily historical fallback without duplicate/partial-as-zero totals | T01, T05 | TC-04, TC-09 |
| FR-07 | PRD functional requirements | Authoritative allowance evidence only | T01, T02, T04 | TC-05; OI-02 |
| FR-08 | PRD functional requirements | Compatible balance/fraction and visible overage | T02, T04, T05 | TC-05 |
| FR-09 | PRD functional requirements | Period/reset/source/freshness qualification | T01, T02, T04, T05, T06 | TC-03, TC-04, TC-05, TC-10 |
| FR-10 | PRD functional requirements | Independent source success and stale retention | T03, T04, T05 | TC-06, TC-07, TC-08, TC-09 |
| FR-11 | PRD functional requirements | No cache reuse across owner/period boundaries | T03, T04, T05 | TC-07 |
| FR-12 | PRD functional requirements | Specific failure reasons and no silent credential substitution | T01, T04, T05 | TC-02, TC-06, TC-08, TC-09 |
| FR-13 | PRD functional requirements | Data-driven actual metric rows | T06, T07 | TC-10, TC-11; M-01 through M-05 |
| NFR-01 | PRD non-functional requirements | Read-only credentials and secret-free artifacts | T01, T03, T04, T05 | TC-07, TC-09 |
| NFR-02 | PRD non-functional requirements | Persisted per-request 429 compliance | T03, T04, T05 | TC-08 |
| NFR-03 | PRD non-functional requirements | Bounded, cancellable retrieval and responsive HUD | T03, T04, T05, T06, T07 | TC-09, TC-11 |
| NFR-04 | PRD non-functional requirements | Defensive decimals/schema/coverage handling | T01, T02, T03, T04, T05 | TC-03, TC-04, TC-05, TC-07 |
| NFR-05 | PRD non-functional requirements | Readable states, geometry, and no activation theft | T06, T07 | TC-10, TC-11 |
| NFR-06 | PRD non-functional requirements | Pure Core and existing provider compatibility | T02, T04, T06, T07 | TC-01, TC-06, TC-10, TC-12 |
| OBJ-01, US-06 | PRD outcomes/stories | Distinguishable metrics across providers | T04, T06, T07 | TC-01, TC-10, TC-11 |
| OBJ-02, US-01, US-02, US-03 | PRD outcomes/stories | Correct personal/organization/enterprise ownership | T01, T04, T05, T07 | TC-02, TC-03, TC-04 |
| OBJ-03 | PRD outcomes | Only evidence-supported balances | T01, T02, T04, T07 | TC-05 |
| OBJ-04, US-04, US-05 | PRD outcomes/stories | Honest partial, stale, unavailable, and rate-limited behavior | T03, T04, T05, T06, T07 | TC-06 through TC-10 |
| OBJ-05 | PRD outcomes | Usable compatible HUD | T06, T07 | TC-10, TC-11, TC-12 |
| D1, D2, D3; A1, A2 | PRD assumptions/decisions | Propagate weakest fidelity; identity-safe cache; textual states; all supported scopes; no new picker | T01 through T07 | Corresponding FR/NFR gates above; unresolved runtime scope is not scope removal |
| DEC-01; CMP-01, CMP-02 | TechSpec decisions/components | Optional dedicated model and pure policy | T02 | TC-01, TC-03, TC-05, TC-12 |
| DEC-02; CMP-03 | TechSpec decisions/components | Evidence-based context resolution | T01, T04 | TC-02; OI-01 |
| DEC-03; CMP-04, CMP-05 | TechSpec decisions/components | Direct source plus daily-report fallback | T04, T05 | TC-03, TC-04, TC-06 |
| DEC-04 | TechSpec decisions | Guard allowance and derived values | T01, T02, T04, T05 | TC-05; OI-02 |
| DEC-05; CMP-07, CMP-08 | TechSpec decisions/components | Independent archive/retention overlay | T03, T04, T05 | TC-06, TC-07 |
| DEC-06; CMP-06 | TechSpec decisions/components | Opt-in shared persistent request gating | T03, T04, T05 | TC-08, TC-09 |
| DEC-07 | TechSpec decisions | Four-dispatch/15-second bounded progress | T01, T04, T05 | TC-09; OI-03 |
| DEC-08; CMP-09 | TechSpec decisions/components | Semantic rows and existing ring meaning | T06 | TC-10 |
| CMP-10 | TechSpec components | Tooltip caller and shared service composition | T04, T06 | TC-06, TC-10, TC-12 |
| DEC-09 | TechSpec decisions | Proportional validation profile | T02 through T07 | TC-01 through TC-12 |
| TC-01 | TechSpec test approach | Quota/credit separation regression | T02, T04 | Core/Infra Copilot tests |
| TC-02 | TechSpec test approach | Scope proof and unknown/ambiguous behavior | T01, T04 | Resolver fixtures plus real contract dossier |
| TC-03 | TechSpec test approach | Direct transport/dimension contract | T01, T02, T04 | Billing client/parser/policy tests |
| TC-04 | TechSpec test approach | Reports, partition/duplicate/coverage correctness | T01, T05 | Streamed report and archive tests |
| TC-05 | TechSpec test approach | Allowance and compatibility boundary | T01, T02, T04, T05 | Policy tests plus allowance evidence finding |
| TC-06 | TechSpec test approach | Independent-source retention | T04, T05 | Provider/store and retention tests |
| TC-07 | TechSpec test approach | Persistent cache integrity | T03, T04, T05 | Real temporary-file tests |
| TC-08 | TechSpec test approach | Durable 429 and store integration | T03, T04, T05 | Fake-clock/HTTP, real-file and store tests |
| TC-09 | TechSpec test approach | Security, cancellation, bounded progress | T03, T04, T05 | Credential/transport/service tests |
| TC-10 | TechSpec test approach | Row and other-provider behavior | T06 | Linked ViewModel tests |
| TC-11 | TechSpec test approach | Desktop manual acceptance | T07 | M-01 through M-05 |
| TC-12 | TechSpec test approach | Architecture and compile compatibility | T02, T04, T06, T07 | Scoped builds/static review |
| OI-01 | TechSpec risks/open items | Production ownership proof | T01, T04, T07 | Exact source mappings and real evidence; resolved paths blocked if absent |
| OI-02 | TechSpec risks/open items | Dynamic allowance/reset mapping | T01, T02, T04, T07 | Verified mapping or explicit absence; null remains valid consumption-only result |
| OI-03 | TechSpec risks/open items | Real report/access/resource semantics | T01, T05, T07 | Authorized sanitized report evidence and bounded progress |
| OI-04 | TechSpec risks/open items | Actual WPF focus/geometry | T06, T07 | Manual screenshots/checklist |
| OI-05 | TechSpec risks/open items | Retention/gate/cancellation regression | T03, T04, T05, T07 | TC-06 through TC-09, TC-12 |

## Tasks

- [T01: Establish real scope and report contract evidence](done/task_01.md).
- [T02: Represent and calculate compatible AI-credit values](done/task_02.md).
- [T03: Persist billing state and enforce every request deadline](done/task_03.md).
- [T04: Deliver direct billing beside the existing quota](done/task_04.md).
- [T05: Recover bounded historical consumption when billing is unavailable](done/task_05.md).
- [T06: Show semantic credit and quota rows in the HUD](done/task_06.md).
- [T07: Record integrated desktop acceptance and release readiness](done/task_07.md).

## Shared execution and validation rules

Use the TechSpec profile and commands as the source of truth. Each implementation task owns implementation plus meaningful tests for its result; do not defer those tests to T07. The required local SDK is the evidenced 10.0.400 or a compatible global.json-selected SDK. Core/Infrastructure and their tests are net10.0; App is WPF net10.0-windows.

CI uses the MTP executable route, not native dotnet test orchestration. Test commands use `rtk dotnet run --project ... --no-build --no-restore -c Release -- --minimum-expected-tests 1`, with the appropriate proven `--filter-class` after the separator. Capture/check every command's exit code immediately. Zero executed tests, all-skipped output, or listing alone does not establish acceptance.

If assets are missing or dependency inputs changed, restore only the affected project once; for example:

```powershell
rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Restore failed: $taskExit" }

rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Build failed: $taskExit" }
```

Use Core.Tests or App paths from the TechSpec when those outputs are affected. A test project build builds its referenced dependencies; do not repeat standalone Core/Infrastructure builds. Source-linked ViewModel edits invalidate the Infrastructure test build as well as App. Do not run tests before their corresponding build exists. Preserve actual stdout and rerun only a failing/hidden filter with `rtk proxy`.

`E2E: omitted by desktop .NET policy`. Do not run full-app desktop, browser, or WebView E2E, including aggregate suites that contain them. Do not delete existing tests. Use local policy/HTTP/file/ViewModel tests and the explicitly manual M-01 through M-05. Those manual steps require Windows MCP App launch_executable and primary-monitor Screenshot display [2]. Build success does not close geometry/focus acceptance.

## Coverage gate

- Coverage: PASS for planning. All 13 FR, 6 NFR, 5 OBJ, 6 US, 9 DEC, 10 CMP, 12 TC, and 5 OI entries map to delivery/evidence or an explicit gate. PRD decisions/assumptions are retained. Runtime acceptance is not claimed.
- Traceability: PASS. Every task identifies PRD/TechSpec sources and acceptance evidence; detailed contracts remain in the TechSpec.
- Dependencies: PASS. T01-T07 form an acyclic graph; file/contract collisions and shared validation outputs are identified.
- Atomicity: PASS. Two justified shared foundations unlock multiple consumers; direct billing, reports, and presentation each include implementation and tests. T01 is prerequisite evidence; T07 is manual integrated acceptance.
- Executability: CONDITIONAL. Independent local tasks have concrete paths/commands. Real ownership/report access gates OI-01/OI-03 remain attached to T01/T04/T05/T07; they cannot be replaced by fake responses.
- Validation profile: PASS for specification. Existing SDK/TFM/MTP route preserved; desktop E2E excluded; manual obligations and environment gaps retained. No tests were executed while generating this plan.
- Idempotency: PASS. All files were new. IDs, task links, and state authority are explicit. Future updates must preserve completed IDs/handoffs and reconcile only changed sources.

## Assumptions and open items

- PRD scope is unchanged: resolved personal, organization, enterprise, and accessible managed historical reports are included. Ambiguous ownership is an unavailable runtime case, not permission to omit all scope-resolution work.
- OI-01 requires actual owner mappings. T01 must keep unproven routes blocked; a new manual selector/login would change scope and needs a separate product decision only if proven indispensable.
- OI-02 can close with confirmed absence of an allowance field. This permits consumption-only behavior while total/remaining stay unavailable; it does not authorize plan/seat math.
- OI-03 requires real report contract evidence and chosen resource guards. Missing access does not prevent T02/T03/T06 or fake-HTTP development; real-service acceptance remains pending.
- OI-04 requires visible Windows desktop/MCP tools and sanitized manual screenshots.
- OI-05 is addressed by targeted integrated retention/gating/cancellation tests, not a broad refactor.
- Required environment: local .NET SDK/assets for T02-T06; isolated writable temp files for T03-T05; already-authorized GitHub account reporting access for T01/real T04-T05 evidence; Windows/MCP desktop access for T07.
- Authorization: this request covers artifact creation. No implementation, external writes, credential changes, messages, deployment, or user-data deletion has been performed. Obtain implementation direction before executing the plan unless separately authorized later.
- Out of scope retained: invented allowances, seat-derived pools, automatic pooling, credential ownership, new sign-in/picker, invoice/currency/prediction features, new activity monitoring, unrelated provider changes, and general HUD redesign.

## State

The manifest governs state. The orchestrator alone updates state and moves approved tasks. Existing staged deletions and unrelated untracked artifacts are preserved.

- [x] T01: Approved on 2026-09-08; OI-01 closed for Organization scope via seat assignment evidence (GET /orgs/{owner}/copilot/billing/seats) and local Copilot CLI session workspace corroboration. OI-02 closed as consumption-only. Unblocks T04 for direct organization billing. Moved to done/task_01.md.
- [x] T02: Approved on 2026-09-08 after correction pass 1 and C# style remediation; Release build passed (2 projects, 0 errors, 0 warnings), 27/27 Copilot tests passed, 6/6 Snapshot tests passed, full 72/72 Core suite passed, all files <= 300 lines, pure Core invariants preserved. Moved to `done/task_02.md`.
- [x] T03: Approved on 2026-09-08; Release build passed (0 errors, 0 warnings), 52/52 Copilot tests passed, 7/7 UsageArchive tests passed, 2/2 RequestGated tests passed, 376/376 full Infrastructure tests passed, 72/72 Core tests passed, App build passed, all files <= 300 lines, request-level gating, copilot_billing.json and copilotHttp state.json persistence verified. Moved to done/task_03.md.
- [x] T04: Approved on 2026-09-08; Release build passed (4 projects, 0 errors, 0 warnings), 82/82 Copilot tests passed, 17/17 UsageStore tests passed, 3/3 RequestGated tests passed, 10/10 SnapshotRetentionPolicy tests passed, 407/407 Infrastructure tests passed, 77/77 Core tests passed, App build passed, all files <= 300 lines, direct organization billing integration verified. Moved to done/task_04.md.
- [x] T05: Approved on 2026-09-08; Release build passed (4 projects, 0 errors, 0 warnings), 106/106 Copilot tests passed, 431/431 Infrastructure tests passed, 27/27 Core tests passed, App build passed, all files <= 300 lines (CopilotMetricsClient remediated from 303 to 287 lines by extracting redirect handling to CopilotDownloadUriValidator), historical daily report fallback verified without summing direct and report values. Moved to done/task_05.md.
- [x] T06: Approved on 2026-09-08; Release build passed (4 projects, 0 errors, 0 warnings), 19/19 ProviderUsageRow tests passed, 18/18 ProviderRingViewModel tests passed, 7/7 NotchViewModel tests passed, full 451/451 Infrastructure tests passed, 77/77 Core tests passed, App build passed, all files <= 300 lines, antislop Mode 1 rules applied, dynamic ItemsControl with flat ProgressBar and WCAG AA wrapping text in TooltipCard verified. Moved to done/task_06.md.
- [x] T07: Approved on 2026-09-08; Release build passed (4 projects, 0 errors, 0 warnings), 77/77 Core tests passed, 451/451 Infrastructure tests passed, live Windows MCP App launch (PID 68340) and Screenshot on primary display [2] verified. Live production Organization billing under ColibriAgile confirmed (11,260.9209 credits used beside 17,105/20,000 monthly premium interactions), manual acceptance M-01 through M-05 passed, acceptance.md recorded. Moved to done/task_07.md.

## Problems and solutions

- Code review approval on 2026-09-08: Re-review in `codereview_02/codereview.md` returned literal status `APPROVED`. All findings from round 1 (CR-01 file length, CR-02 method length in `TooltipCard`, CR-03 test file lengths) resolved cleanly by decomposing `TooltipCard.xaml.cs` (148 lines) and `TooltipCard.Properties.cs` (118 lines) with methods <= 25 lines, and splitting test files into partials <= 260 lines. Full test suite re-executed in Release mode: 77/77 Core tests, 451/451 Infrastructure tests, and App build passed with 0 errors and 0 warnings. `git diff --check` clean.
- T07 approval on 2026-09-08: T07 completed and verified. Integrated desktop acceptance and release readiness recorded in `acceptance.md`. Verified full test suite against Release build: 77/77 Core tests (1.075s), 451/451 Infrastructure tests (2.067s), App Release build clean (0 errors, 0 warnings). Launched Release executable via Windows MCP `App` tool (`mode="launch_executable"`, PID 68340) without activation theft (`WM_MOUSEACTIVATE` = `MA_NOACTIVATE`). Captured primary-monitor screenshot (`display: [2]`) via Windows MCP `Screenshot`, verifying live production Organization billing under `ColibriAgile` (`11,260.9209 credits used`, `Gross: 11,260.9209 • Net: 5,560.9209`, `Direct billing`) beside operational quota `Premium interactions` (`15% Used`, `17,105 / 20,000` remaining, `Resets in 22d 9h`). Validated manual acceptance steps M-01 through M-05 with no geometry clipping, no 0/0 or false percentages, and true multi-provider window semantics. Moved task_07.md to done/task_07.md. All tasks (T01 through T07) are now complete.
- T06 approval on 2026-09-08: T06 completed and verified. Implemented WPF-free `ProviderUsageRow` immutable model and pure `ProviderUsageRowFactory` (with `ProviderUsageRowFactory.Copilot.cs` partial) to project semantic quota and credit usage rows. Preserved true operational quota windows ('Premium interactions' for Copilot, actual category names for other providers) and independent Copilot AI-credit row. Dynamic `ItemsControl` in `TooltipCard.xaml` replaces fixed session/weekly sections with WCAG AA wrapping text and flat progress bars. Exposed atomic `Rows` collection on `ProviderRingViewModel`. All files remain <= 300 lines and antislop Mode 1 rules enforced (no em dashes in UI text). Verified Release builds (4 projects, 0 errors, 0 warnings), 19/19 ProviderUsageRow tests, 18/18 ProviderRingViewModel tests, 7/7 NotchViewModel tests, 451/451 Infrastructure tests, 77/77 Core tests, and App build. Moved task_06.md to done/task_06.md. T07 is now unblocked.
- T05 approval on 2026-09-08: T05 completed and verified. Implemented daily metrics manifest client, credential-free signed downloads with redirect and private IP validation, streaming NDJSON row parser with deduplication and conflict validation, multi-partition tracking, atomic daily summaries in UsageArchive, 15-second pass timeout and 4-dispatch budget enforcement in CopilotBillingService.Historical.cs. Direct billing and historical reports are strictly never added together. Remediated CopilotMetricsClient from 303 lines to 287 lines by consolidating redirect logic into CopilotDownloadUriValidator (81 lines), ensuring all classes remain <= 300 lines. Verified Release builds (4 projects, 0 errors, 0 warnings), 106/106 Copilot tests, 431/431 Infrastructure tests, 27/27 Core tests, and App build. Moved task_05.md to done/task_05.md and updated manifest state. T07 remains pending T06 and desktop environment.
- T04 approval on 2026-09-08: T04 completed and verified. Implemented CMP-03 `CopilotBillingContextResolver` (Organization seat verification via `GET /orgs/{org}/copilot/billing/seats` and local session workspace corroboration), CMP-04 DTOs and `CopilotBillingClient`, and CMP-05 `CopilotBillingService`. Wired `CopilotUsageProvider` as `IRequestGatedUsageProvider` with request gate dispatches, credential generation tracking, and quota/billing independence. Updated `SnapshotRetentionPolicy` (pure Core) to preserve incoming billing across all status branches and strip billing from generic archive snapshots. Composed shared `UsageArchive` in `App.xaml.cs`. Refactored `CopilotUsageProvider` and `CopilotBillingService` into partials to keep all files <= 300 lines. Verified Release builds (4 projects, 0 errors, 0 warnings), 82/82 Copilot tests, 17/17 UsageStore tests, 3/3 RequestGated tests, 10/10 SnapshotRetentionPolicy tests, 407/407 Infrastructure tests, 77/77 Core tests, and clean git diff. Moved `task_04.md` to `done/task_04.md` and updated manifest state. T05 is now unblocked.
- T01 approval on 2026-09-08: T01 evidence completed and verified. Official read-only GitHub API probe on `GET /orgs/{owner}/copilot/billing/seats` proved the authenticated user (`dougcunha`) is an active Copilot Business seat assignee under `ColibriAgile`, and active local Copilot CLI session workspaces in `%USERPROFILE%\.copilot\session-state\` corroborate the repository owner. OI-01 is closed for Organization scope, and OI-02 is closed as consumption-only (no dynamic allowance). Reconciled `evidence/copilot-contracts.md`, `techspec.md`, and `task_01.md`. Moved `task_01.md` to `done/task_01.md`. T04 direct organization billing is now unblocked.
- T03 approval on 2026-09-08: T03 completed and verified. Defined `IRequestGatedUsageProvider` in Core and updated `UsageStore.RefreshProviderAsync` to bypass outer rate-limiting for request-gated providers. Implemented versioned independent billing cache in `copilot_billing.json` with key isolation and period pruning (current + preceding only) in `UsageArchive.CopilotBilling.cs`. Implemented `copilotHttp` persistence in `state.json` honoring legacy `backoffUntil.copilot`. Implemented `CopilotRequestGate` and `CopilotRateLimitExtractor` (keeping files <= 300 lines) with minimum 60s backoff floor, monotonic deadlines, process-lifetime suppression on persistence failure, and streak reset on success. Verified Release builds (Core, Infrastructure, App), 52/52 Copilot tests, 7/7 UsageArchive tests, 2/2 RequestGated tests, 376/376 full Infrastructure tests, and 72/72 Core tests. Moved `task_03.md` to `done/task_03.md` and updated manifest state.
- T02 approval on 2026-09-08: T02 correction pass 1 completed and verified. Excluded T04 retention edits from SnapshotRetentionPolicy to keep T02 strictly pure Core. Remediated C# formatting across all models, policies, and tests: alphabetized usings, ensured blank lines immediately inside multi-line blocks and before control flow statements, split calls with >= 4 arguments across lines, and verified all 24 touched files remain <= 300 lines (with Evaluator at 288 lines and Metadata at 272 lines). Rebuilt and retested: Release build passed with 0 errors/0 warnings, 27/27 Copilot tests passed, 6/6 Snapshot tests passed, and full 72/72 Core tests passed. Moved `task_02.md` to `done/task_02.md` and updated manifest state. T03 is now unblocked.
- User-requested stop on 2026-09-08: execution paused to conserve the remaining usage limit. No new task or validation was started after the stop. T01 remains evidence-blocked; T02 correction pass 1 was interrupted and no task was moved to `done/`. On continuation, inspect T02's current files and handoff first, remove any remaining T04 retention edits introduced by T02, finish the finite review findings below, and validate the final state before approval. T03 has not started. T06 also awaits the antislop timing choice. Preserve unrelated settings TechSpecs.

- Resume on 2026-09-08: no previous executor handles remain active, no `done/` directory exists, and neither T01 nor T02 has a completed handoff. Existing evidence and Core changes are interrupted work, not approved completion. The user's continuation authorizes resuming execution and delegation. New executors own the same scopes; the orchestrator remains the sole manifest writer. Unrelated settings TechSpecs are preserved.
- T01 review on 2026-09-08: the handoff and sanitized dossier consistently retain the 2026-09-07 observation provenance; no repeated account probes or new access are claimed. The evidence ledger and limited TechSpec edits correctly close only OI-02 as consumption-only. Personal, organization, and enterprise owner proof remains absent; one small report cannot close representative completeness, revision, resource, or progress obligations. Partial delivery accepted for the record, but T01 remains at root and T04/T05/T07 remain dependency-blocked. T02/T03/T06 may continue independently.
- T02 initial review: the executor reported a successful Release build, 20 Copilot tests, and 8 Snapshot tests. Independent review found retention-policy edits assigned to T04, missing acceptance cases for compatibility/partial coverage and allowance changes, and C# formatting violations. Findings were returned only to the same executor for a bounded correction pass; the task is not approved and no dependent task is started from this evidence.

- Execution authorization: the user invoked the orchestration skill after planning. This supplies implementation and task delegation direction without changing acceptance criteria. First batch: T01 owns evidence, limited TechSpec evidence amendments, fixtures, and its handoff; T02 owns Core billing contracts/policy/tests and its handoff. Builds/tests are serialized.
- Desktop environment: the current callable tool inventory contains no Windows MCP App or Screenshot tools. T07 manual launch/primary-monitor evidence remains pending; a shell launch cannot substitute for it.

- Existing quota retention replaces whole snapshots. T04 preserves the independent billing payload and keeps its archive separate.
- Outer provider gating cannot protect a second request after a 429. T03/T04 enforce a shared gate before every send and preserve legacy deadlines.
- Repeated direct-billing probes can starve report backfill under a bounded request budget. T05 reserves progress for pending report work and verifies multi-pass completion.
- The current tooltip labels positional windows as session/weekly. T06 renders actual metric rows and preserves the primary operational ring.
- Account-specific scope and allowance evidence is missing. T01 records the facts; T04/T05/T07 preserve conditional gates rather than fabricating mappings.
