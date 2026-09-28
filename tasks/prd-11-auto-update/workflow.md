# Workflow Log: PRD 11 — Auto-update from GitHub Releases

## Feature Context
- **Slug**: `prd-11-auto-update`
- **Objective**: Check the latest GitHub release periodically (configurable interval) and on demand from the tray menu, notify when a newer version exists, and on confirmation download and apply it: fx-dependent zip for portable (close, swap exe, restart) or Inno Setup installer for installed copies, detected by an installer-only marker file (absent = portable).
- **Process Level**: `sdd-lean` (decided at HIL 0).
- **JEV Mode**: `shadow`.
- **Git Base Commit**: `a8bd1bf3361a2dec392ee0b429a450d2ea3bec9d` (worktree clean before this flow, apart from `tasks/triage-log.jsonl` line appended by triage).

## Decisions

### DEC-01: Process Level and Stops Adjustment (sdd-lean)
- **Date**: 2026-09-28
- **Decision**: The human chose `sdd-lean` at HIL 0, with jev in `shadow` (`--jev shadow`).
- **Triage**: The rubric recommended `sdd-full` because S1 (config, tray key, installer marker, release asset names), S2 (GitHub rate limit under the 429 invariant, executing downloaded binaries), S5 and S8 (replacing the running exe) are present. jev_decide gave `sdd-full` 0.55 vs `sdd-lean` 0.44. The human overrode both. Record: `tasks/triage-log.jsonl` 2026-09-28.
- **Stops Modification**: HIL 1 and HIL 2 are merged into one decision after PRD, TechSpec, and task plan are drafted. Independent code review at step 5 is kept.
- **Open decisions to settle in PRD/TechSpec at the merged HIL**: prerelease handling, x64/arm64 asset selection, hash verification, portable swap mechanism, installer silent flags, default interval and opt-out, skip-this-version, non-writable install dir, GitHub 60/h unauthenticated limit under the 429 persistence invariant.

### DEC-02: Merged HIL 1+2 approved
- **Date**: 2026-09-28
- **Decision**: The human approved `prd.md`, `techspec.md`, and `tasks.md` (T01..T09) as presented, including the PRD defaults (prereleases ignored, 24 h interval, size + optional SHA-256) and mechanisms DEC-08/DEC-09, and authorized implementation and corrections within that contract.
- **Human text**: "Aprovar" (HIL 1+2) and "Continuar aqui" (session).
- **Scope**: sha256 prd.md=613b3be497da, techspec.md=9668fefa9251, tasks.md=1ca1e1c5f702.

## Events

