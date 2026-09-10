# Implementation plan — System Tray NotifyIcon

## Stable sources

- PRD: `tasks/prd-system-tray-notifyicon/prd.md` (approved HIL 1 / workflow.md D-003; sha256 `a4d491a7c796b52e1438e74eb6113198099bd3ec1b18fd0c36c11afe0c3203f7`)
- TechSpec: `tasks/prd-system-tray-notifyicon/techspec.md` (sha256 `c89f9d9e0bae37fd83b95a1e17be10435381b526d15271acd0953eb6491cadd9`)

> Common sources are read before the task and before mutable state; read order does not guarantee a host cache hit.
> This is the authorised HIL-2 restructure (workflow.md D-004) of the prior 6-task plan into exactly 3 tasks. Former `task_04.md` / `task_05.md` / `task_06.md` are deleted as superseded (old T01+T02+T03 → T01, old T04+T05 → T02, old T06 → T03). No `done/task_*.md` exists. Task IDs are T01–T03 (files `task_01.md`..`task_03.md`).

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | WPF-free tray seam (`ITrayIcon`, `TrayMenuItemKey`, `TrayMenuModel` + record DTOs, `NotchVisibilityController`); Notch context-menu "Close" → "Hide" relabel + `Window.Closing` intercept + `ShowNotch`/`HideNotch`; behaviour-preserving rename of `HudActionsViewModel`'s terminal member to shutdown semantics (+ its 3 affected tests) | — | T02, T03 |
| T02 | `TrayIconViewModel`: every `TrayMenuItemKey` mapped to a reused HUD/visibility action with no duplicated body; toggle header tracks visibility. `TrayIconHost` coordinator: single-create/dispose guards, dispatcher marshalling, structured logs, graceful-degradation fallback | T01 | T03 |
| T03 | `Hardcodet.NotifyIcon.Wpf 2.0.1` package + `TaskbarIconAdapter`; `App.InitializeUi` composition + `App.ShutdownAsync()` bypass wrapper; test-project `<Compile Include>` links; polling-continuity integration test; full aggregate validation; MAN-01 handed to the coordinator | T01, T02 | — |

The DAG is a linear acyclic chain; `T01` is the sole root and the critical path runs through every node.

```
T01 ──► T02 ──► T03
```

`T01` merges the former T01–T03: the seam foundation (seam DTOs + `NotchVisibilityController` unlock everything downstream and have no reviewable behaviour beyond their headless tests) is bundled with the two behaviour-preserving edits it must precede so each owned file keeps a single writer. The executor follows a fixed internal order: (1) create the seam DTOs + `ITrayIcon` + `TrayMenuModel` + `NotchVisibilityController`; (2) relabel `NotchWindow`'s "Close" item to "Hide", add the `Window.Closing` intercept and `ShowNotch`/`HideNotch`, and repoint `OnCloseClick` to the injected `CloseIntercepted` hide delegate — this removes the last non-test caller of the terminal member; (3) rename `HudActionsViewModel` (`closeAction`→`shutdownAction`, `CloseAsync`→`ShutdownAsync`, `_closeTask`→`_shutdownTask`, `_isClosing`→`_isShuttingDown`, `IsClosing`→`IsShuttingDown`) and its three affected tests in `HudActionsViewModelTests.cs`. Step 2 before step 3 leaves `HudActionsViewModelTests.cs` the only remaining caller of the renamed member, so the rename compiles clean under one writer and `src/TokenHound.App` builds at every step; the positional constructor arg in `App.xaml.cs` (a `T03` file) still compiles because only the parameter name changes. `T01`'s in-task gate is a green `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` plus `*HudActionsViewModelTests*` green (both files are already compiled into the test project).

