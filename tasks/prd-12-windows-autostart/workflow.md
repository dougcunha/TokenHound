# Workflow Log: PRD 12 — Start with Windows setting

## Feature Context
- **Slug**: `prd-12-windows-autostart`
- **Objective**: Add a Settings option to turn start-at-logon on and off. It must be compatible with the Inno Setup `startupicon` task (`{userstartup}\TokenHound.lnk`) in installed copies.
- **Process Level**: `sdd-lean` (decided at HIL 0).
- **JEV Mode**: `active`.
- **Git Base Commit**: `c7440308a6a172d2c5923f03283236e2f97ce3d3`. The worktree was clean before this flow, apart from the `tasks/triage-log.jsonl` line that triage appended.

## Decisions

### DEC-01: Process Level and Stops Adjustment (sdd-lean)
- **Date**: 2026-09-29
- **Decision**: The human chose `sdd-lean` at HIL 0, with jev in `active`.
- **Triage**: The rubric recommended `sdd-full` because three signals are present. S1: the installer shortcut contract, with no `UsePreviousTasks` or `[UninstallDelete]`. S5: the startup mechanism, the source of truth, portable behavior, installer updates, and uninstall cleanup were all undecided. S8: the feature writes OS startup state outside the app folder. jev_decide gave `sdd-lean` 0.97 and `sdd-full` 0.03. The human chose `sdd-lean`. Record: `tasks/triage-log.jsonl`, 2026-09-29.
- **Stops Modification**: HIL 1 and HIL 2 are merged into one decision, taken after the PRD, TechSpec, and task plan are drafted. The independent code review of step 5 is kept.

### DEC-02: Merged HIL 1+2 approved
- **Date**: 2026-09-29
- **Decision**: The human approved `prd.md`, `techspec.md`, and `tasks.md` (T01, T02) as presented. This includes the PRD product decisions: the OS is the source of truth, the Startup-folder `.lnk` is the only mechanism, the StartupApproved flag is honored and cleared by deletion, and the installer changes are in scope (silent keeps the state, interactive pre-selects and removes, uninstall cleans up), with a General tab. The human also authorized implementation and corrections within that contract.
- **Human text**: "Aprovar" (HIL 1+2) and "Continuar aqui (Recommended)" (session).
- **Scope**: sha256 prd.md=604499210b05, techspec.md=46d7d74314e9, tasks.md=a8894a2fd967.

### DEC-03: Reservations decided and feature accepted (HIL 3)
- **Date**: 2026-09-29
- **Decision**: At the reservations HIL the human chose to correct codereview_2/OI-02 directly, without another review round, and to finalize with acceptance. OI-01 (`ShellLink` and `ResolveStartupFolder` are public) and OI-03 (`App.xaml.cs` 465 lines, pre-existing) are accepted as open items.
- **Human text**: "Corrija diretamente a 02 e finalize com aceite". Session: "Continuar aqui (Recommended)".
- **Scope**: the OI-02 change is test-only. `StartupLaunchServiceTests` uses a named `STALL_GUARD_MS = 2000` coarse guard instead of `< 200` ms, and NFR-03's 200 ms budget stays verified by MA-1. Limitation: this change was not independently reviewed, by the human's decision.

## Events