- 2026-09-28: jev available (jev_decide via openrouter succeeded on retry after one 503). The triage session paused at the context threshold before the PRD stage.
- 2026-09-28: Resumed session wrote `prd.md` (lean: OBJ-01..03, US-01..05, FR-01..13, NFR-01..06). Proposed product defaults for the merged HIL: prereleases ignored, 24 h default interval, size + optional SHA-256 integrity. No external content fetched (J0 not triggered). Paused before TechSpec at 59% context (YELLOW): the TechSpec exploration would not finish before the threshold.
- 2026-09-28: Human chose "Continue here" at the session pause, but the answer came in at 66% (ContextBrake RED), so the context pause repeated before the TechSpec started. Checkpoint generation 2 stays paused.
- 2026-09-28: Correction: the 66-77% RED/CRITICAL readings were false. ContextBrake divided measured tokens by a 128k fallback (`contextWindowCeiling`; no statusline window source). The human raised it to 1000000 in `context-brake.config.json` (real usage ~10%) and authorized continuing in this session while ContextBrake is fixed in parallel. TechSpec stage started.
- 2026-09-28: jev probe ok this session (jev_noul, provider openrouter). `techspec.md` written (DEC-01..13, CMP-01..20, TC-01..21, MA-1..4); preparatory refactoring not recommended (App.xaml.cs absorbed by DEC-13). J1 shadow: 32/32 verified, NFR-06 flagged `review` (0.73); no change made (shadow).
- 2026-09-28: `tasks.md` + `task_01..09.md` written (DAG T01..T09). J2 shadow: 47/47 verified, TC-04 flagged `review` (0.73); no change (shadow). Merged HIL 1+2 presented.
- 2026-09-28: T01-T03 done. J3 for T03 was not dispatched (generation stopped before the call); logged as operational-failure in jev-log.jsonl.
- 2026-09-28: Human decision (DEC-03 scope: jev J3 in this feature): keep J3 with the full literal diff for T04-T09; a failed call is logged as operational-failure and the flow moves on. Human text: "Keep J3 as is".
- 2026-09-28: T01-T05 done; T05 J3 logged as operational-failure (tests hunk summarized); build-installer.ps1 verified. Context pause before T06 (estimated ~60-65%, ContextBrake silent since 52%).
- 2026-09-28: T06 done (tray entry, UpdateViewModel, UpdateWindow/Dialog, App.Updates.cs; TC-18 check part, TC-20; build 0 warnings, Infrastructure 900 and Core 161 passed). J3 for T06 logged as operational-failure: the 60 KB diff was split in two and the response carrying part 1 was stopped by a safety classifier; not retried (DEC-03 rule). MA-1 pending at the visual check.
- 2026-09-28: T07 done (UpdateCheckScheduler, tray balloon + click, App.Updates scheduler start; TC-17, TC-21; build 0 warnings, Infrastructure 908 passed). J3 for T07 logged as operational-failure (not dispatched: no isolatable literal diff because T07 and T06 share uncommitted files). J3 has now produced no usable verdict for T01, T03, T05, T06, T07: raise at HIL 3 whether to stop it for the rest of the feature.
- 2026-09-28: T08 done (UpdateApplyCoordinator, apply states in UpdateViewModel, --updated startup mutex/cleanup; TC-18 apply part; build 0 warnings, Infrastructure 927 passed). J3 for T08 logged as operational-failure (not dispatched; no isolatable literal diff). MA-2/MA-3 pending at the visual check.
- 2026-09-28: T09 done (UpdateSettingsViewModel, Updates tab, factory wiring; TC-19; build 0 warnings, Infrastructure 938 passed). J3 for T09 logged as operational-failure (not dispatched; no isolatable literal diff). All tasks T01-T09 done; next is the human visual check (MA-1..MA-4 and the Updates tab), then the delegated review. Escalation trigger noted for the review: SettingsWindow.xaml above 500 lines (pre-existing 623, now 732).
- 2026-09-28: Human correction after the visual-check gate: the HUD right-click popup was missing the update entry. Added "Check for Updates…" before About in `NotchWindow.xaml`, `HudActionsViewModel.CheckForUpdates` (init property `CheckForUpdatesAction`, wired in `App.xaml.cs` to `ShowUpdateDialog`), test `HudActionsUpdateTests`; build 0 warnings, Infrastructure 939 passed. Visual check still pending.

### DEC-03: Visual check passed; review waived; HIL 3 accepted
- **Date**: 2026-09-28
- **Decision**: The human validated the visual check (including the HUD popup entry added afterwards) and asked to close the feature, commit, push, tag, and publish release notes.
- **Human text**: "Está validado. Pode fechar, fazer o commit e enviar. Depois gere uma tag de versão, aguarde o release ser publicado, e adicione as notas de release. Também atualize o README.md se não tiver feito."
- **Limitation recorded**: the independent review of step 5 (kept by DEC-01) did not run: closing was requested before delegating it, so `review_status` stays `null`, no `codereview_*` report exists, and the jev pilot summary is not produced (jev-log.jsonl kept). MA-2, MA-3, and MA-4 need a published newer release and are exercised by this release's first auto-update.
- **Events**: 2026-09-28 closed as `completed`.
