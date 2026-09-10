# Workflow — System Tray NotifyIcon (PRD 11)

Coordinator log for the SDD orchestrated flow. Holds human decisions, authorizations,
and recovery events with stable IDs. Execution authority stays in `prd.md`,
`techspec.md`, `tasks.md`, `task_*.md`, handoffs, and `codereview_*/`.

## Feature identity

- Slug: `system-tray-notifyicon`
- Feature folder: `tasks/prd-system-tray-notifyicon/`
- Roadmap origin: `docs/ROADMAP.md` — Phase 3, PRD 11 "System Tray Integration".
- Coordinator session: https://claude.ai/code/session_013SLXozoKyTdhGmCsQ3p9Gp

## Git base

- Base commit: `565a9c98d435c1f410b5edd8c2aad151d1bd9091` (branch `main`).
- Working tree at flow start: clean (`git status --porcelain` empty).
- Worktree isolation: none. Single App-layer feature, not a parallel provider swarm;
  work proceeds in the primary worktree on `main`.

## Environment & constraints

- .NET desktop policy: omit E2E; keep unit + integration + manual acceptance.
  Aggregate test commands must also exclude E2E.
- Repo invariants (`AGENTS.md`): `TokenHound.Core` stays pure (no UI/OS); one class per
  file; files/classes <= 300 lines, methods <= 30 lines, nesting <= 3; XML docs on public
  members; file-scoped namespaces; JSON via `System.Text.Json`; `.ConfigureAwait(false)`
  in Core/Infrastructure, omit in UI sync contexts.
- Test runner: MTP via `rtk dotnet test --project <path> --no-build --no-restore -- --minimum-expected-tests 1`
  (see `dotnet-efficient-validation` skill).
- Stage skills present under `.claude/skills/`: sdd-create-prd, sdd-create-techspec,
  sdd-plan-tasks, sdd-orchestrate-tasks, sdd-execute-task, sdd-review-code,
  sdd-plan-corrections, sdd-execute-corrections. `sdd-create-prd` has
  `disable-model-invocation: true` — delegate by explicit absolute path.

## Decisions

### D-001 — Feature selected by cost/benefit (2026-09-10)

- Scope: which roadmap feature to implement next.
- Human text: "De uma olhada no ROADMAP e docs/specs e elenque uma feature para ser
  implementada por custo vs beneficio." → then "vamos implementar o TrayIcon então".
- Outcome: PRD 11 (System Tray NotifyIcon) chosen over PRD 12 (Bézier/Mica) and
  PRD 13 (multi-monitor docking). Rationale: closes a real functional gap
  (`ShutdownMode.OnExplicitShutdown` is already set but there is no tray, so hiding /
  closing the Notch is a dead end with no way back), App-layer only, keeps Core pure,
  reuses `HudActionsViewModel`, fits SDD microtask sizing, low risk.
- Provenance: coordinator cost/benefit analysis in session transcript, user confirmation.

### D-002 — Authorization to run the orchestrated flow (2026-09-10)

- Scope: run `sdd-orchestrate-flow` for this feature.
- Human text: "/sdd-orchestrate-flow ... vamos implementar o TrayIcon então".
- Outcome: authorized to produce PRD, TechSpec, and task plan and to present HIL gates.
  HIL 1 (PRD), HIL 2 (TechSpec + DAG + tasks), and HIL 3 (acceptance) still required.
  No commit/push/PR without explicit request.

## Stage log

### S-01 — Product / PRD drafted (2026-09-10)

- Delegated `sdd-create-prd` (subagent `general-purpose`, exclusive scope `prd.md`).
- Artifact: `tasks/prd-system-tray-notifyicon/prd.md` written, self-consistent, house style.
- Content: OBJ-01..06, US-01..08, FR-01..12, NFR-01..11. Manual acceptance script (11 steps)
  for Windows 11 interactive desktop via Windows MCP `App` tool. No E2E.
- Open decision deferred to TechSpec (PRD A-03): repurpose `HudActionsViewModel`
  (rename Close→Hide semantics + add Exit) vs add a thin composing tray view-model.
- Bundled behavior change: Notch context-menu "Close" → "Hide" (FR-09/FR-10) — no
  Notch-close path shuts the app; only tray "Exit" runs `ShutdownAsync`.
- Out of scope (v1): timed "Hide for 1 hour"/auto-hide, single-instance guard
  (recommended next slice, judged non-blocking by PRD author), icon tinting/badge,
  balloon/toast, rich tooltip, localisation, any Core/Infrastructure change.