`T02` merges the former T04+T05: `TrayIconViewModel` (command→action mapping, reused bodies) and `TrayIconHost` (WPF-free coordinator, graceful degradation, dispatcher marshalling, structured logs). It depends on `T01` for the seam types and the renamed `HudActionsViewModel.ShutdownAsync`. `T02`'s in-task gate is a green `src/TokenHound.App` build; the new Tray test classes (`TrayIconViewModelTests`, `TrayIconHostTests`) first EXECUTE under `T03` once CMP-12's `<Compile Include>` links land — the same is true of `T01`'s `NotchVisibilityControllerTests` and `TrayMenuModelTests`.

`T03` is the former T06 unchanged in scope. Both `.csproj` edits (`src/TokenHound.App/TokenHound.App.csproj` package reference, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` `<Compile Include>` links) and `src/TokenHound.App/App.xaml.cs` are consolidated into `T03` as its exclusive write scope. `T03` adds the five links so every seam test first compiles and executes; runs all six focused suites by name plus the aggregate, each with `--minimum-expected-tests 1`, `$LASTEXITCODE` preserved, never `Out-Null`; and hands MAN-01 (PRD steps 1–11) to the coordinator for HIL 3. The DAG (`T03` after `T01` and `T02`) guarantees no coverage is lost.

Every file in the "Relevant files" split is written by exactly one task; both `.csproj` edits and `App.xaml.cs` are in `T03`.

## Tasks

- [T01 — Tray seam, Notch hide, and HudActionsViewModel rename](done/task_01.md): WPF-free seam (`ITrayIcon`, `TrayMenuItemKey`, `TrayMenuModel` + `TrayMenuEntry`/`TrayMenuDescriptor` records, `NotchVisibilityController`); `NotchWindow` "Close" → "Hide" relabel + `Window.Closing` intercept + `ShowNotch`/`HideNotch`; behaviour-preserving rename of `HudActionsViewModel`'s terminal member to shutdown semantics (+ its affected tests). **State: done.**
- [T02 — TrayIconViewModel command mapping and TrayIconHost coordinator](done/task_02.md): every `TrayMenuItemKey` mapped to a reused HUD/visibility action with no duplicated body; toggle header tracks visibility; `TrayIconHost` single-create/dispose guards, dispatcher marshalling, structured logs, graceful-degradation fallback. **State: done.**
- [T03 — Hardcodet adapter, App composition, test links, and validation](done/task_03.md): `Hardcodet.NotifyIcon.Wpf 2.0.1` + `TaskbarIconAdapter`; `App.InitializeUi` composition + `App.ShutdownAsync()` bypass wrapper; test-project `<Compile Include>` links; polling-continuity integration test; full aggregate validation; MAN-01 handed to the coordinator. **State: done.**

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Single persistent notification-area presence | T02, T03 | TC-01, TC-16 |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Restore the Notch after hide/close | T01, T03 | TC-03, TC-05, TC-16 |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Hiding/closing the Notch never terminates the app | T01, T03 | TC-10, TC-11, TC-16 |
| OBJ-04 | `prd.md#outcomes-and-metrics` | `Exit` is the sole coordinated shutdown path | T01, T02, T03 | TC-08, TC-16 |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Tray reuses `HudActionsViewModel` command logic | T02 | TC-06, TC-07, TC-08 |
| OBJ-06 | `prd.md#outcomes-and-metrics` | Consistent Notch/tray lifecycle labels | T01, T02 | TC-04, TC-09 |
| FR-01 | `prd.md#functional-requirements` | Icon identity + single create/dispose | T02, T03 | TC-01, TC-16 |
| FR-02 | `prd.md#functional-requirements` | Tray menu contents and fixed order | T01 | TC-02 |
| FR-03 | `prd.md#functional-requirements` | One visibility transition per gesture | T01, T02 | TC-03 |
| FR-04 | `prd.md#functional-requirements` | Toggle header reflects Notch visibility | T01, T02 | TC-04 |
| FR-05 | `prd.md#functional-requirements` | Show via `ApplyPlacement`, no focus steal | T01 | TC-05, TC-16 |
| FR-06 | `prd.md#functional-requirements` | "Refresh Now" reuses the guarded refresh path | T02 | TC-06 |
| FR-07 | `prd.md#functional-requirements` | "Settings…"/"About…" reuse the modeless dialogs | T02 | TC-07 |
| FR-08 | `prd.md#functional-requirements` | "Exit" idempotent, sole shutdown | T01, T02, T03 | TC-08, TC-16 |
| FR-09 | `prd.md#functional-requirements` | Notch "Close" item becomes "Hide" | T01 | TC-09, TC-16 |
| FR-10 | `prd.md#functional-requirements` | No Notch-close path terminates the app | T01, T03 | TC-10, TC-11 |
| FR-11 | `prd.md#functional-requirements` | Background lifecycle continuity while hidden | T03 | TC-11 |
| FR-12 | `prd.md#functional-requirements` | Graceful degradation if the icon cannot be created | T02 | TC-12 |
| NFR-01 | `prd.md#non-functional-requirements` | Layer purity; WPF-free seam behind a boundary | T01, T02, T03 | TC-15 |
| NFR-02 | `prd.md#non-functional-requirements` | Style/complexity limits | T01–T03 | TC-15 + review |
| NFR-03 | `prd.md#non-functional-requirements` | Only `Hardcodet.NotifyIcon.Wpf`, version-pinned | T03 | CMP-11 build step |
| NFR-04 | `prd.md#non-functional-requirements` | Target frameworks / `OutputType` / `UseWPF` unchanged | T03 | CMP-11 build step |
| NFR-05 | `prd.md#non-functional-requirements` | No busy-wait/polling for visibility; event-driven | T01, T02 | TC-03, TC-13, TC-16 |
| NFR-06 | `prd.md#non-functional-requirements` | Focus safety; non-activating styles + hook intact | T01 | TC-05, TC-16 |
| NFR-07 | `prd.md#non-functional-requirements` | Exactly one shutdown; icon always removed | T01, T02, T03 | TC-01, TC-08 |
| NFR-08 | `prd.md#non-functional-requirements` | Tray callbacks marshalled to the WPF dispatcher | T02 | TC-13 |
| NFR-09 | `prd.md#non-functional-requirements` | Structured Serilog entry per action/lifecycle event | T02 | TC-14 |
| NFR-10 | `prd.md#non-functional-requirements` | Accessible OS menu; multi-frame icon | T01, T03 | TC-02, TC-16 |
| NFR-11 | `prd.md#non-functional-requirements` | Headless testability, no live `NotifyIcon`/message loop | T01, T02 | TC-01–TC-14 |
| DEC-01 | `techspec.md#technical-decisions` | Adopt `Hardcodet.NotifyIcon.Wpf` `2.0.1` pinned; `TaskbarIcon` never leaves the adapter | T03 | CMP-02, CMP-11 / TC-16 |
| DEC-02 | `techspec.md#technical-decisions` | Behaviour-preserving rename of the terminal member; separate `NotchVisibilityController`; compose in `TrayIconViewModel` | T01, T02 | CMP-03, CMP-06, CMP-08 / TC-04, TC-06–TC-08 |
| DEC-03 | `techspec.md#technical-decisions` | Intercept Notch close; "Exit" bypass via `AllowClose()` | T01, T03 | CMP-03, CMP-09, CMP-10 / TC-05, TC-09–TC-11 |
| DEC-04 | `techspec.md#technical-decisions` | New code under `UI/Tray/`; wire-up in `App.InitializeUi` | T01, T02, T03 | CMP-01–CMP-07, CMP-10 / TC-15 |
| DEC-05 | `techspec.md#technical-decisions` | Link seam into `Infrastructure.Tests` via `<Compile Include>`; no `TokenHound.App.Tests` for v1 | T03 | CMP-12 / TC-15 |
| DEC-06 | `techspec.md#technical-decisions` | `TrayIconHost` WPF-free/linkable; WPF `ContextMenu` only in the adapter | T02, T03 | CMP-07, CMP-02 / TC-01, TC-12, TC-13 |
| DEC-07 | `techspec.md#technical-decisions` | Graceful degradation: `try/catch` → warning + `IsDegraded`, no rethrow | T02 | CMP-07 / TC-12 |
| DEC-08 | `techspec.md#technical-decisions` | Icon + copy from the shipped resource and `UPPER_CASE` constants | T01, T03 | CMP-05, CMP-02 / TC-02, TC-14 |
| CMP-01 | `techspec.md#components-and-flow` | `ITrayIcon` seam interface | T01 | TC-15 |
| CMP-02 | `techspec.md#components-and-flow` | `TaskbarIconAdapter` (sole `ITrayIcon` impl, only package consumer) | T03 | TC-16 (manual) |
| CMP-03 | `techspec.md#components-and-flow` | `NotchVisibilityController` state machine | T01 | TC-03, TC-05, TC-09, TC-10 |
| CMP-04 | `techspec.md#components-and-flow` | `TrayMenuItemKey` enum | T01 | TC-02 |
| CMP-05 | `techspec.md#components-and-flow` | `TrayMenuModel` + `TrayMenuEntry`/`TrayMenuDescriptor` records | T01 | TC-02, TC-04 |
| CMP-06 | `techspec.md#components-and-flow` | `TrayIconViewModel` (composes HUD + controller + menu model) | T02 | TC-04, TC-06, TC-07, TC-08, TC-14 |
| CMP-07 | `techspec.md#components-and-flow` | `TrayIconHost` coordinator | T02 | TC-01, TC-12, TC-13, TC-14 |
| CMP-08 | `techspec.md#components-and-flow` | `HudActionsViewModel` behaviour-preserving rename | T01 | TC-08 + `*HudActionsViewModelTests*` |
| CMP-09 | `techspec.md#components-and-flow` | `NotchWindow.xaml` + `.xaml.cs` relabel + intercept + `ShowNotch`/`HideNotch` | T01 | TC-09 (seam) + TC-16 |
| CMP-10 | `techspec.md#components-and-flow` | `App.xaml.cs` wiring + `App.ShutdownAsync()` wrapper | T03 | TC-16 |
| CMP-11 | `techspec.md#components-and-flow` | `TokenHound.App.csproj` package reference | T03 | build step |
| CMP-12 | `techspec.md#components-and-flow` | `TokenHound.Infrastructure.Tests.csproj` five `<Compile Include>` links | T03 | TC-15 |
| CMP-13 | `techspec.md#components-and-flow` | `Tray/*Tests.cs` + `Engine/NotchHiddenPollingTests.cs` | T01, T02, T03 | TC-01–TC-14, TC-11 |
| CMP-14 | `techspec.md#components-and-flow` | `HudActionsViewModelTests.cs` rename in the 3 affected tests | T01 | `*HudActionsViewModelTests*` |
| TC-01 | `techspec.md#test-approach` | Host init once / idempotent second init / dispose twice | T02 authored, T03 executed | `*TrayIconHostTests*` |
| TC-02 | `techspec.md#test-approach` | `BuildDescriptor` order, separator, headers | T01 authored, T03 executed | `*TrayMenuModelTests*` |
| TC-03 | `techspec.md#test-approach` | Toggle twice; redundant `Hide()` no-op | T01 authored, T03 executed | `*NotchVisibilityControllerTests*` |
| TC-04 | `techspec.md#test-approach` | `ResolveToggleHeader` + `PropertyChanged(nameof(ToggleHeader))` | T01, T02 authored, T03 executed | `*TrayMenuModelTests*`, `*TrayIconViewModelTests*` |
| TC-05 | `techspec.md#test-approach` | `Show()` invokes the placement delegate once; no activate/focus delegate | T01 authored, T03 executed | `*NotchVisibilityControllerTests*` |
| TC-06 | `techspec.md#test-approach` | `RefreshNow()` twice while pending → one dispatch | T02 authored, T03 executed | `*TrayIconViewModelTests*` |
| TC-07 | `techspec.md#test-approach` | `ShowSettings`/`ShowAbout` before/after `Exit()` | T02 authored, T03 executed | `*TrayIconViewModelTests*` |
| TC-08 | `techspec.md#test-approach` | `Invoke(Exit)` twice → shutdown once; no other key shuts down | T01, T02 authored, T03 executed | `*TrayIconViewModelTests*`, `*HudActionsViewModelTests*` |
| TC-09 | `techspec.md#test-approach` | Notch "Hide" path → `_hideNotch`, never shutdown | T01 authored, T03 executed | `*NotchVisibilityControllerTests*` |
| TC-10 | `techspec.md#test-approach` | `ShouldInterceptClose` one-way; intercept → `_hideNotch` once | T01 authored, T03 executed | `*NotchVisibilityControllerTests*` |
| TC-11 | `techspec.md#test-approach` | Polling continues >= 2 ticks after `Hide()`; no `UsageStore` reference | T03 | `*NotchHiddenPollingTests*` |
| TC-12 | `techspec.md#test-approach` | `Show` throws → no rethrow, one `Warning`, `IsDegraded == true` | T02 authored, T03 executed | `*TrayIconHostTests*` |
| TC-13 | `techspec.md#test-approach` | Every callback runs inside the injected dispatcher | T02 authored, T03 executed | `*TrayIconHostTests*` |
| TC-14 | `techspec.md#test-approach` | One structured entry per action + per lifecycle event; `nameof` tokens | T02 authored, T03 executed | `*TrayIconHostTests*`, `*TrayIconViewModelTests*` |
| TC-15 | `techspec.md#test-approach` | Test project compiles the seam with the 5 links and no `UseWPF` | T03 | build step + review |
| TC-16 | `techspec.md#test-approach` | PRD manual acceptance script steps 1–11 | T03 | MAN-01 (owner: coordinator, HIL 3) |

## Coverage gate

- Coverage: Pass. Every DEC-01..08, CMP-01..14, TC-01..16, and all FR-01..12 / NFR-01..11 / OBJ-01..06 map to at least one of T01/T02/T03; no orphan, and nothing is dropped versus the prior 6-task plan (old T01+T02+T03 → T01, old T04+T05 → T02, old T06 → T03). The TechSpec "Relevant files" create/modify/impact split maps one-to-one (see per-task "Affected files"); the three impact-only artefacts (`ARCHITECTURE.md`, `docs/ROADMAP.md`, `graft/`) remain pending coordinator items, edited by no task.
- Traceability: Pass. PRD objectives/FRs/NFRs, TechSpec decisions/components, test cases, and the manual script all appear above with owning tasks. DEC-02 is the full behaviour-preserving rename settled at HIL 2 (workflow.md D-005) and is no longer an open item.
- Dependencies: Pass. Acyclic linear chain `T01 → T02 → T03`; the sole root is `T01`; every "Depends on" points to a lower-numbered task. Critical path = all three nodes.
- Atomicity: Pass. Each task is a vertical slice — implementation plus its authored tests in the same task. `T01` bundles the seam foundation with the two behaviour-preserving edits it must precede (`NotchWindow` relabel/intercept, `HudActionsViewModel` rename); the executor follows the fixed internal order recorded in the Dependency-graph narrative so `src/TokenHound.App` stays buildable at every step and each owned file keeps a single writer.
- Executability: Conditional pass. `T01`'s `*HudActionsViewModelTests*` runs in-task (both files are already compiled into the test project); its `NotchVisibilityControllerTests` / `TrayMenuModelTests` and all of `T02`'s `Tray/*Tests.cs` first compile and execute under `T03` once CMP-12 adds the five `<Compile Include>` links (both `.csproj` edits are consolidated in `T03` per the write-scope rule). `NotchWindow` has no linkable unit surface — its close/hide contract is proven headlessly by `NotchVisibilityControllerTests` and end-to-end by MAN-01. Every task runs `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` green as its in-task gate; `T03` runs all six focused suites by name plus the aggregate, each with `--minimum-expected-tests 1`, `$LASTEXITCODE` preserved, never `Out-Null`.
- Validation profile: Pass. Desktop .NET; MTP runner (`global.json` → `Microsoft.Testing.Platform`, SDK `10.0.400`); `xunit.v3.mtp-v2` `4.0.0` + `AwesomeAssertions` `9.6.0` + `NSubstitute` `6.2.0` in `tests/TokenHound.Infrastructure.Tests`. Seam tests use a hand-written `FakeTrayIcon` and an `Action<Action>` dispatcher spy — no live `NotifyIcon`, no message loop. E2E: omitted by desktop .NET policy; no browser/WebView/UI-automation scenario exists and no aggregate suite pulls E2E — the whole-project run is unit + integration only.
- Idempotency: Pass. New source/tests are created once by their owning task; `T01` also modifies its four owned production/test files once, `T03` modifies its three owned files once. Tray tests use hand fakes and Serilog test sinks with no shared static state; `NotchHiddenPollingTests` uses an isolated `UsageStore` with a fake provider. Re-running any suite is idempotent.

## Assumptions and open items

- Assumption (DEC-02 / workflow.md D-005 — settled at HIL 2, not an open item): `HudActionsViewModel`'s terminal member takes the full behaviour-preserving rename — `closeAction`→`shutdownAction`, `_closeAction`→`_shutdownAction`, `_closeTask`→`_shutdownTask`, `_isClosing`→`_isShuttingDown`, `IsClosing`→`IsShuttingDown`, `CloseAsync`→`ShutdownAsync` — plus the three matching test renames in `HudActionsViewModelTests.cs` (`T01`, CMP-08 / CMP-14). `TrayIconViewModel.Exit()` calls `hudActions.ShutdownAsync()` (`T02`); the `App` wrapper is `private Task ShutdownAsync()` whose body is `{ _visibility!.AllowClose(); return _lifetime!.ShutdownAsync(); }` (`T03`). The minimal-diff variant is rejected and removed from the open items.
- Open item (DEC-05 — test-project home): a dedicated `net10.0-windows` `TokenHound.App.Tests` with a `ProjectReference` is the better long-term home (the `<Compile Include>` link list is at 17 and growing) but is a separate slice (new `.csproj` + `TokenHound.slnx` write + STA/dispatcher plumbing). Not taken for v1. Owner: coordinator, post-v1.
- Assumption (A-01 / A-04, confirmed against source): one long-lived `NotchWindow` owned by `App`; `ShutdownMode.OnExplicitShutdown` + `MainWindow = _notchWindow` already keep the process alive behind a hidden Notch, so only the way back and the "Exit" bypass are new. `NotchWindow.xaml.cs` is 247 lines today; `T01`'s additions keep it below 300.
- Assumption (A-02): `logo.ico` already carries 16/32 px frames. If soft at 150–200 % scaling, `TaskbarIconAdapter` can switch from `IconSource` to the `Icon` property, or `logo.ico` is regenerated — no scope change (`T03` risk).
- Pending item (impact only — outside this write scope, hand to coordinator): `ARCHITECTURE.md` §3 folder tree cites a non-existent `App.xaml / Program.cs — Entry point, Single-Instance, and System Tray`; reconcile to `src/TokenHound.App/UI/Tray/` + `App.xaml.cs`. `docs/ROADMAP.md` PRD 11 status `[Planned]` → advance after HIL. Re-run `graft build` after implementation (deterministic, no key). No task in this plan edits these files.
- Pending item (300-line cap): `src/TokenHound.App/App.xaml.cs` is already 343 lines, above the AGENTS.md soft cap of 300; `T03`'s wiring adds roughly 20 lines. TechSpec CMP-10 places the wiring in `App.InitializeUi` in place. If review enforces the cap, the `T03` executor may extract a `src/TokenHound.App/UI/Tray/TrayComposition.cs` static factory (added to `T03`'s sole-writer set); the default remains the in-place CMP-10 wiring. Flag at HIL.
- Risk (low prob / low impact): `Hardcodet.NotifyIcon.Wpf 2.0.1` ships a `net8.0-windows7.0` asset consumed by `net10.0-windows`; if a .NET 10 runtime incompatibility surfaces, fall back to DEC-01's rejected `Shell_NotifyIcon` P/Invoke wrapper behind the same `ITrayIcon` seam — only `TaskbarIconAdapter` (`T03`) changes, all tests are library-agnostic.
- Risk (low / medium): the tray `.ico` frame looks soft at 150–200 % scaling (A-02). Mitigation: `TaskbarIconAdapter` switches from `IconSource` (URI) to the `Icon` property from the resource stream, or `logo.ico` is regenerated with explicit 16/20/24/32 frames — no scope change; verify once in MAN-01.
- Risk (low / low): idle memory rises above the ~30–45 MB target (NFR-05) — one hidden HWND + one icon, no timer added; verify once in MAN-01 with Task Manager.
- Required environment: .NET SDK 10.0.400 and restored MTP output for `T01`/`T02`/`T03` build and test; Windows 11 interactive desktop with `TokenHound.App` launched via the Windows MCP `App` tool (`mode="launch_executable"`), `Screenshot` on `display: [2]`, and existing provider configuration for MAN-01 (`T03`). Implementation proceeds on the `main` working tree (workflow.md D-006); no branch, no credential writes, no commit/push/PR without an explicit user request.

