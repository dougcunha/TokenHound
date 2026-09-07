# Implementation plan: Main window context menu

## Stable sources

- [PRD](prd.md), September 7, 2026 version.
- [TechSpec](techspec.md), September 7, 2026 version.
- Planning skill: sdd-plan-tasks, including both supplied task templates.

Read sources before mutable task state. No prior tasks.md, task contracts, or done directory existed in this feature directory at planning time. IDs T01 through T05 are new. This manifest owns the DAG, links, and execution state. The source PRD/TechSpec are not modified by this plan.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| [T01](done/task_01.md) | Safe refresh and terminal shutdown engine | None | T04 |
| [T02](done/task_02.md) | Empty Settings dialog and shared lifecycle/resources | None | T03 |
| [T03](done/task_03.md) | About with real build metadata | T02 | T04 |
| [T04](done/task_04.md) | Complete four-action HUD menu | T01, T03 | T05 |
| [T05](task_05.md) | Integrated acceptance and release-readiness evidence | T04; P01 gates TC-09 and completion | Feature acceptance |

P01 is an external pending prerequisite, not a fabricated executable implementation task. T05's independent manual checks may proceed after T04 while P01 remains unresolved. No task may claim complete feature acceptance before P01 resolves.

T01 is the sole infrastructure foundation: it unlocks both safe Close and reliable Refresh. T02/T03 deliver buildable dialog/service slices with their own verification; their user entry points arrive together in T04, avoiding dead interim menu actions. T05 consolidates OS/packaging evidence and does not postpone delivery-task unit tests.

### File and validation collisions

- T01 owns UsageStore and engine tests. T04 consumes that contract after T01.
- T02 owns DialogService/shared styles initially; T03 extends DialogService after T02; T04 adds menu/popup resources after T03.
- T03 updates the existing test project source links before T04 does so. T01 needs no source-link edits for its engine tests.
- App.xaml is owned by T02; App.xaml.cs and NotchWindow are owned by T04. T05 routes code failures to the owning delivery instead of editing across tasks silently.
- T01/T02 are logically independent, but App and test builds share Infrastructure/Core output directories: serialize builds/tests even when source work is independent. This plan does not request subagents.
- If P01 later edits UsageStore or the same test project, integrate it serially, reconcile the affected TechSpec/task contracts, and invalidate the relevant T01/T04/T05 evidence.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | PRD functional requirements | Exact context menu and dismissal | T04, T05 | TC-01/MAN-01 |
| FR-02 | PRD functional requirements | Exit the process, including active work/dialogs | T01, T04, T05 | TC-04/MAN-04 |
| FR-03 | PRD functional requirements | Immediate all-provider update | T01, T04, T05 | TC-02 |
| FR-04 | PRD functional requirements | Rate limits, partial failure, honest data | T01, T04, T05; P01 | TC-02/TC-08/TC-09 |
| FR-05 | PRD functional requirements and UX | Progress, duplicates, unavailable/mixed outcomes | T04, T05; engine edges T01 | TC-03/TC-08/TC-10 |
| FR-06 | PRD functional requirements | Empty Settings | T02, T04, T05 | TC-05/MAN-02 |
| FR-07 | PRD functional requirements | About description and version | T03, T04, T05 | TC-06/MAN-05 |
| FR-08 | PRD functional requirements | Single-instance dialogs, reopen/dismiss | T02, T03, T04, T05 | TC-04/TC-05/TC-06 |
| NFR-01 | PRD non-functional requirements | Visual consistency and scaling | T02, T03, T04, T05 | TC-07/MAN-03 |
| NFR-02 | PRD non-functional requirements | Keyboard/focus/accessibility | T02, T03, T04, T05 | TC-01/TC-07 |
| NFR-03 | PRD non-functional requirements | Non-activation and desktop placement | T02, T04, T05 | TC-07/MAN-03 |
| NFR-04 | PRD non-functional requirements | Responsiveness and cancellation | T01, T04, T05 | TC-03/TC-04/TC-10 |
| NFR-05 | PRD non-functional requirements | Data/credential/deadline integrity | T01, T04, T05; P01 | TC-08/TC-09 |
| US-01, OBJ-01 | PRD stories/outcomes | Access actions and exit | T04, T05 | MAN-01/MAN-04 |
| US-02, OBJ-02 | PRD stories/outcomes | Fresh counters on demand | T01, T04, T05 | TC-02/TC-03/TC-10 |
| US-03 | PRD stories | Settings journey | T02, T04, T05 | MAN-02 |
| US-04, OBJ-03 | PRD stories/outcomes | Identify app/version | T03, T04, T05 | TC-06/MAN-05 |
| DEC-01, CMP-01 | TechSpec decisions/components | Native menu wiring | T04 | TC-01 |
| DEC-02, CMP-02 | TechSpec decisions/components | Async action state | T04 | TC-03/TC-10 |
| DEC-03, CMP-03/CMP-09 | TechSpec decisions/components | Lifetime ownership and draining | T01, T04 | TC-04 |
| DEC-04 | TechSpec decisions | Existing restriction policy, persistence gap | T01; P01 | TC-02/TC-09 |
| DEC-05, CMP-04/CMP-05 | TechSpec decisions/components | Modeless shared lifecycle | T02, T03 | TC-05/TC-06 |
| DEC-06, CMP-06/CMP-07 | TechSpec decisions/components | About and assembly metadata | T03 | TC-06 |
| DEC-07, CMP-08 | TechSpec decisions/components | Shared visual resources | T02, T03, T04 | TC-07 |
| DEC-08, GAP-01 | TechSpec decisions/risks | Remove production mock injection | T04 | TC-08, App composition review |
| CMP-10 | TechSpec components | Existing runner/source-linked test pattern | T01, T03, T04 | V02 and named test classes |
| TC-01 through TC-10 | TechSpec test approach | Full evidence inventory | T01: 02/04/08/10; T02: 05/07; T03: 06/07; T04: 01/03/04/08/10; T05: all; P01: 09 | Task verification and MAN-01 through MAN-05 |
| PRD A-01 through A-05 | PRD assumptions | Order/no confirmation, counter scope, quality checks, app style, short copy | T01 through T05 as mapped above | No new menu actions or provider measurements |
| Cancellation, input, metadata, layout, provenance risks | TechSpec risks and interfaces | Preserve true contracts and expose missing evidence | T01: cancellation/provenance; T02/T05: input; T03/T05: metadata; T04/T05: popup sizing | Deterministic tests, source review, manual script |
| Security, observability, rollout, reversal | TechSpec interfaces/rollout | No credential mutation, honest status, no destructive reversal | T01, T04, T05 | Source inspection, focused checks, acceptance record |