- 2026-09-29: jev probe ok (jev_noul, provider openrouter).
- 2026-09-29: `prd.md` written (lean: OBJ-01..02, US-01..03, FR-01..09, NFR-01..04). It proposes product decisions for the merged HIL: the OS is the source of truth (shortcut presence plus the StartupApproved flag), the `.lnk` in the Startup folder is the only mechanism, installer changes are in scope (silent installs keep the current state, interactive installs pre-select from it, uninstall cleans up), and a new "General" tab hosts the option. No external content was fetched, so J0 was not triggered.
- 2026-09-29: `techspec.md` written (DEC-01..12, CMP-01..12, TC-01..11, MA-1..4). Preparatory refactoring not recommended: `SettingsWindow.xaml` is above 500 lines, but the feature adds only one tab there. J1 (active): 20/20 verified, with CON-2 flagged `review` (0.49). Fixed by adding DEC-12 (mode parity) and an installed-copy check to MA-3. PRD NFR-01 wording tightened (tests use a fake Startup apps store, not HKCU).
- 2026-09-29: `tasks.md` + `task_01.md`, `task_02.md` written (DAG T01 → T02). J2 (active): 35/36 verified, with NFR-02 `review` (0.57), NFR-03 `unsupported` (0.52), and DEC-11 `review` (0.65). Fixed by adding acceptance criteria to T01 and T02. Merged HIL 1+2 is next.
- 2026-09-29: Merged HIL 1+2 approved (DEC-02). Continuing in this session with T01.
- 2026-09-29: T01 done: Core `StartupApprovalPolicy`, Infrastructure/Startup service, TC-01..TC-06, build 0 warnings, Core 166 and Infrastructure 944 passed. J3 (active): 6/9 claims `auto`. C9 was fixed by rerunning; C6 and C8 were justified in the Handoff. The rubric-only escalate is recorded as an unspecific signal. The approved-source hashes were refreshed after a CRLF → LF normalization of techspec.md and tasks.md, plus the T01 state and link update in tasks.md; the content is otherwise the same as approved in DEC-02.
- 2026-09-29: T02 done: StartupSettingsViewModel, General tab, App wiring, installer `[Code]`/`Check`/`[UninstallDelete]`, TC-07..TC-09, App build 0 warnings, Infrastructure 950 and Core 166 passed, and ISCC compile clean. Local deviation within DEC-07: the Cadence tab was hard-coded at index 1 (`SelectedIndex == 1`) in the code-behind and 4 XAML triggers. These now use the named `CadenceTab.IsSelected`, which adds `SettingsWindow.xaml.cs` to the affected files. J3 (active): 5/8 claims `auto`. C4, C5, and C6 were justified on their code parts, and their manual parts stay pending. Next: the human visual check (MA-1..MA-4), then the delegated review. The Windows MCP server failed to connect this session, so the human launches the app.
- 2026-09-29: Visual check approved by the human: MA-1..MA-4 all passed. Human text: "Tudo OK". Session: "Continuar aqui (Recommended)". Next: delegated review codereview_1.
- 2026-09-29: During the flow, the human committed `b9bffe6` ("feat(sdd): reduce the jev pilot to per-criterion J3") in another session. It touches only `.agents/skills/**` (the 11 files noted in O-02). All feature changes remain uncommitted on top of it, so the delegated review uses `--base b9bffe6` to exclude the skill edits. The flow's `git_base` c7440308 is kept for provenance. From this point the jev points follow the new `sdd-jev` (J3 only); the delegated reviewer does not call jev.
- 2026-09-29: Delegated review codereview_1 received. The worktree was unchanged, so the review is not contaminated. Status REJECTED. CR-01 (Medium): `CreateDefault` returns an empty path when the Startup folder is missing, which crashes Settings. CR-02, CR-03, and CR-04 are Low. OI-01..03 are informational and not planned. Correction round 1 planned in codereview_1: T03 (CR-01), T04 (CR-02), T05 (CR-03), T06 (CR-04), all within the DEC-02 contract, so they run automatically.
- 2026-09-29: Correction round 1 done: T03 (CR-01, `DoNotVerify` plus a test), T04 (CR-02, `{Operation}` log), T05 (CR-03, 4-argument calls split), T06 (CR-04, manifest and handoff record the visual check). App build 0 warnings. Core 166/166 and Infrastructure 951/951 passed. J3 (active): T03, T04, and T06 were verified at `auto`; for T05, criterion 2 came back `review` (0.77) and was justified by the full runs. Next: delegated re-review codereview_2.
- 2026-09-29: Delegated re-review codereview_2 received. The worktree was unchanged, so the review is not contaminated. Status APPROVED WITH RESERVATIONS. codereview_1/CR-01..CR-04 resolved. Optional reservations: OI-01 (`ShellLink` and `ResolveStartupFolder` are public while CMP-02 says internal), OI-02 (the < 200 ms timing assertion in TC-03 may flake on CI), OI-03 (App.xaml.cs 461 → 465 lines, baseline). No escalation trigger. The approved-source hash for tasks.md changed to 66fe30d9773c through state and link updates only (T01/T02 done, visual check recorded); its contract content is the one approved in DEC-02. Next: reservations HIL.
- 2026-09-29: OI-02 corrected (test-only): Infrastructure.Tests build 0 warnings, 951/951 passed. `jev-summary.md` written (first-review J3: CR-01 was a miss; 6 false alarms over 2 tasks; the feature does not count toward the stop criterion because the jev skill format changed mid-flow). Feature accepted and closed (DEC-03). No commit was requested.
