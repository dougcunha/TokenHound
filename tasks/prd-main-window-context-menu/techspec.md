# TechSpec: Main window context menu

## Sources and traceability

- Product source: [prd.md](prd.md), read September 7, 2026. This is a new TechSpec; no previous TechSpec or task plan exists in this feature directory.
- Repository constraints: [AGENTS.md](../../AGENTS.md), [ARCHITECTURE.md](../../ARCHITECTURE.md), and the supplied RTK instructions.
- Skills: `sdd-create-techspec` and its .NET profile, `repository-cli-efficiency`, and `dotnet-efficient-validation` with its MTP reference. The `no-workarounds` lifecycle guidance informed the distinction between existing defects and verified behavior.
- Application evidence: [App.xaml.cs](../../src/TokenHound.App/App.xaml.cs), [NotchWindow.xaml](../../src/TokenHound.App/UI/Windows/NotchWindow.xaml), [NotchWindow.xaml.cs](../../src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs), [NotchViewModel.cs](../../src/TokenHound.App/ViewModels/NotchViewModel.cs), and [ProviderRingViewModel.cs](../../src/TokenHound.App/ViewModels/ProviderRingViewModel.cs).
- Engine evidence: [UsageStore.cs](../../src/TokenHound.Infrastructure/Engine/UsageStore.cs), especially `RefreshNowAsync`, `RefreshProviderAsync`, `Stop`, and `Dispose`; [UsageStoreTests.cs](../../tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs).
- Build evidence: [global.json](../../global.json), the App/Infrastructure/Core and existing test project files, [CI](../../.github/workflows/ci.yml), and [release packaging](../../.github/workflows/release.yml).
- Historical design: [earlier design reference](../../docs/design/2026-08-28-usage-notch-design.md). Its macOS platform and older menu do not override the PRD or current Windows application.

## Solution summary

Attach a WPF ContextMenu to the HUD's visible capsule. Keep the existing ring data flow and route application actions through a small presentation coordinator composed by App. Use owned, modeless Settings and About windows so the HUD menu, including Close, remains available. Share dialog/menu resources without introducing a UI library or changing HUD geometry.

Manual refresh uses UsageStore and exposes separate refresh state, not the ring's agent-activity flag. Closing coordinates cancellation and resource ownership before explicit application shutdown. Two existing product gaps, automatic mock data and missing integrated deadline persistence, must remain visible in technical review; this document does not claim that the current engine already satisfies those obligations.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, NFR-02, NFR-03, US-01 through US-04 | Use a native WPF ContextMenu on the capsule Border, with Close, Refresh, Settings, About in exact order. Use explicit handlers into the action coordinator. | No command framework exists. Child provider cells must share the ancestor menu. Retains WPF menu keyboard behavior. | A new command library or custom popup would add dependencies and input responsibilities for four actions. |
| DEC-02 | FR-03 through FR-05, NFR-04, OBJ-02 | Add a WPF-independent `HudActionsViewModel` with observable refresh state and an awaited refresh delegate. Reject repeated UI requests while one request is pending. Preserve UsageStore serialization across startup, timer, and manual calls. | The existing semaphore prevents overlap but queues calls; the UI guard prevents a click burst from creating a refresh backlog. | Do not change the public meaning of every existing `RefreshNowAsync` caller merely to deduplicate menu input. One manual request may wait for an already-running timer cycle. |
| DEC-03 | FR-02, FR-08, NFR-04 | Centralize application shutdown in an App-owned `ApplicationLifetime` coordinator and add a terminal asynchronous stop/drain operation to UsageStore. | Current `Dispose` cancels the timer and immediately disposes a semaphore that an active refresh may still release. Startup refresh is currently untracked. | Blocking the dispatcher or swallowing ObjectDisposedException would conceal the lifetime problem. |
| DEC-04 | FR-04, NFR-05 | Reuse provider adapters, snapshot status, and RateLimitPolicy. Manual refresh bypasses schedule timing only. Treat durable deadline loading as a tracked prerequisite (GAP-02). | Engine checks `ActiveBlock.ResetTimeUtc` before dispatch but only against its in-memory dictionary. No archive implementation exists in the Engine directory. | Adding a persistence subsystem without resolving the existing prerequisite would enlarge this feature. Do not report restart protection as implemented. |
| DEC-05 | FR-06 through FR-08, NFR-02 through NFR-04 | Use one owned modeless SettingsWindow and one owned modeless AboutWindow, managed by `DialogService`. Restore/activate an existing instance; clear its reference on Closed. | Owned windows have a defined relationship with the HUD, while modeless operation keeps Close accessible. | Modal ShowDialog disables its owner; that conflicts with the desired continued access to the HUD menu. |
| DEC-06 | FR-07, OBJ-03 | Read `AssemblyInformationalVersionAttribute` from the App assembly and display its full nonblank value, including prerelease/build metadata. Pass the assembly explicitly into a testable metadata reader. | Release and CI already supply `-p:Version`; SDK metadata can include a source revision. No file-location lookup is needed for single-file deployment. | AssemblyVersion alone loses prerelease detail. If information is missing, show `Version unavailable` and fail release acceptance rather than fabricate a version. |
| DEC-07 | NFR-01 through NFR-03, FR-06, FR-07 | Reuse the current dark HUD palette and a shared resource dictionary for readable dialog/menu controls. Keep native focus semantics and standard dialog chrome. | App.xaml currently has no shared resources or dialog convention; the capsule already defines a dark visual baseline. | Custom borderless chrome adds resizing, keyboard, and accessibility work without a stated product need. Exact visual refinement is validated manually. |
| DEC-08 | FR-04, FR-05, NFR-05 | In production composition, stop injecting the mock fallback into NotchViewModel. Preserve the optional mock provider for explicit tests. | App currently supplies MockUsageProvider, and NotchViewModel registers it automatically when credentials are absent. This would make an unavailable refresh look like real data. | Removing the optional testing facility is unnecessary. The composition change addresses the production cause without redesigning adapters. |

