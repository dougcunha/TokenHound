# Context snapshot — prd-11-auto-update

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-09-28
- stage: tasks
- stage_source: tasks.md
- covers_through: T01-T09 done (all tasks)
- authored_code: yes
- git_head: a8bd1bf
- worktree: uncommitted feature code under src/, tests/, installer/, tasks/prd-11-auto-update/; pre-existing .agents/, .opencode/, AGENTS.md, CLAUDE.md, .gitignore, context-brake files
- next_step: human visual check (MA-1..MA-4, Updates tab), then sdd-review-code delegated (base a8bd1bf)
- other_eligible: none
- superseded_by: —

## Load map

| Tier | Load when |
| --- | --- |
| `now` | Session start: header, next step brief, open threads waiting on the user |
| `on-select` | Chosen unit matches a trigger: task or finding ID, affected file, or traceability ID |
| `on-edit` | About to edit or create a path matching a trigger |
| `on-run` | About to run a matching command, or it just failed |
| `on-demand` | A gist is not enough: follow its `src:` pointer, that section only |

Review sessions load only the header, next step brief, `Open threads`, and `on-run` entries.

Entry shape: `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

## Next step brief

- Why next: every task is done; the flow requires the human visual check before the independent review.
- Read first: checkpoint.json, workflow.md#Events, techspec.md#Test approach (MA-1..MA-4), done/task_08.md#Handoff (MA-2/MA-3 build commands).
- Known change points: none pending; corrections, if any, come from the review report.
- Applicable entries: D-04, L-07, O-01
- Watch out: the review must be delegated to a fresh-context reviewer (this session authored the code); J3 produced no usable verdict for T01, T03, T05-T09: raise stopping J3 (or committing per task) at HIL 3.

## Decisions

- [D-03] (when: on-select: T05; T08) Mutex ownership lives in T08 (startup acquire, `--updated` wait); T05 only provides `ApplicationInstanceMutex` — src: done/task_05.md#Handoff; until: T08 done
- [D-04] (when: now) Human: keep J3 with the full literal diff for T04-T09; a failed call is logged as operational-failure and the flow moves on — src: workflow.md#Events; until: feature closed

## Learnings

- [L-01] (when: on-edit: tasks/**/checkpoint.json) Write checkpoints with python json.dump and forward-slash paths; Bash heredocs mangle `\` — src: —; until: next session
- [L-03] (when: on-run: dotnet test) App view models/tray types are tested in Infrastructure.Tests via `Compile Include … Link=`; add new App files there — src: techspec.md#Test approach; until: test project layout changes
- [L-04] (when: on-edit: tests/TokenHound.Infrastructure.Tests/**) Namespace `TokenHound.Infrastructure.Tests.System` shadows `System.*` inside test files: add `using System.Globalization;` instead of fully qualified `System.X` — src: done/task_03.md#Handoff; until: namespace renamed
- [L-05] (when: on-run: ISCC) Git Bash rewrites `/D...` switches as paths; run ISCC or build-installer.ps1 from PowerShell. ISCC lives at `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` — src: done/task_05.md#Handoff; until: tool moves
- [L-06] (when: on-edit: src/TokenHound.Infrastructure/Configuration/UserSettings*.cs) A new settings section must also be added to `UserSettingsFile.MergeWithDefaults`, or it is dropped on load — src: done/task_02.md#Handoff; until: merge becomes generic
- [L-07] (when: on-run: dotnet test) List test names with `dotnet <test dll> --no-banner --list-tests --filter-class ...`; the `dotnet test --project` list form prints nothing under RTK — src: —; until: tooling changes

## Code map

- [M-02] (when: on-edit: src/TokenHound.App/UI/Tray/**) `TrayMenuModel.BuildDescriptor:41-54`, `TrayIconViewModel.Invoke:80-112`, `ITrayIcon.cs`, `TaskbarIconAdapter.BuildContextMenu:86-110`, `TrayIconHost.OnMenuItemInvoked:133-138` — src: techspec.md#Sources and traceability; until: files change
- [M-04] (when: on-edit: src/TokenHound.App/App*.cs) `App.xaml.cs` is 449 lines (baseline hit): add only call sites; wiring goes in new `App.Updates.cs` like `App.Mcp.cs`; `InitializeUi:359-389`, `InitializeTray:391-419`, `ShutdownAsync:421-427` — src: techspec.md#Terrain baseline; until: T09 done
- [M-05] (when: on-select: T06; T07; T08) Backend API: `UpdateCheckService.CheckAsync(trigger, ct)` → `UpdateCheckOutcome` (Status, Release, LatestVersion, RetryAfterUtc, Reason); `UpdateDownloader.DownloadAsync(asset, progress, ct)`; `InstallModeDetector.DetectMode/IsWritable`; `UpdateAssetSelector.Select(release, mode, RuntimeInformation.ProcessArchitecture)`; `PortableUpdateApplier.Apply(zip, appDir, exe)`; `InstallerUpdateLauncher.Launch(setup)`; `UpdateSwapJournal.CleanupAfterUpdate()`; `ApplicationInstanceMutex.TryAcquire(timeout)` — src: src/TokenHound.Infrastructure/Updates/; until: files change
- [M-06] (when: on-select: T06; T08) Dialog pattern: `UI/Windows/ProviderStatusDialog.cs` (activate-or-create, owned VM); styles `UI/Styles/DialogResources.xaml` — src: techspec.md#Sources and traceability; until: files change

## Open threads

- [O-01] (when: now) MA-2/MA-3 inputs already built: `<scratchpad>/ma-builds/publish/win-x64` (portable 0.0.1) and `ma-builds/dist/TokenHound-Setup-0.0.1-win-x64.exe`; the scratchpad is session-specific, so rebuild with `installer/build-installer.ps1 -Version 0.0.1` (PowerShell) if it is gone — src: done/task_05.md#Handoff; until: visual check done
