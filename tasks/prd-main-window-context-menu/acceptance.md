# Acceptance and Release Readiness: Main Window Context Menu

**Feature:** Main Window Context Menu  
**Author:** SDD Executor (Task T05)  
**Date:** September 7, 2026  
**Status:** Implementation Complete / Final Release Pending Prerequisites (P01, P02, P03)  
**Sources:** [prd.md](prd.md), [techspec.md](techspec.md), [tasks.md](tasks.md)

---

## Executive Summary

Task T05 consolidates the integrated acceptance evidence across all deliverables (T01 through T04) of the Main Window Context Menu feature. All unit, component, presentation, lifecycle, and packaging metadata tests pass with 100% success (271 tests passing, 0 failing, 0 skipped). Single-file executable publishing was verified with semantic versioning and commit metadata reflection. Per the repository's desktop .NET policy, automated E2E is omitted in favor of focused tests and structured manual verification scripts. External prerequisites P01 (GAP-02 durable rate limit persistence) and P02/P03 (interactive desktop verification and delayed provider harness) remain explicitly tracked as pending.

---

## Environment & Build Verification

| Verification ID | Scope / Target | Command | Result | Exit Code | Notes |
| --- | --- | --- | --- | --- | --- |
| **V01** | `TokenHound.App` build | `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` | **PASS** | `0` | 0 errors; 3 known warnings (NU1903 in SQLitePCLRaw.lib.e_sqlite3 2.1.11) |
| **V02 (Build)** | `TokenHound.Infrastructure.Tests` build | `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` | **PASS** | `0` | 0 errors; 2 known warnings (NU1903 in SQLitePCLRaw.lib.e_sqlite3 2.1.11) |
| **V02 (Full)** | Complete test suite | `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` | **PASS** | `0` | 271 passed, 0 failed, 0 skipped (1s 309ms) |
| **V03** | Single-file publish & metadata | `rtk dotnet publish src/TokenHound.App/TokenHound.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:Version=0.0.0-contextmenu.validation --nologo` | **PASS** | `0` | Clean publish to `bin/Release/net10.0-windows/win-x64/publish/` |

### V03 Publish Metadata Inspection

Verification of `TokenHound.App.exe` produced by V03 single-file publish:
- **ProductVersion:** `0.0.0-contextmenu.validation+3cca6761226437744520094de273e97b7b19acb7`
- **FileVersion:** `0.0.0.0`
- **ProductName:** `TokenHound`
- **FileDescription:** `TokenHound.App`

*Conclusion:* The assembly metadata reader (`ApplicationInfo.FromAssembly`) correctly reflects the semantic version and commit hash from `AssemblyInformationalVersionAttribute`, satisfying DEC-06 and CMP-07 for single-file deployments.

---

## Test Suite Execution Summary

All tests executed using the repository's Microsoft.Testing.Platform (MTP) executable route (`--no-build --no-restore -c Release -- --minimum-expected-tests 1`).

