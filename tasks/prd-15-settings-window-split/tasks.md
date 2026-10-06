# Implementation plan — Settings window and dialog resources split

## Stable sources

- PRD: `tasks/prd-15-settings-window-split/prd.md`
- TechSpec: `tasks/prd-15-settings-window-split/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Text baselines (keys, automation names, line counts) saved under `baseline/` | — | T02, T03 |
| T02 | `DialogResources.xaml` split into five dictionaries behind an aggregator | T01 | T06 |
| T03 | Settings-only keys live in `SettingsResources.xaml`; Settings still opens | T01 | T04 |
| T04 | General cards are three user controls | T03 | T05 |
| T05 | Updates and Providers tab content are user controls; `SettingsWindow.xaml` ≤ 500 lines | T04 | T06 |
| T06 | Integrated validation: base-vs-head visual parity, keyboard, placement, quality profile | T02, T05 | — |

T04 → T05 is a file-collision edge (both edit `SettingsWindow.xaml`), not a contract dependency. T02 touches only `UI/Styles/Dialog*.xaml` and `HudMenus.xaml`, so it may run before or after T03..T05.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| R-01 | `prd.md#behaviors-to-preserve` | Tabs, order, headers, General collapse | T03, T06 | TC-05 |
| R-02 | `prd.md#behaviors-to-preserve` | General cards order, spacing, null collapse | T04, T06 | TC-02, TC-05 |
| R-03 | `prd.md#behaviors-to-preserve` | Bindings, commands, automation names of four sections | T04, T05, T06 | TC-01, TC-02 |
| R-04 | `prd.md#behaviors-to-preserve` | Focus, Enter, Tab order 1-7 | T06 | TC-06 |
| R-05 | `prd.md#behaviors-to-preserve` | Placement combo does not hide footer status | T04, T06 | TC-07 |
| R-06 | `prd.md#behaviors-to-preserve` | Esc, Close, Cancel | T06 | TC-06 |
| R-07 | `prd.md#behaviors-to-preserve` | Providers rows; opens on Providers when General unavailable | T05, T06 | TC-01, TC-05 |
| R-08 | `prd.md#behaviors-to-preserve` | Updates tab visible with null view model | T05 | Markup review |
| R-09 | `prd.md#behaviors-to-preserve` | Dialog controls render the same in four windows | T02, T06 | TC-03, TC-05 |
| R-10 | `prd.md#behaviors-to-preserve` | HUD and tray menus; keys found by name | T02, T06 | TC-03, TC-05 |
| R-11 | `prd.md#behaviors-to-preserve` | Colors and badge brushes found by name | T02, T06 | TC-03, TC-05 |
| R-12 | `prd.md#behaviors-to-preserve` | Implicit scroll bar style | T02, T03, T06 | TC-05 |
| DEC-01 | `techspec.md#technical-decisions` | Cards and tab content as user controls, verbatim | T04, T05 | TC-02 |
| DEC-02 | `techspec.md#technical-decisions` | Cadence tab and footer stay in the window | T04, T05 | TC-06 |
| DEC-03 | `techspec.md#technical-decisions` | `SettingsResources.xaml`, merged by window and three controls | T03, T04, T05 | TC-04, Settings opens |
| DEC-04, DEC-05 | `techspec.md#technical-decisions` | Five dictionaries, aggregator, parts merge Foundation | T02 | TC-03, TC-04 |
| DEC-06 | `techspec.md#technical-decisions` | Item templates unchanged | T02 | Markup diff |
| DEC-07 | `techspec.md#technical-decisions` | Visual baseline from `git_base` worktree, same session | T01, T06 | TC-05 |
| TC-01..TC-07 | `techspec.md#safety-net` | Unit, static, build, manual checks | T01..T06 | per task |
| QA-01..QA-07 | `techspec.md#quality-profile` | Line limits, key set, names, no `DynamicResource`, 0 warnings, `App.xaml` unchanged | T02..T06 | T06 runs all |

## Tasks

- [T01 — Record text baselines](done/task_01.md): QA-03 keys, QA-04 automation names, and line counts saved at `git_base`; capture-to-file probe.
- [T02 — Split dialog resources](done/task_02.md): five themed dictionaries merged by `DialogResources.xaml`, keys and order unchanged.
- [T03 — Move Settings-only resources](done/task_03.md): six window-local keys move to `SettingsResources.xaml`; Settings opens.
- [T04 — Extract General cards](done/task_04.md): Startup, HUD size, and HUD placement cards become user controls.
- [T05 — Extract Updates and Providers content](done/task_05.md): two tab panels become user controls; window at most 500 lines.
- [T06 — Validate the integrated refactoring](done/task_06.md): TC-01..TC-07 and QA-01..QA-07, with the base-vs-head visual comparison.

## Coverage gate

- Coverage: pass; R-01..R-12, DEC-01..DEC-07, TC-01..TC-07, QA-01..QA-07 each map to a task.
- Traceability: pass; each task cites its source IDs.
- Dependencies: pass; acyclic, with the T04 → T05 file-collision edge added.
- Atomicity: pass; each task is one TechSpec step with its own build and static checks.
- Executability: pass; commands come from the TechSpec safety net and `CLAUDE.md`.
- Validation profile: pass; E2E omitted by .NET desktop policy; unit (TC-01), static (TC-02, TC-03), build (TC-04), manual (TC-05..TC-07) through Windows MCP.
- Idempotency: pass; T01 rewrites the baseline files from `git_base`, never from the working tree; later tasks are verbatim moves that can be redone.

## Assumptions and open items

- Assumption: visual parity is judged in the same session from a `git_base` worktree build (DEC-07), because Windows MCP screenshots are not saved to files.
- Open item: none blocking.
- Required environment: Windows MCP on the user desktop for launches and screenshots (TC-05..TC-07); stop the installed TokenHound (`D:\Apps\TokenHound`) and the dev HUD before builds and checks, restart the installed app after. Authorization: recorded at HIL 2.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
- [x] T06 — done

## Problems and solutions
- T05: running the baseline diffs through PowerShell `bash -c` fails silently because `rg` is not on that PATH (empty input, every line reported missing). Run the `baseline/README.md` commands in the Bash tool.