- Pending files flagged for later change (not touched by PRD): `HudActionsViewModel.cs`
  + its tests, `NotchWindow.xaml`/`.xaml.cs`, `App.xaml.cs` `InitializeUi`,
  `TokenHound.App.csproj` (iff Hardcodet), `ARCHITECTURE.md` folder tree,
  `docs/ROADMAP.md` PRD 11 status, `graft/` rebuild.
- Status: awaiting HIL 1.

### D-003 — HIL 1: PRD approved (2026-09-10)

- Gate: HIL 1 (product).
- Human choice: "Aprovar e seguir".
- Meaning: `prd.md` approved as written, including (a) the Notch context-menu
  "Close" -> "Hide" rebrand (FR-09/FR-10) and (b) single-instance guard left out of
  v1 as a recommended follow-up slice. Authorized to proceed to TechSpec + task plan.
- Approved artifact: `prd.md`
  sha256 `a4d491a7c796b52e1438e74eb6113198099bd3ec1b18fd0c36c11afe0c3203f7`.
- Provenance: AskUserQuestion answer in session transcript.

### S-02 — TechSpec drafted (2026-09-10)

- Delegated `sdd-create-techspec` (subagent `general-purpose`, exclusive scope `techspec.md`).
- Artifact: `tasks/prd-system-tray-notifyicon/techspec.md`. DEC-01..08, CMP-01..14, TC-01..16.
- Coverage verified by coordinator: every PRD OBJ/US/FR/NFR maps to DEC + CMP + TC
  ("Requirement coverage" subsection). Validation profile records
  `E2E: omitted by desktop .NET policy`. MTP commands project-scoped, `--minimum-expected-tests 1`,
  `$LASTEXITCODE` preserved. No gaps.
- Key rulings:
  - DEC-01: adopt `Hardcodet.NotifyIcon.Wpf` `2.0.1` (latest stable, MIT, zero transitive
    deps, `net8.0-windows7.0` asset consumed by `net10.0-windows`). Rejected raw
    `Shell_NotifyIcon` P/Invoke. `TaskbarIcon` confined to `TaskbarIconAdapter`.
  - DEC-02: keep `HudActionsViewModel` behaviour; **rename** terminal member
    `CloseAsync`->`ShutdownAsync`, `IsClosing`->`IsShuttingDown` (Exit only). Add separate
    non-latching `NotchVisibilityController` for hide/show + composing `TrayIconViewModel`.
    (Minimal-diff alternative — keep names, re-point delegate — left open for HIL 2.)
  - DEC-03: `NotchWindow.Closing` intercept -> `e.Cancel` + hide; "Exit" bypasses via
    `NotchVisibilityController.AllowClose()`. `Window.Hide()` keeps HWND so
    `WS_EX_NOACTIVATE` + `WM_MOUSEACTIVATE` hook survive.
  - DEC-04: new code under `src/TokenHound.App/UI/Tray/`; wire in `App.InitializeUi`;
    `TrayIconHost` added to existing `disposableResources` (no `ApplicationLifetime` change).
  - DEC-05: tests via `<Compile Include>` links into `tests/TokenHound.Infrastructure.Tests`
    (existing pattern); NO `TokenHound.App.Tests`, NO `TokenHound.slnx` edit for v1.
- Recommended DAG from TechSpec: A(seam) -> {B(VM rename), C(tray VM), E(Notch window)} ->
  D(host+adapter) -> F(composition+csproj wiring). Both `.csproj` edits assigned solely to F.
- Impacts for the coordinator (outside task write scope): reconcile `ARCHITECTURE.md`
  folder tree (cites non-existent `Program.cs` for tray/single-instance); advance
  `docs/ROADMAP.md` PRD 11 `[Planned]`; rerun `graft build` post-merge.
- Status: task planning delegated next; then HIL 2.

### S-03 — Task plan drafted (2026-09-10)

- Delegated `sdd-plan-tasks` (subagent `general-purpose`, exclusive scope `tasks.md` + `task_*.md`).
- Artifacts: `tasks.md` (sha256 `27b0dce44f3d041205a815c662bf79d762841f37e429c7c7d10cd0243f3dca5d`)
  + `task_01.md`..`task_06.md`.