WPF supports explicit ownership for modeless windows; this is the basis for DEC-05. See [Microsoft's WPF window overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/). DEC-06 uses the version-information attribute documented in [AssemblyInformationalVersionAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assemblyinformationalversionattribute?view=net-10.0).

## Components and flow

Paths below are relative to the repository root and describe proposed changes, not files already created by this document.

| ID | Component | State | Responsibility and integration |
| --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.App/UI/Windows/NotchWindow.xaml` and `.xaml.cs` | Modify | Declare the four menu items, forward events to CMP-02, show refresh feedback, retain left-button dragging and existing non-activation hook. |
| CMP-02 | `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` | Create | Own IsRefreshing, IsClosing, and RefreshStatusText; await injected refresh/shutdown operations and invoke injected dialog actions. No WPF types or credential access. |
| CMP-03 | `src/TokenHound.App/ApplicationLifetime.cs` and `App.xaml.cs` | Create/modify | Own startup/manual task references, lifetime cancellation, disposable providers, and shutdown. Compose CMP-02/CMP-04; remove production mock injection. |
| CMP-04 | `src/TokenHound.App/UI/Windows/DialogService.cs` | Create | Own dialog references, owner assignment, activation, initial placement, and closure. Called only on the WPF dispatcher. |
| CMP-05 | `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` and `.xaml.cs` | Create | Empty content area, title, normal close behavior, and Escape handler. |
| CMP-06 | `src/TokenHound.App/UI/Windows/AboutWindow.xaml` and `.xaml.cs` | Create | App name, PRD description, and version supplied by CMP-07; standard close and Escape handling. |
| CMP-07 | `src/TokenHound.App/Presentation/ApplicationInfo.cs` | Create | Immutable metadata record plus its assembly-reading factory; explicit required name/description/version-display fields. Keep one top-level type in the file. |
| CMP-08 | `src/TokenHound.App/UI/Styles/DialogResources.xaml` and `App.xaml` | Create/modify | Shared surface/text/control resources and visible keyboard/hover/disabled states; merge once at app scope. |
| CMP-09 | `src/TokenHound.Infrastructure/Engine/UsageStore.cs` | Modify | Add safe terminal cancellation/draining; preserve polling, rate checks, snapshot events, and per-provider error isolation. Extract a lifecycle helper within Engine if needed to meet file/method limits. |
| CMP-10 | `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `ViewModels/HudActionsViewModelTests.cs`, `Presentation/ApplicationInfoTests.cs`, `Engine/UsageStoreLifecycleTests.cs`, and existing `Engine/UsageStoreTests.cs` | Modify/create | Extend the established source-link pattern for WPF-independent presentation classes; test coordination and metadata without opening WPF windows. |

The window keeps its existing NotchViewModel DataContext for Rings. Expose the separate actions model through a named root-window property and bind the status presenter explicitly. ContextMenu exists outside the usual visual tree: do not assume it inherits the window's DataContext; resolve the action model from its PlacementTarget or set it explicitly when opening.

Refresh flows from the menu to CMP-02, through an App-composed delegate into UsageStore, then through existing SnapshotUpdated events into NotchViewModel and provider rings. Completion updates the actions model. UI-bound state changes run on the dispatcher; Infrastructure awaits use ConfigureAwait(false). Do not synchronously block the dispatcher to obtain refresh results.

Close sets the terminal closing state once, prevents new action dispatch, cancels startup/manual work, stops and drains the engine, detaches snapshot observers, disposes App-owned providers/monitors that implement IDisposable, and calls Application.Shutdown on the dispatcher. OnExit remains idempotent cleanup, not the primary asynchronous drain boundary. All windows close through application shutdown.

## Contracts and data

### Presentation coordination

- `HudActionsViewModel.RefreshAsync(CancellationToken)` returns Task. IsRefreshing becomes true before awaiting; concurrent UI calls return without another refresh. Finally clears progress unless the model is closing. Exceptions become concise status text at this boundary; expected lifetime cancellation during shutdown is not shown as a provider error.
- `CloseAsync()` returns a shared terminal task for repeated calls. After closing begins, Refresh, Settings, and About cannot dispatch new work.
- `IsRefreshing` and `IsClosing` are read-only observable booleans. `RefreshStatusText` is an optional observable string. No persistent settings or new domain DTO is introduced.
- Refresh feedback appears in a small owned status Popup anchored below the HUD, outside the ring layout, so it cannot trigger NotchWindow.SizeChanged repositioning. It is non-activating and contains no input controls. Show `Refreshing usage...` while pending. Completion/error feedback remains available until the next action or dismissal of the presentation surface; do not add arbitrary time-based behavior as a concurrency mechanism.
- Disable only Refresh while it is pending; retain its exact label. Do not set ProviderRingViewModel.IsBusy, which means agent execution activity.
- Completion summarizes the actual CurrentSnapshots: normal completion uses `Refresh completed`; no registered snapshots uses `No providers available`; if no provider has usable current data, use `No counters updated. Check provider status.` Mixed results use `Refresh completed. Some providers need attention.` A rate-limited existing snapshot is not a successful update. Provider details remain the source of specific status and reset information.

### Engine lifetime

Add `StopAsync(CancellationToken)` as a terminal operation distinct from existing restartable Stop. Its operation closes admission to new refresh/tick calls, requests cancellation through a store-owned token linked into every admitted operation, awaits the timer and admitted operations, and only then permits synchronization-resource disposal. Calls after terminal stopping receive an explicit disposed/stopping failure, and concurrent StopAsync callers observe the same completion.

Track admission and active operations together under one lifecycle lock; a cancellation request alone does not prove a task has completed. Do not dispose a semaphore while holders or waiters exist. Preserve IDisposable compatibility: synchronous disposal requests terminal stop and arranges resource release after the tracked drain, without synchronously waiting on UI callbacks. App awaits StopAsync before final disposal. Existing Stop/Start timer behavior stays covered by regression tests.

Provider cancellation must reach underlying I/O. A provider-local timeout that throws OperationCanceledException while the store/caller token is not canceled is a provider failure and must not abort later providers. A canceled store/caller token terminates the cycle. No new retry policy or timeout duration is introduced.

### Metadata

Read the supplied App assembly, not the test host or SDK version. Product name and description are fixed product text; version comes from informational metadata. Preserve the full version and wrap long text. Missing metadata is an explicit unavailable state and a packaging acceptance failure. There are no configuration writes, endpoints, migrations, or new NuGet dependencies in this design.

## Interfaces, errors, and recovery

- Settings/About use Owner = the HUD, Show rather than ShowDialog, ShowInTaskbar=false, and normal activating window styles. Never apply WindowStyles.EnableNonActivating to those dialogs. Closing them must not call Application.Shutdown.
- Position dialogs in the work area of the HUD's monitor. Convert native monitor coordinates to WPF units where needed; clamp initial bounds to that work area after measuring. Native ContextMenu placement should constrain the menu; confirm at each screen edge. If a placement helper is necessary, create `src/TokenHound.App/Interop/WindowPlacement.cs` and keep all Win32 declarations there, outside Core.
- Preserve WM_MOUSEACTIVATE/MA_NOACTIVATE and the existing SWP_NOZORDER behavior on the HUD. Explicit menu interaction must support focus inside the menu without turning ordinary HUD clicks into activation.
- Shared styles must retain focus, hover, disabled, and accessible-name behavior. About uses a prominent TokenHound title, the PRD's short paragraph, and a separate readable version row. Settings has no settings body or Save/Apply controls. The existing dark surface is the visual reference; no new branding imagery is required.
- Preserve null used fractions. For engine-generated stale snapshots with previous data, retain the last valid sample's windows, fidelity, and fetched timestamp, and keep applicable block information. A failure must not relabel an old sample as freshly fetched official data. With no prior sample, emit empty windows and an explicit unavailable/stale state.
- Do not show credentials, raw response bodies, or token-bearing exception strings in new refresh summaries. Specific existing provider error text should be reviewed where it reaches the tooltip; no new broad logging framework is justified.
- Do not erase stored state or provider credentials on Close, dialog dismissal, rollback, or refresh failure.

## Test approach

### Validation profile

The App project uses Microsoft.NET.Sdk, net10.0-windows, UseWPF=true, and a WPF Application startup path: this is desktop .NET. Core and Infrastructure target net10.0. Both existing test projects target net10.0 and produce executables with UseMicrosoftTestingPlatformRunner=true, xunit.v3.mtp-v2 4.0.0, and MTP extensions. Tests already link WPF-independent App source into Infrastructure.Tests; preserve this pattern instead of adding an App test project or changing its TFM.

global.json requests SDK 10.0.400 with latestFeature roll-forward; `rtk dotnet --version` returned 10.0.400 during inspection. No repository Directory.Build.props/targets or Directory.Packages.props was found. Build with the existing SDK imports and packages. CI explicitly uses the MTP executable route because `dotnet test` orchestration previously discovered zero tests in this repository.

**E2E: omitted by desktop .NET policy.** The profile reference calls this the .NET desktop policy. Exclude full-application automation, desktop UI automation, and browser/WebView suites. Do not remove existing tests. Manual acceptance below preserves UI obligations and remains pending until a human performs it.

No builds or tests were executed for this documentation-only change. The following commands are for implementation validation, serialized to avoid shared bin/obj races. Restore only when assets are missing or incompatible; then build affected projects once in Release:

```powershell
rtk dotnet restore src/TokenHound.App/TokenHound.App.csproj --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

For each test class in the matrix, substitute its class name for `<ClassName>`:

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*<ClassName>*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Use `--list-tests` through the same route only if names/discovery need checking; keep the minimum enforced on execution. Inspect executed/failed/skipped counts and stdout. If RTK hides failure detail, rerun only that failing class with `rtk proxy dotnet run` and the same arguments. Core policy tests need their own compatible Core.Tests build only if policy behavior changes or GAP-02 work makes them relevant. Publishing is reserved for the metadata packaging check; use the existing release workflow's single-file win-x64 options and a known Version value, not a new packaging route.

| ID | Obligations | Level | Scenario and expected evidence | Project/class or manual script |
| --- | --- | --- | --- | --- |
| TC-01 | FR-01, NFR-02, US-01 through US-04, OBJ-01 | Manual | Background/cell right-click shows exact order once; arrows/Enter work; Escape/outside click dismiss without actions; right-click does not drag. | MAN-01 |
| TC-02 | FR-03, FR-04, OBJ-02, US-02 | Unit | Multiple providers, unchanged values, future polling deadline, one thrown failure, one active block. Eligible providers run, unchanged snapshots still publish, one failure does not stop others, blocked provider receives no extra call. | UsageStoreTests |
| TC-03 | FR-05, NFR-04 | Unit | Gate a refresh with TaskCompletionSource, invoke repeatedly, observe one dispatch and visible state transitions; release/fail/cancel it. No duplicate backlog or stuck refreshing state. | HudActionsViewModelTests |
| TC-04 | FR-02, FR-08, NFR-04, US-01 | Unit + manual | Close during startup/timer/manual work and while a waiter exists; admitted operations are canceled/drained before resource disposal; repeated close is safe; process exits with both dialogs open. | UsageStoreLifecycleTests; MAN-04 |
| TC-05 | FR-06, FR-08, US-03 | Manual | Empty Settings, Escape and close control, reopen, repeated activation uses the same instance; no persistence change; monitoring continues. | MAN-02 |
| TC-06 | FR-07, FR-08, OBJ-03, US-04 | Unit + manual | Metadata reader receives an explicit assembly; handles prerelease/full metadata and missing attribute honestly. About matches the running normal and published build. | ApplicationInfoTests; MAN-02/MAN-05 |
| TC-07 | NFR-01 through NFR-03 | Manual | Dialog/menu readability, keyboard focus, work-area placement, ordinary click/drag non-activation, and stable HUD position at 100/150/200% scaling. | MAN-03 |
| TC-08 | FR-04, FR-05, NFR-05 | Unit + source inspection | Null fractions stay null; stale data retains provenance; no new credential-write route; production App no longer injects fallback mock data; missing credentials show real provider states. | UsageStoreTests, existing NotchViewModelTests/ProviderRingViewModelTests, App composition review |
| TC-09 | FR-04, NFR-05 | Integration, prerequisite pending | Persist a future deadline in isolated test storage, recreate coordinator, request refresh, verify zero network dispatch until expiry. Also cover Retry-After: 0 using the existing policy floor. | GAP-02 resolution must identify archive integration and test class before implementation acceptance. |
| TC-10 | FR-05, NFR-04, PRD empty/error journeys | Unit + manual | Empty registry, all unavailable providers, partial failure, and provider-local timeout. Feedback is honest, UI stays responsive, later eligible providers are still attempted. | HudActionsViewModelTests, UsageStoreTests; MAN-04 |

### Manual acceptance script

Owner: the implementing developer or designated Windows reviewer. Record build/version, scaling, scenario result, and screenshot where visual evidence is needed. These checks are not automated E2E.

- MAN-01: Launch the built app on the user's desktop, using the repository-prescribed Windows MCP App launch capability if the agent assists with launch. A human opens the menu on background and cells and performs TC-01. No shell Start-Process launch is used for visible acceptance.
- MAN-02: Human opens Settings and About, tests both close routes, repeats open actions, and confirms single instances and continued HUD monitoring. Inspect the version text and empty settings body.
- MAN-03: At each requested Windows scaling, human checks menu/dialog text and focus and moves the HUD near work-area edges before opening each surface. Keep an editor focused during ordinary HUD click/drag and confirm focus preservation. The repository's primary-monitor screenshot selection is `display: [2]` when screenshots are captured.
- MAN-04: With a controlled delayed/failing provider setup supplied by the unit fixture or a separately documented development harness, verify refresh status and Close during work. Never modify real credentials to manufacture a failure. If no safe harness is available, record the UI slow/error scenario as pending rather than claim fixture-only evidence proves it.
- MAN-05: Compare About against the build's informational version, then repeat on an existing single-file release/CI artifact or a local publish using its established options. Preserve prerelease/build metadata and confirm text wraps.

## Observability and rollout

The visible refresh status and existing provider tooltip status are the user-facing signals. No telemetry collection, performance percentage, new logging dependency, or persistent settings file is introduced. Implementation review must distinguish a completed cycle from all providers successfully returning data.

Rollout requires focused tests, a successful App build, completed essential manual evidence, and explicit resolution of GAP-02. The menu itself needs no migration or feature flag. Reversal consists of reverting this feature's code changes through normal version control and rebuilding; do not delete provider state. Any separately delivered persistence prerequisite needs its own backward-compatible rollback policy.

## Risks and open items

- GAP-01, resolved by DEC-08: production mock fallback conflicts with the PRD's honest unavailable-data behavior. Remove its injection at App composition; retain explicit test use. This is a necessary consequence of FR-04/FR-05, not a provider redesign.
- GAP-02, unresolved prerequisite: repository instructions require durable 429 deadlines, but inspected UsageStore contains only in-memory snapshots and the Engine directory contains no archive. NFR-05 and TC-09 cannot be marked satisfied from the current code. Technical owner must locate an already-planned persistence implementation or define a separately scoped prerequisite and its integration before this feature's final acceptance. No waiver or architecture is invented here, and this TechSpec does not silently add a storage subsystem.
- Lifecycle risk: a provider that ignores cancellation can delay graceful shutdown. TC-04 must verify the actual cancellation chain for the registered providers, not only substitutes. If a provider violates its contract, record and fix that cause within an explicitly identified provider change; do not use process killing or arbitrary sleeps to conceal it.
- Input risk: non-activating HUD and focused native menu behavior must be checked on Windows. Pure unit tests cannot establish OS focus or DPI correctness.
- Metadata risk: linked-source tests can accidentally read the test assembly; explicit assembly injection and the single-file manual check prevent a false positive.
- Layout risk: the current HUD repositions on SizeChanged. The refresh surface must not participate in its measured ring layout.
- Existing fidelity/timestamp changes on an engine error are addressed in the narrow stale-snapshot mapping specified above. Adapter-provided snapshots retain their own contract; broader adapter audits are outside this feature.

All FR-01 through FR-08, NFR-01 through NFR-05, user stories, outcomes, and empty/error journeys map to decisions and evidence above. TC-09 is explicitly pending; all other execution evidence is planned, not claimed as passed. No existing task plan is invalidated. This document is ready for technical review and plan preparation, with GAP-02 visible as a prerequisite to full acceptance.

## Relevant files

The CMP table is the proposed implementation file inventory. Only `tasks/prd-main-window-context-menu/techspec.md` is written in this stage. The PRD, architecture, skills, application code, tests, workflows, and provider specs remain untouched. Sequencing and assignment belong in the subsequent task plan, not this TechSpec.
