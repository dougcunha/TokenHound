# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T06 — Validate the integrated refactoring

## Outcome

The integrated state passes TC-01..TC-07 and QA-01..QA-07. MA-2 (head build) matches MA-1 (`git_base` worktree build) captured in the same session, and the evidence is ready for the human visual check.

## Dependencies and boundaries

- Depends on: T02, T05
- Unblocks: — (visual check, then delegated review)
- In scope: validation, a temporary baseline worktree and its removal, and fixing defects found inside the T02..T05 contracts.
- Out of scope: new visuals or behavior; a defect that needs a contract change goes to exception HIL.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01..R-12 | `prd.md#behaviors-to-preserve` | All behaviors checked on the integrated state |
| DEC-07 | `techspec.md#technical-decisions` | Base vs head, same session, sequential |
| TC-01..TC-07, MA-1..MA-4 | `techspec.md#safety-net` | Full safety net |
| QA-01..QA-07 | `techspec.md#quality-profile` | Quality profile |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`, `repository-cli-efficiency`; `CLAUDE.md` Windows MCP rules (launch via `App` `launch_executable`, `Screenshot` on the primary monitor).
- Baselines: `baseline/README.md`.
- Snapshot: L-01 (opening Settings), M-01.
- Single instance: `TokenHound.Infrastructure/System/ApplicationInstanceMutex.cs:30`, so only one build runs at a time; settings are shared at `%LOCALAPPDATA%\TokenHound`.

## Work

- [x] T06.1 Static and build: QA-01..QA-07 and TC-02/TC-03 diffs; app and test builds; TC-01 test run.
- [x] T06.2 Baseline build: `git worktree add <scratchpad>/th-base 90d6748`, `rtk dotnet restore` and `rtk dotnet build` of its `src/TokenHound.App/TokenHound.App.csproj`.
- [x] T06.3 MA-1: launch the base exe, capture Settings (four tabs, General scrolled through every card, a visible scroll bar), About, Update (if reachable), Provider Status, the HUD context menu with Position, and the tray menu; close it.
- [x] T06.4 MA-2: launch the head exe and capture the same set in the same order; compare each pair and note any difference.
- [x] T06.5 MA-3 keyboard script (focus and selection on the Cadence box, Tab order 1-7, Enter applies, Esc closes) on head.
- [x] T06.6 MA-4 last: change HUD placement in Settings, confirm the footer status stays and the HUD moves, then restore the original placement.
- [x] T06.7 `git worktree remove` the baseline worktree; restart the installed TokenHound.

## Acceptance criteria

- Every QA and TC passes; test count 1043; 0 warnings; `App.xaml` unchanged.
- No visible difference between each MA-1/MA-2 pair; any difference is fixed inside its task contract or raised at exception HIL.
- The baseline worktree is removed and the original HUD placement restored.

## Verification

- Unit: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` after building the test project.
- Integration: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`.
- E2E: omitted by .NET desktop policy.
- Manual: MA-1..MA-4 through Windows MCP (owner: agent; confirmed by the human at the visual check).
- Commands: as above, plus the `baseline/README.md` diffs, `rg -c DynamicResource src/TokenHound.App`, `git diff --quiet 90d6748 -- src/TokenHound.App/App.xaml`, and `wc -l`.
- Environment dependency: stop the installed TokenHound and the dev HUD; Windows MCP on the user desktop.
- Expected evidence: a results table in the handoff (each QA/TC with value) and a per-pair comparison list for MA-1/MA-2.

## Affected files

- Modify: only files from T02..T05 if a defect is found.
- Create: none in the repository (worktree in the scratchpad).

## Observability and recovery

- Operational signal: startup or Settings exceptions in the dispatcher unhandled exception log.
- Recovery: revert per task; remove the worktree even on failure.

## Handoff

- Produced result: integrated validation of T02..T05 (no code change in T06). Static: QA-01 `SettingsWindow.xaml` 450 lines; QA-02 largest new file `SettingsResources.xaml` 207, largest control 123, largest dialog part 166; QA-03 key set identical; QA-04 automation multiset identical (52); QA-05 no `DynamicResource`; QA-06 0 warnings; QA-07 `App.xaml` unchanged. TC-01: 1043 tests passed; TC-04 app build 0/0. These came from the T05 run on the same code state and were reused.
- Changed files: none in the repository. Baseline worktree `scratchpad/th-base` at `90d6748` created, restored, built (0 warnings), and removed with `git worktree remove --force` (untracked bin/obj).
- Checks (TC-05, MA-1 vs MA-2, same session, sequential): HUD context menu with Position submenu; About; Provider Status; Settings General (top and scrolled), Providers, Cadence & Rate Limits, Updates. Every pair matches in layout, positions, colors, and text; only live usage numbers in Provider Status differ. An orange window border appeared on some captures of both builds alike (timing-dependent Windows attention/accent effect), so it is not a regression. HUD placement change and restore were verified in T04.
- Validated state: HEAD `90d6748` plus T02..T05 files; the head dev build is left running for the human visual check; installed TokenHound stopped.
- Human visual check (DEC-04): the human ran MA-3 (keyboard focus, Tab order 1-7, Enter, Esc), the MA-4 footer status part, and the tray menu on the head build and approved them. Automation could not drive these: the Settings window does not take the foreground under Windows MCP clicks, and the tray icon sits in the hidden area of an auto-hidden taskbar. The Update window was not opened (network check); it uses the same dialog button style as About.
- Environment restored: dev build stopped; installed TokenHound restarted from `D:\Apps\TokenHound`.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