- Coordinator validation: coverage (every DEC/CMP/TC/FR/NFR/OBJ -> >=1 task, no orphan),
  DAG acyclic, one writer per file (both `.csproj` + `App.xaml.cs` sole-owned by T06),
  MTP commands with `--minimum-expected-tests 1` + `$LASTEXITCODE` preserved, E2E omitted
  and recorded. All pass. One accepted property: T01/T04/T05 author `Tray/*Tests.cs` that
  first execute under T06 once T06 adds the `<Compile Include>` links (intermediate task
  gate = green `src/TokenHound.App` build; T06 runs every suite by name).
- Tasks: T01 seam+menu+visibility controller (root) / T02 Notch close->hide intercept (root)
  / T03 `HudActionsViewModel` rename (dep T02) / T04 `TrayIconViewModel` (dep T01,T03)
  / T05 `TrayIconHost` + graceful degradation (dep T01,T04) / T06 package + `TaskbarIconAdapter`
  + `App` wiring + `.csproj` links + integration test + MAN-01 (dep T01..T05).
- Deviations from recommended shape, justified by planner: T03 depends on T02 (T02 removes
  the last non-test caller of `CloseAsync` so the rename builds clean under one writer);
  `TaskbarIconAdapter` moved from the host task to T06 (co-located with its package ref;
  no unit tests by design, covered by MAN-01).
- Open for HIL 2: DEC-02 rename (default) vs minimal-diff (keep `CloseAsync`/`IsClosing`,
  re-point delegate only) — planner mapped exactly which steps/tests each variant changes.
- Risk flagged: `App.xaml.cs` already 343 lines (> 300 soft cap); T06 adds ~20. Default
  in-place wiring; T06 executor may extract `UI/Tray/TrayComposition.cs` if review enforces
  the cap.
- Status: awaiting HIL 2.

### D-004 — HIL 2: approve design, restructure plan to 3 tasks (2026-09-10)

- Gate: HIL 2 (technical).
- Human choice: "Faça em 3 tasks" (custom answer).
- Meaning: TechSpec design direction approved; the 6-task decomposition is NOT approved as
  written. Re-plan into exactly 3 tasks, preserving all DEC/CMP/TC coverage, acyclic DAG,
  one-writer-per-file, and vertical slices. Implementation authorization applies once the
  3-task plan is confirmed.
- Provenance: AskUserQuestion answer in session transcript.

### D-005 — HIL 2: DEC-02 = full rename (2026-09-10)

- Human choice: "Rename completo (recomendado)".
- Meaning: `HudActionsViewModel` terminal member renamed to shutdown semantics
  (`closeAction`->`shutdownAction`, `CloseAsync`->`ShutdownAsync`, `_closeTask`->`_shutdownTask`,
  `_isClosing`->`_isShuttingDown`, `IsClosing`->`IsShuttingDown`), behaviour preserved; wired
  only to tray "Exit". The minimal-diff alternative is rejected. `HudActionsViewModelTests`
  renamed accordingly.
- Provenance: AskUserQuestion answer in session transcript.

### D-006 — HIL 2: implement on main working tree (2026-09-10)

- Human choice: "Direto na main".
- Meaning: no feature branch; changes applied to the `main` working tree. No commit / push /
  PR until the user explicitly asks. Git base remains `565a9c9`.
- Provenance: AskUserQuestion answer in session transcript.

### S-04 — Plan restructured to 3 tasks (2026-09-10)

- Delegated `sdd-plan-tasks --update` (subagent `general-purpose`). Rewrote `tasks.md`,
  wrote `task_01..03.md`, deleted `task_04..06.md`.
- Hashes: `tasks.md` `771dc54b262eb28c3db95b75f813073dc799586d4bba72c17fa26ce28adb6e0b`;
  `task_01.md` `05e1b0a8556cf4d1963de5640686d06ccf2d758c23982feff65eaf940417be21`;
  `task_02.md` `92770a16858e107d2b6914a5679c0be1f38a6825b4824e24d2c219a98c66b798`;
  `task_03.md` `ac9e9edb9644db2649aec079b2591592a29535af18ea05806e1fca81a296266a`.
- 3-task DAG (linear): **T01** seam + `NotchWindow` close->hide + `HudActionsViewModel`
  full rename (10 files) -> **T02** `TrayIconViewModel` + `TrayIconHost` (4 files) ->
  **T03** package + `TaskbarIconAdapter` + `App` wiring + `.csproj` links + integration
  test + MAN-01 (5 files).