| Test Suite / Class | Focus Area | Executed | Passed | Failed | Skipped | Duration |
| --- | --- | :---: | :---: | :---: | :---: | :---: |
| [`HudActionsViewModelTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs) | Menu action concurrency, refresh guard, status text, shutdown cancellation | 9 | 9 | 0 | 0 | 371ms |
| [`UsageStoreTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs) | Scheduled/manual refresh, rate-limit preservation, timeout isolation, snapshot republication | 11 | 11 | 0 | 0 | 482ms |
| [`UsageStoreLifecycleTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreLifecycleTests.cs) | Terminal `StopAsync`, admission closing, work draining, semaphore safety, safe disposal | 8 | 8 | 0 | 0 | 514ms |
| [`ApplicationInfoTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/Presentation/ApplicationInfoTests.cs) | `AssemblyInformationalVersionAttribute` reading, fallback formatting, commit metadata | 10 | 10 | 0 | 0 | 366ms |
| [`NotchViewModelTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs) | Ring initialization, snapshot propagation, provider catalog integration | 6 | 6 | 0 | 0 | 480ms |
| [`ProviderRingViewModelTests`](file:///C:/Users/Admin/.herdr/worktrees/TokenHound/main-menu/tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs) | Angle computation, status mapping, busy indicator semantics | 14 | 14 | 0 | 0 | 351ms |
| **Full Infrastructure Suite** | Complete regression and integration suite | **271** | **271** | **0** | **0** | **1s 309ms** |

---

## Traceability & Acceptance Matrix (TC-01 through TC-10)

| ID | PRD / NFR Obligations | Level | Expected Scenario & Behavior | Evidence & Verified Components | Status |
| --- | --- | --- | --- | --- | :---: |
| **TC-01** | FR-01, NFR-02, US-01–US-04, OBJ-01 | Manual / Code Inspection | Background and cell right-click opens menu with exactly four labels: **Close**, **Refresh**, **Settings**, **About** in that exact order. Arrow keys navigate, Enter activates, Escape or clicking outside dismisses without dispatching action. Right-click does not trigger window dragging. | `NotchWindow.xaml` declares `ContextMenu` on `CapsuleBorder` using `HudContextMenuStyle` with exact 4 items in sequence. `PreviewMouseLeftButtonDown` in `NotchWindow.xaml.cs` handles `DragMove` only for left-clicks. Native WPF ContextMenu provides standard keyboard navigation. Manual acceptance script defined (MAN-01). | **PASS (Code) / Pending Human Session (MAN-01)** |
| **TC-02** | FR-03, FR-04, OBJ-02, US-02 | Unit / Integration | Manual refresh queries all eligible providers immediately without waiting for scheduled timer. Active rate-limit deadlines (`ActiveBlock.ResetTimeUtc`) are preserved without premature network calls. Single-provider timeout/failure does not abort other providers. Unchanged snapshots are republished to UI. | `UsageStore.RefreshNowAsync` serializes execution, respects active blocks, isolates timeouts per provider via linked tokens, and republishes snapshots. Verified by 11 unit tests in `UsageStoreTests`. | **PASS** |
| **TC-03** | FR-05, NFR-04 | Unit | Refresh progress is observable (`IsRefreshing = true`, status text). Concurrent UI refresh clicks while a refresh is in flight are deduplicated and rejected without queueing backlog. Completion or failure clears progress state. | `HudActionsViewModel.RefreshAsync` uses an atomic boolean guard to reject concurrent calls. Status text formulated accurately for in-flight, partial, failed, and empty states. Verified by 9 unit tests in `HudActionsViewModelTests`. | **PASS** |
| **TC-04** | FR-02, FR-08, NFR-04, US-01 | Unit / Integration / Manual | Selecting Close terminates the application process. Running startup/timer/manual operations are canceled and drained before semaphore/resource disposal. Modeless dialogs are closed. Repeated Close calls are idempotent. | `ApplicationLifetime.CloseAsync` coordinates cancellation, `UsageStore.StopAsync` drains in-flight operations, active dialogs are closed via `DialogService`, providers disposed, and `Application.Shutdown` invoked. Verified by 8 unit tests in `UsageStoreLifecycleTests` and `HudActionsViewModelTests`. Manual process exit verified via Windows MCP process management. | **PASS** |
| **TC-05** | FR-06, FR-08, US-03 | Code Inspection / Manual | Settings opens an empty application-styled dialog titled "Settings". Modeless lifecycle (HUD remains accessible). Repeated selection focuses the existing instance without creating duplicates. Dismissal via Escape or close button works and does not exit HUD monitoring. | `SettingsWindow.xaml` styled with dark theme resources (`DialogResources.xaml`) with empty content grid. `DialogService.ShowSettings` maintains single instance, sets owner, and restores/activates. Modeless `Show()` does not disable parent HUD. Manual script defined (MAN-02). | **PASS (Code) / Pending Human Session (MAN-02)** |
| **TC-06** | FR-07, FR-08, OBJ-03, US-04 | Unit / Publish / Manual | About opens a modeless dialog displaying application name ("TokenHound"), PRD description paragraph, and running build version. Long version text wraps cleanly. Missing metadata gracefully falls back to "Version unavailable". Modeless single-instance lifecycle. | `ApplicationInfo.FromAssembly` extracts informational version. `AboutWindow.xaml` binds title, description, and wrapping version text. Verified by 10 unit tests in `ApplicationInfoTests` and verified against V03 single-file publish artifact (`0.0.0-contextmenu.validation+...`). Manual script defined (MAN-02/MAN-05). | **PASS** |
| **TC-07** | NFR-01, NFR-02, NFR-03 | Visual Inspection / Manual | HUD retains non-activating window behavior (`WM_MOUSEACTIVATE` -> `MA_NOACTIVATE`, `SWP_NOZORDER`). Dialogs and context menu render with consistent dark palette. Window placement clamps within monitor work area. Legible and unclipped at 100%, 150%, 200% DPI scaling. | `WindowPlacement.cs` handles native monitor work-area bounds. `NotchWindow.xaml.cs` retains Win32 non-activating hooks. HUD rendered and verified on primary display (100% DPI) via Windows MCP `App` and `Screenshot`. Full multi-DPI review (MAN-03) pending interactive human session. | **PASS (Baseline) / Pending Human Session (MAN-03)** |
| **TC-08** | FR-04, FR-05, NFR-05 | Unit / Source Inspection | Data integrity: null `usedFraction` values remain null (never fabricated into zeroes or percentages); stale snapshots retain original fetched timestamps and provenance. Fallback `MockUsageProvider` completely removed from production composition (`App.xaml.cs`). | `UsageStore.CreateStaleSnapshot` retains prior fidelity and timestamp. `App.xaml.cs` composition reviewed: `MockUsageProvider` removed from production injection (DEC-08/GAP-01). Verified by `UsageStoreTests`, `NotchViewModelTests`, and `ProviderRingViewModelTests`. | **PASS** |
| **TC-09** | FR-04, NFR-05 | Integration / Durable Storage | Persisted rate-limit deadlines: after application restart or coordinator recreation, previously received HTTP 429 deadlines are restored from persistent storage, preventing network calls until expiry. | **UNRESOLVED PREREQUISITE (GAP-02 / P01).** Rate limits are currently preserved in-memory during execution (`UsageStore._activeBlocks`), but cross-restart durable storage is not yet implemented in the Engine. Honestly recorded as blocked/pending P01. | **PENDING (P01 / GAP-02)** |
| **TC-10** | FR-05, NFR-04, PRD error flows | Unit / Manual | Empty provider registry, all-unavailable providers, partial failures, and provider timeouts produce honest user feedback without crashing or freezing the UI. UI remains responsive to Close. | `HudActionsViewModel.FormulateStatusText` generates specific user-facing feedback (`No providers available`, `No counters updated...`, `Refresh completed. Some providers need attention.`). `UsageStore` isolates provider failures and timeouts. Verified by unit tests in `HudActionsViewModelTests` and `UsageStoreTests`. Manual delay harness pending (P03/MAN-04). | **PASS** |

---

## Manual Acceptance Scripts (MAN-01 through MAN-05)

Desktop .NET policy omits automated UI E2E suites. The following manual validation protocols are established for human desktop sessions (P02/P03) and assisted via Windows MCP:

```markdown
### MAN-01: Context Menu & Capsule Navigation
1. Launch TokenHound.App via Windows MCP App tool (`mode="launch_executable"`).
2. Right-click anywhere on the dark HUD capsule background: verify menu opens near pointer.
3. Confirm exact items and order: Close, Refresh, Settings, About.
4. Right-click on an individual provider ring: confirm identical context menu opens.
5. Press Down/Up arrow keys: confirm visible keyboard focus moves through items.
6. Press Escape: confirm menu dismisses immediately without triggering any action.
7. Reopen menu and click outside the menu: confirm dismissal without action.
8. Left-click and drag the capsule: confirm HUD repositions smoothly.
9. Right-click the capsule: confirm menu opens and dragging does NOT initiate.

### MAN-02: Dialog Modeless Lifecycle & Reopening
1. Open context menu, select "Settings".
2. Verify Settings window opens within monitor work area; verify title is "Settings" and content body is clean and empty.
3. While Settings is open, click the HUD: verify HUD is interactive and does not beep or block (modeless).
4. From the HUD menu, select "Settings" again: verify existing Settings window is brought to front (no duplicate window).
5. Press Escape: verify Settings window closes.
6. Open context menu, select "About".
7. Verify About window displays "TokenHound", the full PRD description paragraph, and the build version string.
8. Select "About" again from context menu: verify single instance brought to front.
9. Close About window via standard window close button (X).
10. Confirm HUD remains visible and actively monitoring throughout.

### MAN-03: Visual Theme, Placement, & DPI Scaling
1. Launch app on Display 2 (Primary, 100% DPI): inspect crisp text rendering, margins, and dark styling.
2. Drag HUD near screen top, left, right, and bottom edges; open context menu at each position: verify menu is clamped within visible work area.
3. Open Settings and About near screen edges: verify windows center within current monitor work area (`WindowPlacement`).
4. Keep an external editor or browser active; single-click the HUD capsule: verify external window retains active focus (non-activating HUD).
5. Switch Windows display scaling to 150% and 200%:
   - Verify HUD capsule, rings, context menu, and dialogs scale proportionally without clipping, blurry text, or overlapping elements.
   - Verify About version string wraps properly across multiple lines if needed.

### MAN-04: Refresh Progress & Shutdown During Work
1. Trigger Refresh from context menu.
2. Verify non-activating status popup appears below HUD showing "Refreshing usage...".
3. Verify "Refresh" menu item is disabled during refresh while Close, Settings, and About remain enabled.
4. Attempt rapid clicks on Refresh: verify no duplicate refreshes are dispatched.
5. While refresh is in progress, select "Close":
   - Verify in-flight refresh cancellation is signaled.
   - Verify HUD and any open dialogs close immediately.
   - Verify TokenHound.App process terminates cleanly without hanging or crashing.

### MAN-05: Single-File Publish & Release Version Verification
1. Build release publish artifact via V03 command (`-p:Version=0.0.0-contextmenu.validation`).
2. Run published executable from `bin/Release/net10.0-windows/win-x64/publish/TokenHound.App.exe`.
3. Open About dialog: verify displayed version string reflects `0.0.0-contextmenu.validation+<commit-sha>`.
4. Verify version string wraps without truncation or horizontal scrollbars.
```

---

## Open Prerequisites & Blockers

1. **P01 / GAP-02 (Durable 429 Rate Limit Persistence):**
   - *Status:* **BLOCKED / PENDING EXTERNAL PREREQUISITE**.
   - *Detail:* The repository instructions require durable rate-limit deadline persistence across application restarts. While in-memory rate-limit handling is fully verified (TC-02), durable persistence requires a dedicated storage subsystem and schema not defined in the context menu scope.
   - *Requirement for Release:* P01 must be delivered and verified via TC-09 in isolated storage before overall feature completion.

2. **P02 (Interactive Windows Session for MAN-01 through MAN-03, MAN-05):**
   - *Status:* **PENDING HUMAN REVIEWER SESSION**.
   - *Detail:* HUD positioning and rendering verified via Windows MCP on primary display (100% DPI). Full verification of multi-DPI scaling (150%, 200%), OS focus non-stealing, and physical keyboard/mouse gestures requires human reviewer interaction.

3. **P03 (Safe Delayed/Failing Provider Test Harness for MAN-04):**
   - *Status:* **PENDING HARNESS DEFINITION**.
   - *Detail:* As specified in the TechSpec, live credentials must never be altered to induce failures. Interactive manual verification of slow/failing refresh UI feedback awaits a documented developer mock harness. Unit-level coordination is 100% verified.

---

## Release Readiness Verdict

- **Code & Test Acceptance:** **PASS (100%)** — All 271 automated tests pass; 0 build errors; single-file publishing verified.
- **Architectural Conformance:** **PASS** — Core remains pure; `App.xaml.cs` contains zero mock fallbacks; non-activating HUD style preserved.
- **Feature Release Gate:** **CONDITIONAL** — Production release gated on resolution of P01 (GAP-02 durable rate limit persistence) and completion of human desktop verification (P02/P03).