## Tasks

- [x] [T01: Make refresh and terminal shutdown safe](done/task_01.md).
- [x] [T02: Provide an empty application-styled Settings dialog](done/task_02.md).
- [x] [T03: Show application information and the running build version](done/task_03.md).
- [x] [T04: Deliver the four-action HUD context menu](done/task_04.md).
- [T05: Record integrated acceptance and release readiness](task_05.md).

## Validation commands and environment

### V01: App build

Use the TechSpec's Windows WPF net10.0-windows profile and the repository-compatible SDK (10.0.400 observed during TechSpec preparation). Recheck only if the environment changes. Read dotnet-efficient-validation before execution. Restore only for missing/incompatible assets; build once per changed input set. Commands run from the repository root:

```powershell
rtk dotnet restore src/TokenHound.App/TokenHound.App.csproj --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

T01 requires V02, not an unnecessary App build. T02 needs V01. T03/T04 need both because they affect WPF and source-linked tests. Reuse dependent builds where valid; do not repeat unchanged builds solely to fill a task checklist.

### V02: Existing MTP executable route

Infrastructure.Tests targets net10.0 and links selected App classes; preserve xunit.v3.mtp-v2 4.0.0 and UseMicrosoftTestingPlatformRunner. Use the CI-proven executable route, not the known zero-discovery orchestration route. Restore conditionally and build when changed:

```powershell
rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Each implementation task supplies its exact filtered test commands. Preserve exit codes and executed counts; zero executed tests and all-skipped runs do not satisfy acceptance. Inspect stdout; if filtered output hides a failure, use rtk proxy on the failing class only. Do not run solution-wide suites or add Core tests/builds unless a real Core policy change makes them relevant.

**E2E: omitted by desktop .NET policy**, including local desktop/full-application/browser automation. Unit/integration checks and human MAN-01 through MAN-05 preserve the obligations. Builds and tests are not executed in this planning stage.

### V03: Optional local single-file metadata artifact

Prefer an existing corresponding artifact. If one is unavailable, a local publish can verify the same route without deploying; the literal version below is a test input, not a proposed product version. Publish only when needed and never overwrite an unrelated published deliverable:

```powershell
rtk dotnet publish src/TokenHound.App/TokenHound.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:Version=0.0.0-contextmenu.validation --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

This build changes RID/version inputs and may legitimately restore/build. A human compares About with that artifact's informational metadata, including any SDK source-revision suffix. No external release, upload, installer installation, or production credential change is authorized by this command.

## Coverage gate

- Coverage: PASS for decomposition. Every obligation has a delivery/evidence owner or explicit P01; full implementation acceptance remains pending.
- Traceability: PASS. Original PRD/DEC/CMP/TC/GAP IDs are preserved and all five task links are unique.
- Dependencies: PASS. Acyclic edges T02 -> T03 -> T04 -> T05 and T01 -> T04; P01 is an external completion gate only.
- Atomicity: PASS. Each implementation contract contains its own focused verification. Shared foundation is limited to the engine lifecycle and shared dialog infrastructure accompanying Settings.
- Executability: PARTIAL. T01 through T04 are specified for subsequent authorized execution; T05 can run independent checks after T04 but cannot complete before P01 and essential manual evidence.
- Validation profile: PASS for planning, E2E excluded; actual build/test/manual results are pending. Real provider cancellation, durable storage, and desktop behavior are not proved by substitutes.
- Idempotency: PASS. New files only, no existing task IDs/handoffs/completions replaced; reread changed sources and reconcile only affected tasks on re-entry.

## Assumptions and open items

- P01 / GAP-02, owner: technical owner. Durable deadline integration and TC-09 remain undefined in the TechSpec. Locate an existing separately planned implementation or specify the prerequisite in its proper scope. Before scheduling storage implementation, resolve its paths/contracts/tests in the TechSpec and update affected tasks with authorization. Do not turn this pending item into an invented archive design or waive NFR-05. Independent menu work remains executable.
- P02, owner: Windows reviewer. Human MAN-01 through MAN-05 evidence is required for T05. Availability of an interactive desktop and scale changes is an environment prerequisite, not permission to run automated E2E.
- P03, owner: implementing developer. A safe delayed/failing-provider UI harness is not identified by the TechSpec. Establish an existing supported setup or retain MAN-04 slow/error UI evidence as pending. Do not treat a unit fixture as a running-app harness or mutate real credentials to create the state.
- The listed dialog/engine slices can be build-verified before menu integration; their full user journeys are accepted only after T04/T05. This sequencing does not alter PRD behavior.
- Scope exclusions carry forward: no settings behavior/persistence, additional actions, tray/global shortcuts, provider redesign, new counters, credential renewal, update checking, new branding/themes, or deployment. P01 does not authorize a persistence implementation within these contracts.
- Required environment: Windows desktop, existing provider access used read-only, compatible SDK/Release outputs, optional single-file publish. Existing test fixtures use isolated state. New external integrations require their own identified scope.

## State

- Plan status: in-progress; deliveries T01 through T04 completed in done/; T05 acceptance record established, completion gated on P01 and P02/P03.
- [x] T01: completed.
- [x] T02: completed.
- [x] T03: completed.
- [x] T04: completed.
- [ ] T05: evidence recorded in acceptance.md; final completion gated on P01 and P02/P03.
- P01: unresolved external prerequisite (GAP-02 durable rate-limit persistence).
- P02/P03: manual environment/evidence pending (interactive human session & delay harness).

## Problems and solutions

- No existing plan or completed-task inventory required reconciliation. Unrelated repository changes must remain untouched.
- Historical menu/platform differences are already resolved in the PRD; this plan preserves the exact current request.
- Scope gap in durable rate limits is retained as P01, so independent tasks can proceed without making a false readiness claim.
- Changes to source documents during execution invalidate only derivatives that consume the changed obligations/contracts; preserve all unchanged task IDs and handoffs.
- T01 completed: UsageStore safely drains admitted work, implements terminal StopAsync with admission closing and deferred resource disposal, preserves restartable timer Stop/Start and IDisposable compatibility, isolates provider-local timeouts, and retains stale snapshot provenance. Helper UsageStoreLifetime extracted to satisfy the 300-line limit. All 19 UsageStore unit tests pass deterministically.
- T02 completed: Empty SettingsWindow, DialogResources.xaml shared styles, DialogService modeless lifetime management, and WindowPlacement Win32 monitor work-area positioning implemented with 0 build errors. Full Infrastructure test suite (252 tests) passes.
- T03 completed: ApplicationInfo assembly metadata reader with explicit injection contract, modeless AboutWindow dialog with PRD description and wrapping version text, and DialogService About integration implemented. All 10 ApplicationInfoTests pass deterministically (262 tests total).
- T04 completed: Complete four-action HUD context menu (Close, Refresh, Settings, About) delivered on the main capsule with HudActionsViewModel, ApplicationLifetime shutdown coordinator, non-activating refresh feedback popup outside HUD layout, and removal of automatic mock provider injection (GAP-01/DEC-08). All 271 Infrastructure tests pass.
- T05 evidence recorded: Re-verified all test suites (271 passed, 0 failed), verified V03 single-file publish metadata reflection (0.0.0-contextmenu.validation+commitSha), launched and verified HUD placement via Windows MCP on primary display, and documented acceptance.md. Final completion gated on external prerequisites P01 (GAP-02 durable rate-limit persistence) and P02/P03 (interactive desktop and delay harness).
