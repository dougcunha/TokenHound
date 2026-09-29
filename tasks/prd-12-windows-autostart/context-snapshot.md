# Context snapshot — prd-12-windows-autostart

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-09-29
- stage: acceptance
- stage_source: codereview_2/
- covers_through: codereview_2
- authored_code: yes
- git_head: b9bffe6
- worktree: 13 changed: .agents/skills/** (11 skill files edited outside this flow), tasks/triage-log.jsonl, tasks/prd-12-windows-autostart/
- next_step: — (feature completed; commit on request)
- other_eligible: —
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

- Why next: codereview_2 is APPROVED WITH RESERVATIONS. The human decides whether to correct OI-01/OI-02 or finalize.
- Read first: `codereview_2/codereview.md#Findings`.
- Known change points: OI-01 is `src/TokenHound.Infrastructure/Startup/ShellLink.cs:13` and `StartupLaunchService.cs:60` (public → internal, plus `InternalsVisibleTo`). OI-02 is `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs:79`.
- Applicable entries: O-01, O-02
- Watch out: a correction round needs a new delegated re-review (codereview_3). The review base is b9bffe6.

## Decisions

- [D-01] (when: now) Process sdd-lean, jev active, and HIL 1+2 merged — src: workflow.md#DEC-01; until: feature closed

## Learnings

- [L-01] (when: on-run: dotnet test; dotnet run) Tests are MTP executables. Use `rtk dotnet run --project tests/<X> --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Name*"`. CI runs them on windows-latest — src: .github/workflows/ci.yml:35-46; until: CI changes
- [L-02] (when: on-edit: tests/TokenHound.Infrastructure.Tests/**; src/TokenHound.App/ViewModels/**) App ViewModels are tested by linking the source into Infrastructure.Tests (`<Compile Include ... Link>`), so new ViewModels must not use WPF types — src: tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj:60-80; until: test layout changes
- [L-03] (when: on-run: build-installer.ps1; ISCC) ISCC is at `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`, not Program Files (x86) — src: —; until: next session

## Code map

- [M-01] (when: on-edit: src/TokenHound.App/**; review) `App.xaml.cs:349 CreateSettingsViewModel` builds the Settings ViewModel on every open. `SettingsViewModel.cs:62` has the `Updates` init property pattern. `SettingsWindow.xaml:497-600` holds the Updates card markup — src: —; until: review done

## Open threads

- [O-02] (when: now) 11 `.agents/skills/**` files were modified in the worktree by someone other than this flow. Do not stage them with feature commits — src: —; until: the human commits or reverts them