- Coordinator validation: coverage identical to the 6-task plan (67 traceability rows,
  no drop), acyclic, one writer per file, both `.csproj` + `App.xaml.cs` sole-owned by T03,
  DEC-02 full rename baked in, minimal-diff removed as settled. All coverage-gate lines pass
  (Executability conditional — same accepted "author now / execute at T03" property).
- Note: SDD artifact files under `tasks/prd-system-tray-notifyicon/` are staged in the git
  index (`git add`, by a subagent or hook) but uncommitted; no source file changed;
  HEAD still `565a9c9`.
- Status: proceeding to implementation on `main` (D-006). Delegating `sdd-orchestrate-tasks`
  batch by batch (T01, then T02, then T03) with a coordinator checkpoint + pause offer
  between tasks.

### S-05 — Batch {T01} executed, reviewed, approved (2026-09-10)

- Delegated `sdd-orchestrate-tasks` batch {T01} (subagent `general-purpose`); it delegated
  `sdd-execute-task` and reviewed the diff independently. Returned `batch-completed`.
- T01 moved to `tasks/prd-system-tray-notifyicon/done/task_01.md`; `tasks.md` State
  `[x] T01`, T02 eligible, T03 blocked on T02.
- Coordinator independent spot-check: `src/TokenHound.App` Release build clean (0/0);
  `*HudActionsViewModelTests*` 9/9 pass; old-name grep
  (`CloseAsync|IsClosing|_closeTask|_closeAction|_isClosing|closeAction`) = 0 hits across
  282 files; `NotchWindow.xaml.cs` 282 lines, `HudActionsViewModel.cs` 247; seam files
  one-class-per-file, BCL-only; `NotchVisibilityController` + `NotchWindow.Closing` intercept
  read correct (e.Cancel + hide; ShowNotch has no Activate()).
- Expected non-blocking state: `tests/TokenHound.Infrastructure.Tests` does not build until
  T03 adds the CMP-12 `<Compile Include>` links; the ONLY break is 2 `using
  TokenHound.App.UI.Tray;` in the new `Tray/*Tests.cs`. `NotchVisibilityControllerTests` /
  `TrayMenuModelTests` first execute under T03.
- ADR candidate **T01-ADR-01**: `TrayMenuEntry` / `TrayMenuDescriptor` declared as top-level
  records co-located in `TrayMenuModel.cs` (not nested) to match the authoritative TechSpec
  "Contracts and data" seam block, which uses them unqualified everywhere. Non-blocking;
  carry to HIL 3 / global review.
- Minor nit for global review: `NotchWindow.ShouldInterceptClose` / `CloseIntercepted` are
  public fields (delegate-injection per TechSpec contract), not properties. Left for
  `sdd-review-code`.
- Working tree (D-006, no commit): `M` on `NotchWindow.xaml(.cs)`, `HudActionsViewModel.cs`,
  `HudActionsViewModelTests.cs`; new `src/TokenHound.App/UI/Tray/` (4) and
  `tests/TokenHound.Infrastructure.Tests/Tray/` (2); `done/task_01.md`. HEAD still `565a9c9`.
- Status: T01 complete; batch {T02} eligible.

### D-007 — Pause after T01 (2026-09-10)

- Between-batch choice: "Pausar aqui".
- Meaning: stop after T01; do not start T02 in this session. No gate approval implied —
  implementation authorization (D-004) still stands; T02/T03 remain pending and eligible
  in dependency order. No commit performed (D-006).
- Resume with: `Use $sdd-orchestrate-flow to resume feature system-tray-notifyicon in this repository.`
- Next batch on resume: {T02} — `TrayIconViewModel` + `TrayIconHost` + their tests
  (`src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`, `TrayIconHost.cs`;
  `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs`, `TrayIconHostTests.cs`).
- Provenance: AskUserQuestion answer in session transcript.

### D-008 — Continue in this session with batch {T03} (2026-09-10)

- Between-batch choice: "Continue in this session with batch {T03}".
- Meaning: proceed immediately to delegating task T03 in the same session. Implementation
  authorization (D-004) still stands; T03 is eligible.
- Provenance: AskUserQuestion answer in session transcript.

### S-06 — Batch {T02} executed, reviewed, approved (2026-09-10)

- Resumed session `891fdde5-551b-45bf-944f-b9d6d844f6e3` from checkpoint generation 15.
- Delegated `sdd-execute-task` for batch {T02} to subagent `e7fae1c5-cf2e-4cec-aa09-53b1c371fcdb`
  (exclusive write scope: `TrayIconViewModel.cs`, `TrayIconHost.cs`, and their 2 test files).