## State

- [x] T01 — completed (moved to `done/task_01.md`)
- [x] T02 — completed (moved to `done/task_02.md`)
- [x] T03 — completed (moved to `done/task_03.md`)

## Problems and solutions

- Structural notes carried from the HIL-2 restructure: (1) `T01` merges the former T01–T03 so the seam foundation and the two behaviour-preserving edits it must precede (`NotchWindow` "Close" → "Hide" + `Closing` intercept; `HudActionsViewModel` terminal-member rename) land under one writer; the executor follows the fixed internal order (seam DTOs + controller → `NotchWindow` relabel/intercept + repoint `OnCloseClick` off the terminal member → rename + the 3 test renames) so `src/TokenHound.App` builds at every step. (2) Because both `.csproj` edits live in `T03`, the `Tray/*Tests.cs` files authored in `T01`/`T02` first execute under `T03` once CMP-12's `<Compile Include>` links land — see the Coverage gate "Executability" line.
- T01 (done): Delivered the four WPF-free seam files under `src/TokenHound.App/UI/Tray/` (namespace `TokenHound.App.UI.Tray`, no `using` directives, purity grep clean), the `NotchWindow` "Close" → "Hide" relabel + `Window.Closing` intercept + `ShowNotch`/`HideNotch` (282 lines, non-activating hook/styles/`ApplyPlacement` intact), the full behaviour-preserving `HudActionsViewModel` rename (`closeAction`/`_closeAction`/`_closeTask`/`_isClosing`/`IsClosing`/`CloseAsync` → shutdown names; +25/−25, zero control-flow change; grep confirms no residual old symbols in any `.cs`), the two renamed tests in `HudActionsViewModelTests.cs`, and the two new headless suites `NotchVisibilityControllerTests` (8 facts, TC-03/05/09/10) + `TrayMenuModelTests` (4 facts, TC-02/04 T01 half). Evidence: `src/TokenHound.App` Release build exit 0; test-project build + `*HudActionsViewModelTests*` 9/9 green captured on a clean build before the Tray test files were added (independently re-verified: 9 passed / 0 failed / 0 skipped). Documented expected state: the test project does not `dotnet build` until T03 adds the CMP-12 `<Compile Include>` links — the only build errors are `using TokenHound.App.UI.Tray;` (CS0234) in the two new `Tray/*Tests.cs`; nothing else regressed.
- T01-ADR-01 (ADR candidate — hand to coordinator for promotion at QA): `TrayMenuEntry` / `TrayMenuDescriptor` are top-level records co-located in `TrayMenuModel.cs`, not nested inside `TrayMenuModel`. Reconciles the "nested" wording in task_01.md/CMP-05 with the authoritative TechSpec "Contracts and data" seam block, which declares both at namespace scope and references them unqualified everywhere (`ITrayIcon.Show(..., TrayMenuDescriptor menu)`; task_02.md T02.4 `new TrayMenuDescriptor { Entries = menu.BuildDescriptor(...) }`). `BuildDescriptor(bool)` returns `IReadOnlyList<TrayMenuEntry>`; T02/T03 consumers use the unqualified names unchanged. Full context/alternatives/consequences in `done/task_01.md` "### ADR candidates".
- T01 numbering deviation (accepted, no scope impact): the executor authored the two `Tray/*Tests.cs` files (T01.5) after Step C's rename instead of within Step A, so the in-task gate (`*HudActionsViewModelTests*` 9/9 on a compilable test project) could be captured at the one moment before the seam test files break the test-project build. All production-file work kept the mandated Step A → B → C order; consistent with task_01.md's stated "buildable at every step / one writer per file" principle and its note that the Tray tests "first compile and execute under T03".
- T02 (done): Delivered `TrayIconViewModel.cs` (167 lines) and `TrayIconHost.cs` (143 lines) under `src/TokenHound.App/UI/Tray/`, plus their headless test suites `TrayIconViewModelTests.cs` (243 lines, TC-04/06/07/08/14) and `TrayIconHostTests.cs` (291 lines, TC-01/12/13/14) under `tests/TokenHound.Infrastructure.Tests/Tray/`. Seam purity verified: zero references to `System.Windows` or `Hardcodet`. All style/complexity rules satisfied (all files <= 300 lines, methods <= 30 lines, nesting <= 3 levels, XML docs, file-scoped namespaces, Serilog structured logging with `nameof` tokens). Evidence: `src/TokenHound.App` Release build exit 0 (0 errors, 0 warnings). Expected documented state: Tray test classes first compile and execute under T03 once CMP-12 adds the `<Compile Include>` links to `TokenHound.Infrastructure.Tests.csproj`.
- T03 (done): Added `Hardcodet.NotifyIcon.Wpf 2.0.1` package reference to `TokenHound.App.csproj`; implemented `TaskbarIconAdapter.cs` (138 lines) in `src/TokenHound.App/UI/Tray/`; wired tray lifecycle and `ShutdownAsync()` wrapper in `App.xaml.cs` (kept modular with `InitializeTray`); added `<Compile Include>` seam links in `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`; implemented `NotchHiddenPollingTests.cs` (98 lines, TC-11); verified all 6 focused test suites and full aggregate suite (606 tests passed, 0 failed, 0 warnings). MAN-01 handed to coordinator.
- T03-ADR-01 (ADR candidate): Link `ITrayIcon.cs` into `TokenHound.Infrastructure.Tests.csproj`. `ITrayIcon.cs` contains zero WPF types (`Uri`, `string`, `TrayMenuDescriptor`, `EventHandler`, `TrayMenuItemKey`, `IDisposable`). Linking it allows `TrayIconHost.cs` and `TrayIconHostTests.cs` (using `FakeTrayIcon : ITrayIcon`) to compile under the headless `net10.0` test project without referencing `TokenHound.App.csproj`.
- T03-ADR-02 (ADR candidate): Headless `pack://` URI scheme registration via `[ModuleInitializer]` in test assembly. Added a `[ModuleInitializer]` in `NotchHiddenPollingTests.cs` registering the `pack` scheme via `UriParser.Register` so headless tests evaluate pack URIs without loading WPF.