- Returned handoff with all acceptance items covered.
- Coordinator independent review:
  - `src/TokenHound.App` Release build clean: 3 projects, 0 errors, 0 warnings (exit code 0).
  - Seam purity: zero references to `System.Windows` or `Hardcodet` in `TrayIconViewModel.cs` or `TrayIconHost.cs`.
  - Invariants: all classes sealed, files <= 300 lines (`TrayIconViewModel.cs` 167 lines, `TrayIconHost.cs` 143 lines, `TrayIconViewModelTests.cs` 243 lines, `TrayIconHostTests.cs` 291 lines), methods <= 30 lines, nesting <= 3 levels, XML docs on public members, file-scoped namespaces, structured logging with `nameof` tokens.
  - Test suites: `TrayIconViewModelTests` covers TC-04, TC-06, TC-07, TC-08, TC-14; `TrayIconHostTests` covers TC-01, TC-12, TC-13, TC-14. Tests will compile and run once T03 adds the CMP-12 `<Compile Include>` links.
- T02 moved to `tasks/prd-system-tray-notifyicon/done/task_02.md`; `tasks.md` updated (`[x] T02`, T03 unblocked and eligible).
- Working tree (D-006, no commit): `M` on `NotchWindow.xaml(.cs)`, `HudActionsViewModel.cs`, `HudActionsViewModelTests.cs`; new `src/TokenHound.App/UI/Tray/` (6 files: `ITrayIcon.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `NotchVisibilityController.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`), new `tests/.../Tray/` (4 files: `NotchVisibilityControllerTests.cs`, `TrayMenuModelTests.cs`, `TrayIconViewModelTests.cs`, `TrayIconHostTests.cs`); `done/task_01.md`, `done/task_02.md`. HEAD still `565a9c9`.
### S-07 — Batch {T03} executed, reviewed, approved (2026-09-10)

- Delegated `sdd-execute-task` for batch {T03} to subagent `59d77fa8-6448-406c-ab8c-f36c67481eff`
  (exclusive write scope: `TokenHound.App.csproj`, `App.xaml.cs`, `TokenHound.Infrastructure.Tests.csproj`,
  `TaskbarIconAdapter.cs`, `NotchHiddenPollingTests.cs`, and `task_03.md`).
- Returned handoff with all acceptance items covered.
- Coordinator independent review:
  - `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests` Release builds clean (0 errors, 0 warnings).
  - Test suites: all 6 focused suites passed:
    - `NotchVisibilityControllerTests`: 8 passed
    - `TrayMenuModelTests`: 4 passed
    - `TrayIconViewModelTests`: 6 passed
    - `TrayIconHostTests`: 5 passed
    - `HudActionsViewModelTests`: 9 passed
    - `NotchHiddenPollingTests`: 2 passed
    - Aggregate test run: 606 passed, 0 failed, 0 warnings (exit code 0).
  - Invariants: `TaskbarIconAdapter.cs` (138 lines) is the sole consumer of `Hardcodet.NotifyIcon.Wpf`.
    Methods <= 30 lines, nesting <= 3 levels. `App.xaml.cs` wired with `InitializeTray` helper and
    `ShutdownAsync()` wrapper. Seam links compiled cleanly in `TokenHound.Infrastructure.Tests.csproj`
    without WPF dependencies.
  - ADR candidates recorded: `T03-ADR-01` (link `ITrayIcon.cs` into test project) and `T03-ADR-02` (headless
    `pack://` URI registration via `[ModuleInitializer]`).
- T03 moved to `tasks/prd-system-tray-notifyicon/done/task_03.md`; `tasks.md` updated (`[x] T03`, all DAG tasks completed).
- Working tree (D-006, no commit): `M` on `App.xaml.cs`, `TokenHound.App.csproj`, `NotchWindow.xaml(.cs)`,
  `HudActionsViewModel.cs`, `TokenHound.Infrastructure.Tests.csproj`, `HudActionsViewModelTests.cs`; new
  `src/TokenHound.App/UI/Tray/` (7 files: `ITrayIcon.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`,
  `NotchVisibilityController.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`, `TaskbarIconAdapter.cs`), new
  `tests/.../Tray/` (4 files: `NotchVisibilityControllerTests.cs`, `TrayMenuModelTests.cs`, `TrayIconViewModelTests.cs`,
  `TrayIconHostTests.cs`), new `tests/.../Engine/NotchHiddenPollingTests.cs`; `done/task_01.md`, `done/task_02.md`,
  `done/task_03.md`. HEAD still `565a9c9`.
- Status: All tasks complete. Proceeding to Step 5: Global Code Review (`sdd-review-code`).

### S-08 — Global code review completed (2026-09-10)

- Delegated `sdd-review-code` to independent reviewer subagent `fa20b678-a46b-44c5-b855-dd01d56044d3`.
- Artifact: `tasks/prd-system-tray-notifyicon/codereview_1/codereview.md`.
- Literal status: `APPROVED WITH RESERVATIONS`.
- Matrix: 100% of obligations (OBJ-01..06, FR-01..12, NFR-01..11, DEC-01..08, CMP-01..14, TC-01..16)
  conforming. All 6 focused test suites (34 tests) and full aggregate (606 tests) green, 0 errors, 0 warnings.
- Findings:
  - CR-01 (Low): `App.xaml.cs` (387 lines) exceeds 300-line soft cap.
  - CR-02 (Low): `NotchWindow.ShouldInterceptClose` / `CloseIntercepted` are public fields, not auto-properties.
  - CR-03 (Low): `TrayIconHost.Initialize()` spans 37 lines (method limit <= 30).
- ADR candidates evaluated: `T01-ADR-01`, `T03-ADR-01`, `T03-ADR-02` assessed as sound and recommended.
- Status: opening Reservations HIL to decide whether to correct findings or finalize to acceptance (Step 6).

### D-009 — Reservations HIL: finalize without code changes (2026-09-10)

- Scope: code review reservations decision.
- Human choice: "Finalize without code changes: accept the 3 Low findings as open items and proceed to Step 6 Acceptance (MAN-01)".
- Meaning: CR-01 (App.xaml.cs 387 lines), CR-02 (NotchWindow public fields -> properties), and CR-03 (TrayIconHost.Initialize 37 lines) accepted as non-blocking open items; proceed directly to Step 6 Acceptance.
- Provenance: AskUserQuestion answer in session transcript.

### S-09 — Acceptance preparation & MAN-01 validation (2026-09-10)

- Reconciled `ARCHITECTURE.md` §3: added `src/TokenHound.App/UI/Tray/` folder tree, updated `App.xaml / App.xaml.cs`.
- Advanced `docs/ROADMAP.md` PRD 11 status from `[Planned]` to `[Complete]`.
- Executed `graft build`: 3017 nodes, 1830 edges, 288 cards indexed.
- Executed desktop HUD & tray validation (MAN-01):
  - Launched `TokenHound.App.exe` (PID 22264) via Windows MCP `App` tool (`launch_executable`).
  - Captured desktop screenshot on primary monitor `display: [2]` (3440x1440). Verified HUD Notch rendered at top right (persisted coordinates 2663, 0) and TokenHound icon present in Windows system tray notification area.
  - Inspected runtime log `TokenHound_20260910.logc`: confirmed `HUD window displayed successfully.` and `Tray icon Initialize with tooltip TokenHound`.
  - Terminated test process cleanly.
- Status: ready for HIL 3 (final acceptance).

### D-010 — HIL 3: Delivery accepted (2026-09-10)

- Gate: HIL 3 (acceptance).
- Human choice: "Approve delivery and complete feature system-tray-notifyicon".
- Meaning: human acceptance recorded for feature `system-tray-notifyicon`. Delivery approved as complete.
- Provenance: AskUserQuestion answer in session transcript.

### S-10 — Feature flow completed (2026-09-10)

- All tasks in DAG completed: T01, T02, T03 in `done/`.
- Full aggregate verification passing: 606 tests passed, 0 warnings, 0 failed.
- Code review approved with reservations (`codereview_1/codereview.md`), with 3 low-severity findings
  (CR-01, CR-02, CR-03) recorded as accepted open items per D-009.
- Live Windows 11 desktop verification (MAN-01) confirmed Notch HUD and system tray notification icon.
- `ARCHITECTURE.md` and `docs/ROADMAP.md` reconciled; `graft build` refreshed.
- ADR candidates assessed as sound: `T01-ADR-01`, `T03-ADR-01`, `T03-ADR-02`.
- Checkpoint marked `phase: completed`, `status: completed`, `next_action.kind: close`.
- Working tree remains on `main` (D-006); no commit/push/PR without explicit user instruction.

## Recovery events

- (none)





